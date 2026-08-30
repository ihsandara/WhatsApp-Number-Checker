using System;
using WhatsAppNumberChecker.Exceptions;
using WhatsAppNumberChecker.Internal;
using Xunit;

namespace WhatsAppNumberChecker.Tests
{
    public class NumberNormalizerTests
    {
        private readonly WhatsAppNumberNormalizer _normalizer = new WhatsAppNumberNormalizer();

        [Theory]
        [InlineData("+1 (555) 123-4567", "15551234567")]
        [InlineData("+44 7911 123456", "447911123456")]
        [InlineData("971501234567", "971501234567")]
        [InlineData("+971-50-123-4567", "971501234567")]
        [InlineData("  +1.555.123.4567  ", "15551234567")]
        [InlineData("+33 (0) 6 12 34 56 78", "330612345678")]
        [InlineData("123456", "123456")] // Min length
        [InlineData("123456789012345", "123456789012345")] // Max 15 length
        public void Normalize_WithValidInputs_ReturnsDigitsOnly(string input, string expected)
        {
            var result = _normalizer.Normalize(input);
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData(null)]
        public void Normalize_WithNullOrWhitespace_ThrowsValidationException(string? input)
        {
            Assert.Throws<WhatsAppValidationException>(() => _normalizer.Normalize(input!));
        }

        [Theory]
        [InlineData("12345")] // Too short (< 6)
        [InlineData("1234567890123456")] // Too long (> 15)
        [InlineData("+1-555-CALL-NOW")] // Contains letters
        [InlineData("+1-555-$1234")] // Contains invalid symbols
        public void Normalize_WithInvalidFormat_ThrowsValidationException(string input)
        {
            var ex = Assert.Throws<WhatsAppValidationException>(() => _normalizer.Normalize(input));
            Assert.Equal(input, ex.InvalidNumber);
        }

        [Fact]
        public void TryNormalize_ReturnsExpectedSuccessAndErrorMessage()
        {
            Assert.True(_normalizer.TryNormalize("+15551234567", out var normalized, out var error));
            Assert.Equal("15551234567", normalized);
            Assert.Null(error);

            Assert.False(_normalizer.TryNormalize("invalid", out var failedNormalized, out var failedError));
            Assert.Null(failedNormalized);
            Assert.NotNull(failedError);
        }
    }
}
