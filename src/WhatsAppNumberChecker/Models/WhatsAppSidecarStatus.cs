using System;
using System.Text.Json.Serialization;

namespace WhatsAppNumberChecker.Models
{
    /// <summary>
    /// Represents the status and authentication information returned by the Baileys sidecar service.
    /// </summary>
    public class WhatsAppSidecarStatus
    {
        /// <summary>
        /// Gets or sets the mapped connection state.
        /// </summary>
        public WhatsAppConnectionState State { get; set; } = WhatsAppConnectionState.Unknown;

        /// <summary>
        /// Gets or sets the raw status string returned by the sidecar.
        /// </summary>
        [JsonPropertyName("status")]
        public string RawStatus { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets whether the sidecar is currently authenticated with WhatsApp.
        /// </summary>
        [JsonPropertyName("authenticated")]
        public bool IsAuthenticated { get; set; }

        /// <summary>
        /// Gets or sets the WhatsApp user information if authenticated.
        /// </summary>
        [JsonPropertyName("user")]
        public WhatsAppUserInfo? User { get; set; }

        /// <summary>
        /// Gets or sets the raw ASCII QR code string if authentication is required.
        /// </summary>
        [JsonPropertyName("qrCode")]
        public string? QrCode { get; set; }

        /// <summary>
        /// Gets or sets the Base64 Data URL (data:image/png;base64,...) for the QR code image.
        /// </summary>
        [JsonPropertyName("qrCodeDataUrl")]
        public string? QrCodeDataUrl { get; set; }

        /// <summary>
        /// Gets or sets the 8-digit pairing code if requested.
        /// </summary>
        [JsonPropertyName("pairingCode")]
        public string? PairingCode { get; set; }

        /// <summary>
        /// Gets or sets the UTC timestamp of the status report.
        /// </summary>
        [JsonPropertyName("timestamp")]
        public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// User profile information for the authenticated WhatsApp session.
    /// </summary>
    public class WhatsAppUserInfo
    {
        /// <summary>
        /// Gets or sets the WhatsApp user JID (e.g., "15551234567:12@s.whatsapp.net").
        /// </summary>
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        /// <summary>
        /// Gets or sets the WhatsApp display name if available.
        /// </summary>
        [JsonPropertyName("name")]
        public string? Name { get; set; }
    }
}
