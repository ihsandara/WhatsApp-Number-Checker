using System;
using System.Net;

namespace WhatsAppNumberChecker.Exceptions
{
    /// <summary>
    /// Exception thrown when the WebSocket connection or Noise cryptographic handshake with WhatsApp fails.
    /// </summary>
    public class WhatsAppConnectionException : WhatsAppCheckerException
    {
        public Uri? EndpointUri { get; }

        public WhatsAppConnectionException(string message, Uri? endpointUri = null, Exception? innerException = null)
            : base(message, HttpStatusCode.ServiceUnavailable, "CONNECTION_FAILED", innerException)
        {
            EndpointUri = endpointUri;
        }
    }
}
