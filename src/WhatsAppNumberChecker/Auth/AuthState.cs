using System;
using System.Text.Json.Serialization;

namespace WhatsAppNumberChecker.Auth
{
    /// <summary>
    /// Holds persisted session state and cryptographic keys for WhatsApp multi-device authentication.
    /// </summary>
    public class AuthState
    {
        [JsonPropertyName("noiseStaticPrivateKey")]
        public string? NoiseStaticPrivateKeyBase64 { get; set; }

        [JsonPropertyName("noiseStaticPublicKey")]
        public string? NoiseStaticPublicKeyBase64 { get; set; }

        [JsonPropertyName("identityPrivateKey")]
        public string? IdentityPrivateKeyBase64 { get; set; }

        [JsonPropertyName("identityPublicKey")]
        public string? IdentityPublicKeyBase64 { get; set; }

        [JsonPropertyName("advSecretKey")]
        public string? AdvSecretKeyBase64 { get; set; }

        [JsonPropertyName("registered")]
        public bool Registered { get; set; }

        [JsonPropertyName("meJid")]
        public string? MeJid { get; set; }

        [JsonPropertyName("meName")]
        public string? MeName { get; set; }

        [JsonPropertyName("lastConnectedUtc")]
        public DateTime? LastConnectedUtc { get; set; }

        [JsonIgnore]
        public byte[]? NoiseStaticPrivateKey => !string.IsNullOrEmpty(NoiseStaticPrivateKeyBase64)
            ? Convert.FromBase64String(NoiseStaticPrivateKeyBase64)
            : null;

        [JsonIgnore]
        public byte[]? NoiseStaticPublicKey => !string.IsNullOrEmpty(NoiseStaticPublicKeyBase64)
            ? Convert.FromBase64String(NoiseStaticPublicKeyBase64)
            : null;

        [JsonIgnore]
        public byte[]? IdentityPrivateKey => !string.IsNullOrEmpty(IdentityPrivateKeyBase64)
            ? Convert.FromBase64String(IdentityPrivateKeyBase64)
            : null;

        [JsonIgnore]
        public byte[]? IdentityPublicKey => !string.IsNullOrEmpty(IdentityPublicKeyBase64)
            ? Convert.FromBase64String(IdentityPublicKeyBase64)
            : null;
    }
}
