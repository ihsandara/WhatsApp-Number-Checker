using System;
using System.Collections.Generic;

namespace WhatsAppNumberChecker.Models
{
    /// <summary>
    /// Represents the aggregated result of a batch verification operation.
    /// </summary>
    public class WhatsAppBatchResult
    {
        /// <summary>
        /// Gets the total number of phone numbers requested for verification.
        /// </summary>
        public int TotalRequested { get; set; }

        /// <summary>
        /// Gets the total number of phone numbers successfully processed.
        /// </summary>
        public int TotalProcessed { get; set; }

        /// <summary>
        /// Gets the number of phone numbers confirmed to have active WhatsApp accounts.
        /// </summary>
        public int ExistingCount { get; set; }

        /// <summary>
        /// Gets the number of phone numbers confirmed to NOT have active WhatsApp accounts.
        /// </summary>
        public int InactiveCount { get; set; }

        /// <summary>
        /// Gets the number of lookups that failed due to validation or sidecar errors.
        /// </summary>
        public int FailedCount { get; set; }

        /// <summary>
        /// Gets the total duration elapsed for processing the batch.
        /// </summary>
        public TimeSpan Duration { get; set; }

        /// <summary>
        /// Gets the detailed individual verification result for each phone number.
        /// </summary>
        public IReadOnlyList<WhatsAppCheckResult> Results { get; set; } = Array.Empty<WhatsAppCheckResult>();
    }
}
