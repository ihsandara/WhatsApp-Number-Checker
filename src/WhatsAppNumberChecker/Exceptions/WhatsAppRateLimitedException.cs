using System;
using System.Net;

namespace WhatsAppNumberChecker.Exceptions
{
    /// <summary>
    /// Exception thrown when WhatsApp or the sidecar enforces rate limiting.
    /// </summary>
    public class WhatsAppRateLimitedException : WhatsAppCheckerException
    {
        /// <summary>
        /// Gets the recommended wait duration before retrying, if provided.
        /// </summary>
        public TimeSpan? RetryAfter { get; }

        /// <summary>
        /// Initializes a new instance of <see cref="WhatsAppRateLimitedException"/>.
        /// </summary>
        public WhatsAppRateLimitedException(string message, TimeSpan? retryAfter = null)
            : base(message, (HttpStatusCode)429, "RATE_LIMITED")
        {
            RetryAfter = retryAfter;
        }
    }
}
