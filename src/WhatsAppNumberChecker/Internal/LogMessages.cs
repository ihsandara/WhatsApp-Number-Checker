using System;
using Microsoft.Extensions.Logging;

namespace WhatsAppNumberChecker.Internal
{
    internal static class LogMessages
    {
        public static readonly EventId CheckStartedEvent = new EventId(1001, "CheckStarted");
        public static readonly EventId CheckCompletedEvent = new EventId(1002, "CheckCompleted");
        public static readonly EventId BatchStartedEvent = new EventId(1003, "BatchStarted");
        public static readonly EventId BatchProgressEvent = new EventId(1004, "BatchProgress");
        public static readonly EventId BatchCompletedEvent = new EventId(1005, "BatchCompleted");
        public static readonly EventId SidecarUnavailableEvent = new EventId(2001, "SidecarUnavailable");
        public static readonly EventId NotAuthenticatedEvent = new EventId(2002, "NotAuthenticated");
        public static readonly EventId RateLimitedEvent = new EventId(2003, "RateLimited");
        public static readonly EventId ValidationErrorEvent = new EventId(2004, "ValidationError");

        private static readonly Action<ILogger, string, Exception?> _checkStarted =
            LoggerMessage.Define<string>(
                LogLevel.Debug,
                CheckStartedEvent,
                "Starting WhatsApp lookup for phone number: {PhoneNumber}");

        private static readonly Action<ILogger, string, bool, string?, double, Exception?> _checkCompleted =
            LoggerMessage.Define<string, bool, string?, double>(
                LogLevel.Information,
                CheckCompletedEvent,
                "WhatsApp lookup finished for {PhoneNumber}: Exists={Exists}, JID={Jid} in {ElapsedMs:F1}ms");

        private static readonly Action<ILogger, int, double, Exception?> _batchStarted =
            LoggerMessage.Define<int, double>(
                LogLevel.Information,
                BatchStartedEvent,
                "Starting WhatsApp batch check for {Count} numbers with base delay of {DelayMs}ms");

        private static readonly Action<ILogger, int, int, double, Exception?> _batchProgress =
            LoggerMessage.Define<int, int, double>(
                LogLevel.Debug,
                BatchProgressEvent,
                "WhatsApp batch progress: {Processed}/{Total} ({Percentage:F1}%)");

        private static readonly Action<ILogger, int, int, int, double, Exception?> _batchCompleted =
            LoggerMessage.Define<int, int, int, double>(
                LogLevel.Information,
                BatchCompletedEvent,
                "WhatsApp batch check completed: {ExistingCount}/{Total} active numbers ({FailedCount} failed) in {TotalDurationMs:F0}ms");

        private static readonly Action<ILogger, Uri?, Exception?> _sidecarUnavailable =
            LoggerMessage.Define<Uri?>(
                LogLevel.Error,
                SidecarUnavailableEvent,
                "WhatsApp sidecar at {Uri} is unavailable or connection was refused");

        private static readonly Action<ILogger, string, Exception?> _notAuthenticated =
            LoggerMessage.Define<string>(
                LogLevel.Warning,
                NotAuthenticatedEvent,
                "WhatsApp sidecar is running but unauthenticated (Status: {Status}). QR code scan required.");

        private static readonly Action<ILogger, double?, Exception?> _rateLimited =
            LoggerMessage.Define<double?>(
                LogLevel.Warning,
                RateLimitedEvent,
                "WhatsApp rate limit hit. Recommended retry delay: {RetryAfterSeconds}s");

        public static void CheckStarted(ILogger logger, string phoneNumber) =>
            _checkStarted(logger, phoneNumber, null);

        public static void CheckCompleted(ILogger logger, string phoneNumber, bool exists, string? jid, double elapsedMs) =>
            _checkCompleted(logger, phoneNumber, exists, jid, elapsedMs, null);

        public static void BatchStarted(ILogger logger, int count, double delayMs) =>
            _batchStarted(logger, count, delayMs, null);

        public static void BatchProgress(ILogger logger, int processed, int total, double percentage) =>
            _batchProgress(logger, processed, total, percentage, null);

        public static void BatchCompleted(ILogger logger, int existingCount, int total, int failedCount, double totalDurationMs) =>
            _batchCompleted(logger, existingCount, total, failedCount, totalDurationMs, null);

        public static void SidecarUnavailable(ILogger logger, Uri? uri, Exception? ex) =>
            _sidecarUnavailable(logger, uri, ex);

        public static void NotAuthenticated(ILogger logger, string status) =>
            _notAuthenticated(logger, status, null);

        public static void RateLimited(ILogger logger, TimeSpan? retryAfter) =>
            _rateLimited(logger, retryAfter?.TotalSeconds, null);
    }
}
