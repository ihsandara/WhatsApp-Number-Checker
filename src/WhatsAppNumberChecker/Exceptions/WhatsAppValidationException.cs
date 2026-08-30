using System.Net;

namespace WhatsAppNumberChecker.Exceptions
{
    /// <summary>
    /// Exception thrown when a phone number format does not conform to valid numeric E.164 requirements.
    /// </summary>
    public class WhatsAppValidationException : WhatsAppCheckerException
    {
        /// <summary>
        /// Gets the invalid raw phone number string that triggered the validation failure.
        /// </summary>
        public string InvalidNumber { get; }

        /// <summary>
        /// Initializes a new instance of <see cref="WhatsAppValidationException"/>.
        /// </summary>
        public WhatsAppValidationException(string message, string invalidNumber)
            : base(message, HttpStatusCode.BadRequest, "INVALID_PHONE_NUMBER")
        {
            InvalidNumber = invalidNumber;
        }
    }
}
