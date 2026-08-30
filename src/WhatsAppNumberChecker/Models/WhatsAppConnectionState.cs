namespace WhatsAppNumberChecker.Models
{
    /// <summary>
    /// Represents the current connection state of the WhatsApp Baileys sidecar service.
    /// </summary>
    public enum WhatsAppConnectionState
    {
        /// <summary>
        /// The connection state is unknown or could not be determined.
        /// </summary>
        Unknown = 0,

        /// <summary>
        /// The sidecar is disconnected from WhatsApp.
        /// </summary>
        Disconnected = 1,

        /// <summary>
        /// The sidecar is currently attempting to establish a socket connection with WhatsApp.
        /// </summary>
        Connecting = 2,

        /// <summary>
        /// The sidecar requires QR code scanning or pairing code entry on a mobile device.
        /// </summary>
        ScanQrCode = 3,

        /// <summary>
        /// The sidecar is actively authenticated and ready to execute number verification checks.
        /// </summary>
        Connected = 4
    }
}
