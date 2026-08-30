using System;
using System.Net;

namespace WhatsAppNumberChecker.Exceptions
{
    /// <summary>
    /// Base exception for all errors originating from the WhatsApp Number Checker library and sidecar.
    /// </summary>
    public class WhatsAppCheckerException : Exception
    {
        /// <summary>
        /// Gets the HTTP status code returned by the sidecar, if applicable.
        /// </summary>
        public HttpStatusCode? StatusCode { get; }

        /// <summary>
        /// Gets the machine-readable error code string returned by the sidecar.
        /// </summary>
        public string? ErrorCode { get; }

        /// <summary>
        /// Initializes a new instance of <see cref="WhatsAppCheckerException"/>.
        /// </summary>
        public WhatsAppCheckerException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of <see cref="WhatsAppCheckerException"/> with an inner exception.
        /// </summary>
        public WhatsAppCheckerException(string message, Exception? innerException)
            : base(message, innerException)
        {
        }

        /// <summary>
        /// Initializes a new instance of <see cref="WhatsAppCheckerException"/> with status code and error code.
        /// </summary>
        public WhatsAppCheckerException(string message, HttpStatusCode? statusCode, string? errorCode, Exception? innerException = null)
            : base(message, innerException)
        {
            StatusCode = statusCode;
            ErrorCode = errorCode;
        }
    }
}
