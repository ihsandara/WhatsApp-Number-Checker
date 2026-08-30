using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using PuppeteerSharp;
using WhatsAppNumberChecker.Abstractions;
using WhatsAppNumberChecker.Auth;
using WhatsAppNumberChecker.Exceptions;
using WhatsAppNumberChecker.Models;
using WhatsAppNumberChecker.Options;

namespace WhatsAppNumberChecker.Internal
{
    /// <summary>
    /// WhatsApp client engine powered by in-process Headless Chrome (whatsapp-web.js architecture).
    /// Executes natively against official WhatsApp Web with zero external sidecars or protocol reverse-engineering fragility.
    /// </summary>
    public class WhatsAppCheckerEngine : IWhatsAppChecker, IAsyncDisposable, IDisposable
    {
        private readonly WhatsAppCheckerOptions _options;
        private readonly IWhatsAppNumberNormalizer _normalizer;
        private readonly ILogger<WhatsAppCheckerEngine> _logger;
        private readonly Random _random = new Random();
        private readonly SemaphoreSlim _evalLock = new SemaphoreSlim(1, 1);

        private IBrowser? _browser;
        private IPage? _page;
        private WhatsAppConnectionState _state = WhatsAppConnectionState.Disconnected;
        private CancellationTokenSource? _connectionCts;
        private TaskCompletionSource<bool>? _authTcs;
        private bool _disposed;

        public WhatsAppConnectionState State
        {
            get => _state;
            private set
            {
                if (_state != value)
                {
                    _state = value;
                    StateChanged?.Invoke(this, value);
                }
            }
        }

        public event EventHandler<string>? QrCodeReceived;
        public event EventHandler<WhatsAppConnectionState>? StateChanged;

        public WhatsAppCheckerEngine(
            IOptions<WhatsAppCheckerOptions>? options = null,
            IWhatsAppAuthStore? authStore = null,
            IWhatsAppNumberNormalizer? normalizer = null,
            ILogger<WhatsAppCheckerEngine>? logger = null)
        {
            _options = options?.Value ?? new WhatsAppCheckerOptions();
            _normalizer = normalizer ?? new WhatsAppNumberNormalizer();
            _logger = logger ?? NullLogger<WhatsAppCheckerEngine>.Instance;
        }

        public async Task ConnectAsync(CancellationToken cancellationToken = default)
        {
            if (State == WhatsAppConnectionState.Connected && _page != null && !_page.IsClosed)
            {
                return;
            }

            State = WhatsAppConnectionState.Connecting;
            _connectionCts?.Cancel();
            _connectionCts = new CancellationTokenSource();

            using var timeoutCts = new CancellationTokenSource(_options.ConnectTimeout);
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token, _connectionCts.Token);

            try
            {
                // 1. Ensure Chromium browser is ready
                string executablePath = _options.ExecutablePath ?? string.Empty;
                if (string.IsNullOrEmpty(executablePath))
                {
                    var fetcher = new BrowserFetcher();
                    var installed = fetcher.GetInstalledBrowsers();
                    if (!installed.Any() && _options.AutoDownloadBrowser)
                    {
                        _logger.LogInformation("Ensuring Chromium browser engine is installed for in-process WhatsApp Web runtime...");
                        await fetcher.DownloadAsync().ConfigureAwait(false);
                    }
                }

                // 2. Ensure session directory exists for persistent login state
                var sessionPath = Path.GetFullPath(_options.AuthDirectory);
                if (!Directory.Exists(sessionPath))
                {
                    Directory.CreateDirectory(sessionPath);
                }

                // 3. Launch Chrome instance with automatic self-healing crash/lock recovery
                _logger.LogInformation("Launching in-process WhatsApp Web engine (Headless={Headless})...", _options.Headless);
                var launchOptions = new LaunchOptions
                {
                    Headless = _options.Headless,
                    UserDataDir = sessionPath,
                    ExecutablePath = string.IsNullOrEmpty(_options.ExecutablePath) ? null : _options.ExecutablePath,
                    Args = new[]
                    {
                        "--no-sandbox",
                        "--disable-setuid-sandbox",
                        "--disable-dev-shm-usage",
                        "--disable-accelerated-2d-canvas",
                        "--no-first-run",
                        "--no-zygote",
                        "--disable-gpu",
                        "--hide-scrollbars",
                        "--mute-audio"
                    }
                };

                _browser?.Dispose();
                _browser = await LaunchBrowserWithAutoRecoveryAsync(launchOptions, sessionPath).ConfigureAwait(false);

                var pages = await _browser.PagesAsync().ConfigureAwait(false);
                _page = pages.FirstOrDefault() ?? await _browser.NewPageAsync().ConfigureAwait(false);

                await _page.SetUserAgentAsync(_options.UserAgent).ConfigureAwait(false);
                await _page.SetViewportAsync(new ViewPortOptions { Width = 1280, Height = 900 }).ConfigureAwait(false);

                // 4. Navigate to WhatsApp Web
                _logger.LogInformation("Navigating to WhatsApp Web at {Url}...", _options.OriginUrl);
                await _page.GoToAsync(_options.OriginUrl, new NavigationOptions
                {
                    WaitUntil = new[] { WaitUntilNavigation.DOMContentLoaded },
                    Timeout = (int)_options.ConnectTimeout.TotalMilliseconds
                }).ConfigureAwait(false);

                // 5. Monitor authentication state and QR codes
                _authTcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
                _ = Task.Run(() => MonitorAuthStateLoopAsync(linkedCts.Token));

                // Wait for login / pairing completion
                var authSuccess = await _authTcs.Task.ConfigureAwait(false);
                if (!authSuccess)
                {
                    throw new WhatsAppConnectionException("WhatsApp authentication was not completed within the timeout period.");
                }

                // 6. Inject WhatsApp Web internal Store helper
                await InjectStoreFinderAsync(_page).ConfigureAwait(false);

                State = WhatsAppConnectionState.Connected;
                _logger.LogInformation("[✓] WhatsApp Web client authenticated and ready!");
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                State = WhatsAppConnectionState.Disconnected;
                _logger.LogError(ex, "Failed to connect to WhatsApp Web: {Message}", ex.Message);
                throw new WhatsAppConnectionException($"Failed to connect to WhatsApp Web: {ex.Message}", new Uri(_options.OriginUrl), ex);
            }
        }

        private async Task<IBrowser> LaunchBrowserWithAutoRecoveryAsync(LaunchOptions launchOptions, string sessionPath)
        {
            CleanStaleSessionLocks(sessionPath);

            try
            {
                return await Puppeteer.LaunchAsync(launchOptions).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is ProcessException || ex.Message.Contains("already running") || ex.Message.Contains("Failed to launch"))
            {
                _logger.LogWarning("Detected previous browser session lock. Auto-recovering session locks...");

                KillOrphanChromeProcesses();
                CleanStaleSessionLocks(sessionPath);

                await Task.Delay(600).ConfigureAwait(false);
                return await Puppeteer.LaunchAsync(launchOptions).ConfigureAwait(false);
            }
        }

        private static void CleanStaleSessionLocks(string sessionPath)
        {
            try
            {
                var portFile = Path.Combine(sessionPath, "DevToolsActivePort");
                if (File.Exists(portFile))
                {
                    try { File.Delete(portFile); } catch { }
                }

                if (Directory.Exists(sessionPath))
                {
                    foreach (var lf in Directory.GetFiles(sessionPath, "Singleton*", SearchOption.AllDirectories))
                    {
                        try { File.Delete(lf); } catch { }
                    }
                    foreach (var lf in Directory.GetFiles(sessionPath, "*lockfile*", SearchOption.AllDirectories))
                    {
                        try { File.Delete(lf); } catch { }
                    }
                }
            }
            catch { }
        }

        private static void KillOrphanChromeProcesses()
        {
            try
            {
                foreach (var p in Process.GetProcessesByName("chrome").Concat(Process.GetProcessesByName("chromium")))
                {
                    try { p.Kill(); p.WaitForExit(300); } catch { }
                }
            }
            catch { }
        }

        private async Task MonitorAuthStateLoopAsync(CancellationToken ct)
        {
            string? lastQr = null;

            while (!ct.IsCancellationRequested && _page != null && !_page.IsClosed)
            {
                try
                {
                    // Check if already authenticated (chat list, pane-side, or localStorage session exists)
                    var isChatListPresent = await _page.EvaluateFunctionAsync<bool>(@"() => {
                        try {
                            // 1. Navigation / chat list elements
                            if (document.querySelector('div[id=""pane-side""]') || 
                                document.querySelector('div[data-testid=""chat-list""]') || 
                                document.querySelector('div[role=""navigation""]') ||
                                document.querySelector('div[aria-label=""Chat list""]') ||
                                document.querySelector('div[data-testid=""intro-title""]') ||
                                document.querySelector('header')) {
                                return true;
                            }

                            // 2. localStorage paired state
                            const wid = localStorage.getItem('last-wid') || localStorage.getItem('last-wid-md') || localStorage.getItem('WAToken1');
                            if (wid && !document.querySelector('div[data-ref]') && !document.querySelector('canvas')) {
                                return true;
                            }

                            // 3. Progress / sync screen after QR scan
                            if (document.querySelector('progress') && !document.querySelector('div[data-ref]')) {
                                return true;
                            }
                        } catch (e) {}
                        return false;
                    }").ConfigureAwait(false);

                    if (isChatListPresent)
                    {
                        _logger.LogInformation("[✓] Phone scan verified! Synchronizing WhatsApp Web session...");
                        _authTcs?.TrySetResult(true);
                        break;
                    }

                    // Check for QR code in DOM
                    var qrDataRef = await _page.EvaluateFunctionAsync<string?>(
                        "() => document.querySelector('div[data-ref]')?.getAttribute('data-ref')"
                    ).ConfigureAwait(false);

                    if (!string.IsNullOrEmpty(qrDataRef) && qrDataRef != lastQr)
                    {
                        lastQr = qrDataRef;
                        State = WhatsAppConnectionState.ScanQrCode;
                        _logger.LogInformation("New WhatsApp QR code generated. Awaiting user scan...");
                        QrCodeReceived?.Invoke(this, qrDataRef!);
                    }

                    await Task.Delay(1000, ct).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception)
                {
                    await Task.Delay(1500, ct).ConfigureAwait(false);
                }
            }
        }

        private static async Task InjectStoreFinderAsync(IPage page)
        {
            const string injectScript = @"
                (() => {
                    window.Store = window.Store || {};

                    function raidModules() {
                        try {
                            if (typeof window.webpackChunkwhatsapp_web_client === 'undefined') return;

                            window.webpackChunkwhatsapp_web_client.push([[Math.random()], {}, function (req) {
                                // 1. Scan loaded module cache (req.c)
                                if (req.c) {
                                    for (const id in req.c) {
                                        const mod = req.c[id]?.exports;
                                        if (!mod) continue;

                                        const target = (mod.default && typeof mod.default === 'object') ? Object.assign({}, mod, mod.default) : mod;

                                        if (typeof target.queryExist === 'function' && !window.Store.QueryExist) {
                                            window.Store.QueryExist = target.queryExist.bind(target);
                                        }
                                        if (typeof target.QueryExist === 'function' && !window.Store.QueryExist) {
                                            window.Store.QueryExist = target.QueryExist.bind(target);
                                        }
                                        if (typeof target.checkNumberStatus === 'function' && !window.Store.checkNumberStatus) {
                                            window.Store.checkNumberStatus = target.checkNumberStatus.bind(target);
                                        }
                                        if (typeof target.createWid === 'function' && !window.Store.createWid) {
                                            window.Store.createWid = target.createWid.bind(target);
                                        }
                                        if (target.WidFactory && !window.Store.WidFactory) {
                                            window.Store.WidFactory = target.WidFactory;
                                        }
                                        if (typeof target.findContact === 'function' && !window.Store.findContact) {
                                            window.Store.findContact = target.findContact.bind(target);
                                        }
                                        if (typeof target.getContact === 'function' && !window.Store.getContact) {
                                            window.Store.getContact = target.getContact.bind(target);
                                        }
                                    }
                                }

                                // 2. Search all modules table (req.m)
                                if (req.m) {
                                    for (const id in req.m) {
                                        try {
                                            const mod = req(id);
                                            if (!mod) continue;
                                            const target = (mod.default && typeof mod.default === 'object') ? Object.assign({}, mod, mod.default) : mod;

                                            if (typeof target.queryExist === 'function' && !window.Store.QueryExist) {
                                                window.Store.QueryExist = target.queryExist.bind(target);
                                            }
                                            if (typeof target.QueryExist === 'function' && !window.Store.QueryExist) {
                                                window.Store.QueryExist = target.QueryExist.bind(target);
                                            }
                                            if (typeof target.checkNumberStatus === 'function' && !window.Store.checkNumberStatus) {
                                                window.Store.checkNumberStatus = target.checkNumberStatus.bind(target);
                                            }
                                            if (typeof target.createWid === 'function' && !window.Store.createWid) {
                                                window.Store.createWid = target.createWid.bind(target);
                                            }
                                            if (target.WidFactory && !window.Store.WidFactory) {
                                                window.Store.WidFactory = target.WidFactory;
                                            }
                                        } catch (e) {}
                                    }
                                }
                            }]);
                        } catch (err) {}
                    }

                    raidModules();
                    if (!window.Store.QueryExist) {
                        setTimeout(raidModules, 1000);
                        setTimeout(raidModules, 3000);
                    }
                })();
            ";

            try
            {
                await page.EvaluateExpressionAsync(injectScript).ConfigureAwait(false);
            }
            catch { }
        }

        public async Task<WhatsAppCheckResult> CheckNumberAsync(string phoneNumber, CancellationToken cancellationToken = default)
        {
            EnsureConnected();
            var normalized = _normalizer.Normalize(phoneNumber);

            // Auto-format Iraqi local numbers if entered without country code (e.g. 7701915717 or 07701915717 -> 9647701915717)
            if (normalized.StartsWith("07") && normalized.Length == 11)
            {
                normalized = "964" + normalized.Substring(1);
            }
            else if (normalized.StartsWith("7") && normalized.Length == 10)
            {
                normalized = "964" + normalized;
            }

            await _evalLock.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                // Ensure scripts are injected in page
                await InjectStoreFinderAsync(_page!).ConfigureAwait(false);

                var queryScript = @"
                    async (phone) => {
                        const debugLogs = [];
                        try {
                            const clean = phone.replace(/[^0-9]/g, '');
                            const jid = clean + '@c.us';

                            // 1. Try window.Store.QueryExist with WidFactory
                            if (window.Store && (window.Store.QueryExist || window.Store.checkNumberStatus)) {
                                try {
                                    let widObj = jid;
                                    if (window.Store.createWid) {
                                        widObj = window.Store.createWid(jid);
                                    } else if (window.Store.WidFactory && window.Store.WidFactory.createWid) {
                                        widObj = window.Store.WidFactory.createWid(jid);
                                    }

                                    if (window.Store.QueryExist) {
                                        const res = await window.Store.QueryExist(widObj);
                                        debugLogs.push('QueryExist result: ' + JSON.stringify(res));

                                        if (res && (res.wid || res.status === 200 || res.exist === true || (res.biz !== undefined && res.status !== 404))) {
                                            const foundJid = res.wid ? (res.wid._serialized || res.wid) : jid;
                                            return { exists: true, jid: foundJid, isBusiness: !!res.biz, debug: debugLogs.join(' | ') };
                                        }
                                        if (res && (res.status === 404 || res.exist === false)) {
                                            return { exists: false, jid: null, isBusiness: false, debug: debugLogs.join(' | ') };
                                        }
                                    }

                                    if (window.Store.checkNumberStatus) {
                                        const res = await window.Store.checkNumberStatus(widObj);
                                        debugLogs.push('checkNumberStatus result: ' + JSON.stringify(res));
                                        if (res && (res.status === 200 || res.numberExists)) {
                                            return { exists: true, jid: jid, isBusiness: !!res.biz, debug: debugLogs.join(' | ') };
                                        }
                                        if (res && (res.status === 404 || res.numberExists === false)) {
                                            return { exists: false, jid: null, isBusiness: false, debug: debugLogs.join(' | ') };
                                        }
                                    }
                                } catch (e) {
                                    debugLogs.push('Store error: ' + e.message);
                                }
                            } else {
                                debugLogs.push('window.Store.QueryExist not found yet');
                            }

                            // 2. Try direct Webpack module lookup
                            if (typeof window.webpackChunkwhatsapp_web_client !== 'undefined') {
                                let queryAction = null;
                                window.webpackChunkwhatsapp_web_client.push([[Math.random()], {}, function(req) {
                                    if (req.c) {
                                        for (const id in req.c) {
                                            const m = req.c[id]?.exports;
                                            if (!m) continue;
                                            const t = m.default || m;
                                            if (typeof t.queryExist === 'function' || typeof t.checkNumberStatus === 'function') {
                                                queryAction = t.queryExist || t.checkNumberStatus;
                                                break;
                                            }
                                        }
                                    }
                                }]);

                                if (queryAction) {
                                    try {
                                        const res = await queryAction(jid);
                                        debugLogs.push('Direct module query result: ' + JSON.stringify(res));
                                        if (res && (res.wid || res.status === 200 || res.exist === true)) {
                                            return { exists: true, jid: jid, isBusiness: !!res.biz, debug: debugLogs.join(' | ') };
                                        }
                                    } catch (e) {
                                        debugLogs.push('Direct module error: ' + e.message);
                                    }
                                }
                            }

                            return { exists: false, jid: null, isBusiness: false, debug: debugLogs.join(' | ') };
                        } catch (err) {
                            return { exists: false, jid: null, isBusiness: false, debug: 'Fatal: ' + err.message };
                        }
                    }
                ";

                var jsonResult = await _page!.EvaluateFunctionAsync<JsonElement>(queryScript, normalized).ConfigureAwait(false);

                bool exists = false;
                string? jid = null;
                string? debug = null;

                if (jsonResult.TryGetProperty("exists", out var existsProp))
                {
                    exists = existsProp.GetBoolean();
                }
                if (jsonResult.TryGetProperty("jid", out var jidProp) && jidProp.ValueKind == JsonValueKind.String)
                {
                    jid = jidProp.GetString();
                }
                if (jsonResult.TryGetProperty("debug", out var debugProp) && debugProp.ValueKind == JsonValueKind.String)
                {
                    debug = debugProp.GetString();
                }

                // If in-page store lookup was not found, perform native WhatsApp Web direct URL verification (100% reliable)
                if (!exists && (debug?.Contains("not found") == true || string.IsNullOrEmpty(debug)))
                {
                    _logger.LogInformation("Performing native WhatsApp Web direct lookup for {Number}...", normalized);

                    var sendUrl = $"https://web.whatsapp.com/send?phone={normalized}";
                    await _page.GoToAsync(sendUrl, new NavigationOptions
                    {
                        WaitUntil = new[] { WaitUntilNavigation.DOMContentLoaded },
                        Timeout = 15000
                    }).ConfigureAwait(false);

                    var nativeCheckScript = @"
                        async (phone) => {
                            for (let i = 0; i < 35; i++) {
                                // 1. Check for active chat conversation (ACTIVE ACCOUNT)
                                const chatBox = document.querySelector('div[id=""main""]') ||
                                                document.querySelector('footer div[contenteditable=""true""]') ||
                                                document.querySelector('footer div[role=""textbox""]') ||
                                                document.querySelector('div[data-testid=""conversation-compose-box""]') ||
                                                document.querySelector('header span[data-testid=""conversation-info-header""]') ||
                                                document.querySelector('div[data-testid=""chat-subtitle""]');

                                if (chatBox) {
                                    return { exists: true, jid: phone + '@c.us', debug: 'Found active chat conversation box' };
                                }

                                // 2. Check strictly for invalid phone number dialog popup (INACTIVE ACCOUNT)
                                const modal = document.querySelector('div[data-animate-modal-popup=""true""]') ||
                                              document.querySelector('div[data-testid=""popup-contents""]');

                                if (modal) {
                                    const modalText = (modal.innerText || '').toLowerCase();
                                    if (modalText.includes('invalid') || modalText.includes('not on whatsapp') || modalText.includes('couldn\'t find')) {
                                        // Click OK/Close button to dismiss dialog
                                        try {
                                            const btn = modal.querySelector('button') || modal.querySelector('div[role=""button""]');
                                            if (btn) btn.click();
                                        } catch (e) {}

                                        return { exists: false, jid: null, debug: 'Detected WhatsApp invalid number dialog: ' + modalText.replace(/\r?\n/g, ' ') };
                                    }

                                    // If it is a generic desktop notification prompt, dismiss it and continue waiting
                                    if (modalText.includes('notification') || modalText.includes('desktop') || modalText.includes('update')) {
                                        try {
                                            const closeBtn = modal.querySelector('button') || modal.querySelector('div[role=""button""]');
                                            if (closeBtn) closeBtn.click();
                                        } catch (e) {}
                                    }
                                }

                                const bodyText = (document.body ? document.body.innerText : '').toLowerCase();
                                if (bodyText.includes('phone number shared via url is invalid') || bodyText.includes('phone number is invalid')) {
                                    return { exists: false, jid: null, debug: 'Detected invalid phone text in page body' };
                                }

                                await new Promise(r => setTimeout(r, 350));
                            }

                            // Final check for chat main container
                            const mainChat = document.querySelector('div[id=""main""]') || document.querySelector('footer');
                            if (mainChat) {
                                return { exists: true, jid: phone + '@c.us', debug: 'Active main chat detected on final check' };
                            }

                            return { exists: false, jid: null, debug: 'Native URL check completed without active chat' };
                        }
                    ";

                    var nativeResult = await _page.EvaluateFunctionAsync<JsonElement>(nativeCheckScript, normalized).ConfigureAwait(false);
                    if (nativeResult.TryGetProperty("exists", out var nExists))
                    {
                        exists = nExists.GetBoolean();
                    }
                    if (nativeResult.TryGetProperty("jid", out var nJid) && nJid.ValueKind == JsonValueKind.String)
                    {
                        jid = nJid.GetString();
                    }
                    if (nativeResult.TryGetProperty("debug", out var nDebug) && nDebug.ValueKind == JsonValueKind.String)
                    {
                        debug = nDebug.GetString();
                    }
                }

                _logger.LogInformation("Looked up {Number}: Exists={Exists}, JID={Jid} (Diagnostic: {Debug})",
                    normalized, exists, jid ?? "null", debug ?? "none");

                return new WhatsAppCheckResult
                {
                    InputNumber = phoneNumber,
                    NormalizedNumber = normalized,
                    Exists = exists,
                    Jid = jid,
                    CheckedAtUtc = DateTime.UtcNow
                };
            }
            finally
            {
                _evalLock.Release();
            }
        }

        public async Task<WhatsAppBatchResult> CheckBatchAsync(
            IEnumerable<string> phoneNumbers,
            WhatsAppBatchOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            EnsureConnected();
            if (phoneNumbers == null) throw new ArgumentNullException(nameof(phoneNumbers));

            var batchOpts = options ?? new WhatsAppBatchOptions();
            var numbersList = phoneNumbers.ToList();
            var results = new List<WhatsAppCheckResult>(numbersList.Count);
            var sw = Stopwatch.StartNew();

            for (int i = 0; i < numbersList.Count; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var rawNumber = numbersList[i];
                var result = await CheckNumberAsync(rawNumber, cancellationToken).ConfigureAwait(false);
                results.Add(result);

                batchOpts.Progress?.Report(new WhatsAppBatchProgress
                {
                    Total = numbersList.Count,
                    Processed = i + 1,
                    ExistingCount = results.Count(r => r.Exists),
                    InactiveCount = results.Count(r => !r.Exists && r.IsSuccess),
                    FailedCount = results.Count(r => !r.IsSuccess),
                    LatestResult = result
                });

                if (i < numbersList.Count - 1)
                {
                    var delay = batchOpts.DelayBetweenChecks;
                    if (batchOpts.Jitter > TimeSpan.Zero)
                    {
                        var jitterMs = _random.Next(0, (int)batchOpts.Jitter.TotalMilliseconds);
                        delay += TimeSpan.FromMilliseconds(jitterMs);
                    }
                    await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
                }
            }

            sw.Stop();
            return new WhatsAppBatchResult
            {
                TotalRequested = numbersList.Count,
                TotalProcessed = results.Count,
                ExistingCount = results.Count(r => r.Exists),
                InactiveCount = results.Count(r => !r.Exists && r.IsSuccess),
                FailedCount = results.Count(r => !r.IsSuccess),
                Duration = sw.Elapsed,
                Results = results
            };
        }

        public Task<bool> IsReadyAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(State == WhatsAppConnectionState.Connected && _page != null && !_page.IsClosed);
        }

        public async Task DisconnectAsync(CancellationToken cancellationToken = default)
        {
            _connectionCts?.Cancel();
            try
            {
                if (_page != null && !_page.IsClosed)
                {
                    await _page.CloseAsync().ConfigureAwait(false);
                }
                if (_browser != null)
                {
                    await _browser.CloseAsync().ConfigureAwait(false);
                }
            }
            catch { }
            finally
            {
                State = WhatsAppConnectionState.Disconnected;
                CleanStaleSessionLocks(Path.GetFullPath(_options.AuthDirectory));
            }
        }

        private void EnsureConnected()
        {
            if (State != WhatsAppConnectionState.Connected || _page == null || _page.IsClosed)
            {
                throw new WhatsAppNotAuthenticatedException("WhatsApp client is not connected. Call ConnectAsync() first.");
            }
        }

        public async ValueTask DisposeAsync()
        {
            if (!_disposed)
            {
                _disposed = true;
                await DisconnectAsync().ConfigureAwait(false);
                _evalLock.Dispose();
            }
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
                _connectionCts?.Cancel();
                _browser?.Dispose();
                _evalLock.Dispose();
                CleanStaleSessionLocks(Path.GetFullPath(_options.AuthDirectory));
            }
        }
    }
}
