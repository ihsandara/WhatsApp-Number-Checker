using System;
using System.Text.Json.Serialization;

namespace WhatsAppNumberChecker.Models
{
    /// <summary>
    /// Represents the verification result for an individual phone number lookup.
    /// </summary>
    public class WhatsAppCheckResult
    {
        /// <summary>
        /// Gets or sets the original raw input string provided to the lookup method.
        /// </summary>
        public string InputNumber { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the sanitized, normalized numeric phone number.
        /// </summary>
        [JsonPropertyName("number")]
        public string NormalizedNumber { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets whether the phone number is registered with an active WhatsApp account.
        /// </summary>
        [JsonPropertyName("exists")]
        public bool Exists { get; set; }

        /// <summary>
        /// Gets or sets the WhatsApp Jabber ID (JID) if registered (e.g. "15551234567@s.whatsapp.net"); otherwise <c>null</c>.
        /// </summary>
        [JsonPropertyName("jid")]
        public string? Jid { get; set; }

        /// <summary>
        /// Gets or sets the UTC timestamp when the lookup was performed.
        /// </summary>
        [JsonPropertyName("timestamp")]
        public DateTime CheckedAtUtc { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Gets or sets any error message encountered if this check failed during a batch operation.
        /// </summary>
        [JsonPropertyName("error")]
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// Gets whether the lookup succeeded without errors.
        /// </summary>
        [JsonIgnore]
        public bool IsSuccess => string.IsNullOrEmpty(ErrorMessage);
    }
}
