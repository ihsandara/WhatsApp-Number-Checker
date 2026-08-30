using System;
using System.Net;

namespace WhatsAppNumberChecker.Exceptions
{
    /// <summary>
    /// Exception thrown when the Baileys Node.js sidecar service cannot be reached (connection refused, network error, or timeout).
    /// </summary>
    public class WhatsAppSidecarUnavailableException : WhatsAppCheckerException
    {
        /// <summary>
        /// Gets the URI of the sidecar endpoint that failed to respond.
        /// </summary>
        public Uri? SidecarUri { get; }

        /// <summary>
        /// Initializes a new instance of <see cref="WhatsAppSidecarUnavailableException"/>.
        /// </summary>
        public WhatsAppSidecarUnavailableException(string message, Uri? sidecarUri = null, Exception? innerException = null)
            : base(message, HttpStatusCode.ServiceUnavailable, "SIDECAR_UNAVAILABLE", innerException)
        {
            SidecarUri = sidecarUri;
        }
    }
}
