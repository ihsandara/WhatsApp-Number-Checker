using System.Net;
using WhatsAppNumberChecker.Models;

namespace WhatsAppNumberChecker.Exceptions
{
    /// <summary>
    /// Exception thrown when the sidecar is running but has not completed WhatsApp authentication (QR scan or pairing code required).
    /// </summary>
    public class WhatsAppNotAuthenticatedException : WhatsAppCheckerException
    {
        /// <summary>
        /// Gets the current reported state of the sidecar.
        /// </summary>
        public WhatsAppConnectionState CurrentState { get; }

        /// <summary>
        /// Gets whether a QR code is currently available to be scanned in the sidecar.
        /// </summary>
        public bool QrCodeAvailable { get; }

        /// <summary>
        /// Initializes a new instance of <see cref="WhatsAppNotAuthenticatedException"/>.
        /// </summary>
        public WhatsAppNotAuthenticatedException(
            string message,
            WhatsAppConnectionState currentState = WhatsAppConnectionState.ScanQrCode,
            bool qrCodeAvailable = false)
            : base(message, HttpStatusCode.ServiceUnavailable, "NOT_AUTHENTICATED")
        {
            CurrentState = currentState;
            QrCodeAvailable = qrCodeAvailable;
        }
    }
}
