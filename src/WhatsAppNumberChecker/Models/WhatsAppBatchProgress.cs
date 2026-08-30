using System;

namespace WhatsAppNumberChecker.Models
{
    /// <summary>
    /// Progress notification emitted during a throttled batch execution.
    /// </summary>
    public class WhatsAppBatchProgress
    {
        /// <summary>
        /// Total number of phone numbers in the batch.
        /// </summary>
        public int Total { get; set; }

        /// <summary>
        /// Total number of phone numbers processed so far.
        /// </summary>
        public int Processed { get; set; }

        /// <summary>
        /// Total number of verified active WhatsApp accounts discovered so far.
        /// </summary>
        public int ExistingCount { get; set; }

        /// <summary>
        /// Total number of inactive/unregistered numbers discovered so far.
        /// </summary>
        public int InactiveCount { get; set; }

        /// <summary>
        /// Total number of lookups that failed due to validation or network errors.
        /// </summary>
        public int FailedCount { get; set; }

        /// <summary>
        /// The result of the most recently processed phone number.
        /// </summary>
        public WhatsAppCheckResult LatestResult { get; set; } = null!;

        /// <summary>
        /// Percentage of completion (0.0 to 100.0).
        /// </summary>
        public double Percentage => Total > 0 ? ((double)Processed / Total) * 100.0 : 0.0;
    }
}
