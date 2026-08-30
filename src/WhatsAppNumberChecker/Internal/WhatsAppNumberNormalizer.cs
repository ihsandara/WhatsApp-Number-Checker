using System;
using System.Text;
using WhatsAppNumberChecker.Abstractions;
using WhatsAppNumberChecker.Exceptions;

namespace WhatsAppNumberChecker.Internal
{
    /// <summary>
    /// Default implementation of <see cref="IWhatsAppNumberNormalizer"/> that validates and extracts pure numeric digits.
    /// </summary>
    public class WhatsAppNumberNormalizer : IWhatsAppNumberNormalizer
    {
        public const int MinDigitLength = 6;
        public const int MaxDigitLength = 15;

        public string Normalize(string rawPhoneNumber)
        {
            if (TryNormalize(rawPhoneNumber, out var normalized, out var errorMessage))
            {
                return normalized!;
            }

            throw new WhatsAppValidationException(
                errorMessage ?? $"Invalid phone number: '{rawPhoneNumber}'",
                rawPhoneNumber ?? string.Empty);
        }

        public bool TryNormalize(string rawPhoneNumber, out string? normalizedNumber, out string? errorMessage)
        {
            normalizedNumber = null;
            errorMessage = null;

            if (string.IsNullOrWhiteSpace(rawPhoneNumber))
            {
                errorMessage = "Phone number cannot be null or whitespace.";
                return false;
            }

            var trimmed = rawPhoneNumber.Trim();
            var sb = new StringBuilder(trimmed.Length);

            for (int i = 0; i < trimmed.Length; i++)
            {
                char c = trimmed[i];

                if (c >= '0' && c <= '9')
                {
                    sb.Append(c);
                }
                else if (c == '+' && i == 0)
                {
                    // Allow leading +, but omit from raw WhatsApp query digits
                    continue;
                }
                else if (c == ' ' || c == '-' || c == '(' || c == ')' || c == '.' || c == '/')
                {
                    // Allow common phone number formatting separators
                    continue;
                }
                else
                {
                    errorMessage = $"Phone number contains invalid character '{c}'. Only digits and standard formatting symbols (+, -, (, ), ., space) are allowed.";
                    return false;
                }
            }

            if (sb.Length < MinDigitLength || sb.Length > MaxDigitLength)
            {
                errorMessage = $"Phone number must contain between {MinDigitLength} and {MaxDigitLength} digits. Found {sb.Length} digits.";
                return false;
            }

            normalizedNumber = sb.ToString();
            return true;
        }
    }
}
