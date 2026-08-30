using System;
using System.Collections.Generic;
using WhatsAppNumberChecker.Protocol.WABinary;

namespace WhatsAppNumberChecker.Protocol.Messages
{
    /// <summary>
    /// Builds WhatsApp Multi-Device Usync contact verification stanzas.
    /// </summary>
    public static class UsyncQueryBuilder
    {
        public static BinaryNode BuildContactCheckStanza(string queryId, IEnumerable<string> normalizedPhoneNumbers)
        {
            if (string.IsNullOrWhiteSpace(queryId)) throw new ArgumentNullException(nameof(queryId));
            if (normalizedPhoneNumbers == null) throw new ArgumentNullException(nameof(normalizedPhoneNumbers));

            var userNodes = new List<BinaryNode>();
            foreach (var num in normalizedPhoneNumbers)
            {
                if (string.IsNullOrWhiteSpace(num)) continue;
                var formatted = num.StartsWith("+") ? num : "+" + num;

                userNodes.Add(new BinaryNode("user", null, new[]
                {
                    new BinaryNode("contact", null, formatted)
                }));
            }

            var usyncNode = new BinaryNode("usync", new Dictionary<string, string>
            {
                { "sid", DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString() },
                { "mode", "query" },
                { "last", "true" },
                { "index", "0" },
                { "context", "interactive" }
            }, new[]
            {
                new BinaryNode("query", null, new[] { new BinaryNode("contact") }),
                new BinaryNode("list", null, userNodes.ToArray())
            });

            return new BinaryNode("iq", new Dictionary<string, string>
            {
                { "id", queryId },
                { "to", "s.whatsapp.net" },
                { "type", "get" },
                { "xmlns", "usync" }
            }, new[] { usyncNode });
        }
    }
}
