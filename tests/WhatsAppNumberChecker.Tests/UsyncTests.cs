using System.Collections.Generic;
using WhatsAppNumberChecker.Protocol.Messages;
using WhatsAppNumberChecker.Protocol.WABinary;
using Xunit;

namespace WhatsAppNumberChecker.Tests
{
    public class UsyncTests
    {
        [Fact]
        public void BuildContactCheckStanza_CreatesValidStanzaHierarchy()
        {
            var numbers = new[] { "15551234567", "447911123456" };
            var stanza = UsyncQueryBuilder.BuildContactCheckStanza("query_001", numbers);

            Assert.Equal("iq", stanza.Tag);
            Assert.Equal("query_001", stanza.GetAttribute("id"));
            Assert.Equal("s.whatsapp.net", stanza.GetAttribute("to"));
            Assert.Equal("get", stanza.GetAttribute("type"));
            Assert.Equal("usync", stanza.GetAttribute("xmlns"));

            var usyncChild = stanza.GetChild("usync");
            Assert.NotNull(usyncChild);
            Assert.Equal("query", usyncChild.GetAttribute("mode"));
            Assert.Equal("interactive", usyncChild.GetAttribute("context"));

            var listChild = usyncChild.GetChild("list");
            Assert.NotNull(listChild);

            var userChildren = listChild.GetChildren();
            Assert.Equal(2, userChildren.Length);

            Assert.Equal("user", userChildren[0].Tag);
            var contact1 = userChildren[0].GetChild("contact");
            Assert.NotNull(contact1);
            Assert.Equal("+15551234567", contact1.Content);

            Assert.Equal("user", userChildren[1].Tag);
            var contact2 = userChildren[1].GetChild("contact");
            Assert.NotNull(contact2);
            Assert.Equal("+447911123456", contact2.Content);
        }

        [Fact]
        public void ParseResponse_WhenUserIsActive_ReturnsExistsTrueAndJid()
        {
            // Simulate WhatsApp response: <iq><usync><list><user jid="15551234567@s.whatsapp.net"><contact type="in"/></user></list></usync></iq>
            var contactNode = new BinaryNode("contact", new Dictionary<string, string> { { "type", "in" } });
            var userNode = new BinaryNode("user", new Dictionary<string, string>
            {
                { "jid", "15551234567@s.whatsapp.net" }
            }, new[] { contactNode });

            var listNode = new BinaryNode("list", null, new[] { userNode });
            var usyncNode = new BinaryNode("usync", null, new[] { listNode });
            var iqNode = new BinaryNode("iq", null, new[] { usyncNode });

            var results = UsyncResponseParser.ParseResponse(iqNode, "+1 555 123 4567");

            Assert.Single(results);
            Assert.True(results[0].Exists);
            Assert.Equal("15551234567@s.whatsapp.net", results[0].Jid);
            Assert.Equal("15551234567", results[0].NormalizedNumber);
            Assert.Equal("+1 555 123 4567", results[0].InputNumber);
        }

        [Fact]
        public void ParseResponse_WhenUserIsNotRegistered_ReturnsExistsFalse()
        {
            // Simulate WhatsApp response: <iq><usync><list><user><contact type="out">+15559999999</contact></user></list></usync></iq>
            var contactNode = new BinaryNode("contact", new Dictionary<string, string> { { "type", "out" } }, "+15559999999");
            var userNode = new BinaryNode("user", null, new[] { contactNode });
            var listNode = new BinaryNode("list", null, new[] { userNode });
            var usyncNode = new BinaryNode("usync", null, new[] { listNode });
            var iqNode = new BinaryNode("iq", null, new[] { usyncNode });

            var results = UsyncResponseParser.ParseResponse(iqNode, "+1 555 999 9999");

            Assert.Single(results);
            Assert.False(results[0].Exists);
            Assert.Null(results[0].Jid);
            Assert.Equal("15559999999", results[0].NormalizedNumber);
        }
    }
}
