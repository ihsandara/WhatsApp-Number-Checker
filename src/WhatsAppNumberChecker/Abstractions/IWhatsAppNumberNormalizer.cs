namespace WhatsAppNumberChecker.Abstractions
{
    /// <summary>
    /// Contract for sanitizing and validating phone numbers into pure digit-only strings suitable for WhatsApp queries.
    /// </summary>
    public interface IWhatsAppNumberNormalizer
    {
        /// <summary>
        /// Sanitizes and validates a raw phone number.
        /// </summary>
        /// <param name="rawPhoneNumber">The input phone number (e.g. "+1 (555) 123-4567").</param>
        /// <returns>A normalized, digits-only phone number string (e.g. "15551234567").</returns>
        /// <exception cref="Exceptions.WhatsAppValidationException">Thrown if the phone number is null, empty, or fails E.164 length criteria.</exception>
        string Normalize(string rawPhoneNumber);

        /// <summary>
        /// Attempts to sanitize and validate a raw phone number without throwing exceptions.
        /// </summary>
        /// <param name="rawPhoneNumber">The input phone number.</param>
        /// <param name="normalizedNumber">When this method returns, contains the normalized digits, or <c>null</c> on failure.</param>
        /// <param name="errorMessage">When this method returns, contains an error description if validation failed.</param>
        /// <returns><c>true</c> if normalization succeeded; otherwise <c>false</c>.</returns>
        bool TryNormalize(string rawPhoneNumber, out string? normalizedNumber, out string? errorMessage);
    }
}
