using System.Collections.Generic;
using System.Text;
using WhatsAppNumberChecker.Protocol.WABinary;
using Xunit;

namespace WhatsAppNumberChecker.Tests
{
    public class WABinaryTests
    {
        [Fact]
        public void BinaryNode_SimpleNode_EncodesAndDecodesCorrectly()
        {
            // Arrange
            var node = new BinaryNode("iq", new Dictionary<string, string>
            {
                { "id", "12345" },
                { "type", "get" },
                { "to", "s.whatsapp.net" }
            });

            // Act
            var encoded = BinaryEncoder.Encode(node);
            var decoded = BinaryDecoder.Decode(encoded);

            // Assert
            Assert.NotNull(decoded);
            Assert.Equal("iq", decoded.Tag);
            Assert.Equal("12345", decoded.GetAttribute("id"));
            Assert.Equal("get", decoded.GetAttribute("type"));
            Assert.Equal("s.whatsapp.net", decoded.GetAttribute("to"));
        }

        [Fact]
        public void BinaryNode_NestedChildren_EncodesAndDecodesCorrectly()
        {
            // Arrange
            var child1 = new BinaryNode("query", null, new[] { new BinaryNode("contact") });
            var child2 = new BinaryNode("user", new Dictionary<string, string> { { "jid", "15551234567@s.whatsapp.net" } });

            var parent = new BinaryNode("usync", new Dictionary<string, string> { { "mode", "query" } }, new[] { child1, child2 });

            // Act
            var encoded = BinaryEncoder.Encode(parent);
            var decoded = BinaryDecoder.Decode(encoded);

            // Assert
            Assert.NotNull(decoded);
            Assert.Equal("usync", decoded.Tag);
            Assert.Equal("query", decoded.GetAttribute("mode"));

            var children = decoded.GetChildren();
            Assert.Equal(2, children.Length);

            Assert.Equal("query", children[0].Tag);
            Assert.NotNull(children[0].GetChild("contact"));

            Assert.Equal("user", children[1].Tag);
            Assert.Equal("15551234567@s.whatsapp.net", children[1].GetAttribute("jid"));
        }

        [Fact]
        public void BinaryNode_WithBinaryContent_PreservesBytes()
        {
            // Arrange
            var payloadBytes = Encoding.UTF8.GetBytes("Binary XML Payload Data");
            var node = new BinaryNode("message", new Dictionary<string, string> { { "id", "msg_99" } }, payloadBytes);

            // Act
            var encoded = BinaryEncoder.Encode(node);
            var decoded = BinaryDecoder.Decode(encoded);

            // Assert
            Assert.NotNull(decoded);
            Assert.Equal("message", decoded.Tag);
            Assert.Equal("msg_99", decoded.GetAttribute("id"));

            var contentBytes = decoded.GetContentAsBytes();
            Assert.NotNull(contentBytes);
            Assert.Equal(payloadBytes, contentBytes);
            Assert.Equal("Binary XML Payload Data", decoded.GetContentAsString());
        }

        [Theory]
        [InlineData("iq", true)]
        [InlineData("usync", true)]
        [InlineData("s.whatsapp.net", true)]
        [InlineData("custom-nonexistent-token", false)]
        public void TokenDictionary_ResolvesStandardTokens(string token, bool shouldExist)
        {
            var found = TokenDictionary.TryGetTokenByte(token, out var tokenByte);
            Assert.Equal(shouldExist, found);
            if (shouldExist)
            {
                Assert.True(TokenDictionary.TryGetByteToken(tokenByte, out var recovered));
                Assert.Equal(token, recovered);
            }
        }
    }
}
