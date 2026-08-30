using System;

namespace WhatsAppNumberChecker.Models
{
    /// <summary>
    /// Configuration settings for batch number checking operations.
    /// </summary>
    public class WhatsAppBatchOptions
    {
        /// <summary>
        /// Gets or sets the delay between consecutive lookups to prevent triggering WhatsApp anti-spam rate limits.
        /// Defaults to 750 milliseconds.
        /// </summary>
        public TimeSpan DelayBetweenChecks { get; set; } = TimeSpan.FromMilliseconds(750);

        /// <summary>
        /// Gets or sets the random jitter variation applied to <see cref="DelayBetweenChecks"/> to simulate natural usage patterns.
        /// Defaults to 250 milliseconds (e.g., delay +/- jitter).
        /// </summary>
        public TimeSpan Jitter { get; set; } = TimeSpan.FromMilliseconds(250);

        /// <summary>
        /// Gets or sets whether the batch process should continue if an individual number lookup encounters an error.
        /// Defaults to <c>true</c>.
        /// </summary>
        public bool ContinueOnError { get; set; } = true;

        /// <summary>
        /// Gets or sets an optional progress reporter callback for monitoring progress in real-time.
        /// </summary>
        public IProgress<WhatsAppBatchProgress>? Progress { get; set; }
    }
}
