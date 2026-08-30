using System;

namespace WhatsAppNumberChecker.Options
{
    /// <summary>
    /// Configuration options for the pure C# native WhatsApp client.
    /// </summary>
    public class WhatsAppCheckerOptions
    {
        /// <summary>
        /// Configuration section name used in appsettings.json.
        /// </summary>
        public const string SectionName = "WhatsAppChecker";

        /// <summary>
        /// Gets or sets the path to the local directory where WhatsApp session credentials and cryptographic keys are stored.
        /// Defaults to "./auth_data".
        /// </summary>
        public string AuthDirectory { get; set; } = "./auth_data";

        /// <summary>
        /// Gets or sets the WhatsApp Web WebSocket URL. Defaults to "wss://web.whatsapp.com/ws/chat".
        /// </summary>
        public Uri WebSocketUrl { get; set; } = new Uri("wss://web.whatsapp.com/ws/chat");

        /// <summary>
        /// Gets or sets the HTTP Origin header sent during WebSocket handshake. Defaults to "https://web.whatsapp.com".
        /// </summary>
        public string OriginUrl { get; set; } = "https://web.whatsapp.com";

        /// <summary>
        /// Gets or sets the User-Agent header sent during WebSocket handshake.
        /// </summary>
        public string UserAgent { get; set; } = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/128.0.0.0 Safari/537.36";

        /// <summary>
        /// Gets or sets the timeout for establishing socket and Noise handshakes. Defaults to 30 seconds.
        /// </summary>
        public TimeSpan ConnectTimeout { get; set; } = TimeSpan.FromSeconds(30);

        /// <summary>
        /// Gets or sets the timeout for individual number lookup queries. Defaults to 20 seconds.
        /// </summary>
        public TimeSpan QueryTimeout { get; set; } = TimeSpan.FromSeconds(20);

        /// <summary>
        /// Gets or sets the default base delay applied between lookups during batch operations. Defaults to 750 milliseconds.
        /// </summary>
        public TimeSpan DefaultBatchDelay { get; set; } = TimeSpan.FromMilliseconds(750);

        /// <summary>
        /// Gets or sets the random jitter variation applied during batch operations. Defaults to 250 milliseconds.
        /// </summary>
        public TimeSpan DefaultBatchJitter { get; set; } = TimeSpan.FromMilliseconds(250);

        /// <summary>
        /// Gets or sets whether to run the WhatsApp Web engine in headless mode. Defaults to true.
        /// </summary>
        public bool Headless { get; set; } = true;

        /// <summary>
        /// Gets or sets an optional custom browser executable path (e.g. Chrome or Edge). If null, Chrome is automatically managed.
        /// </summary>
        public string? ExecutablePath { get; set; }

        /// <summary>
        /// Gets or sets whether to automatically download Chromium if not found locally. Defaults to true.
        /// </summary>
        public bool AutoDownloadBrowser { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to automatically attempt reconnecting on unexpected drops. Defaults to true.
        /// </summary>
        public bool AutoReconnect { get; set; } = true;
    }
}
