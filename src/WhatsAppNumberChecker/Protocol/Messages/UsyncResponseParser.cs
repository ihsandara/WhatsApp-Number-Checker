using System;
using System.Collections.Generic;
using WhatsAppNumberChecker.Models;
using WhatsAppNumberChecker.Protocol.WABinary;

namespace WhatsAppNumberChecker.Protocol.Messages
{
    /// <summary>
    /// Parses incoming WhatsApp usync contact verification result stanzas.
    /// </summary>
    public static class UsyncResponseParser
    {
        public static List<WhatsAppCheckResult> ParseResponse(BinaryNode rootNode, string originalInput = "")
        {
            var results = new List<WhatsAppCheckResult>();
            if (rootNode == null) return results;

            var usyncNode = rootNode.GetChild("usync") ?? rootNode;
            var listNode = usyncNode.GetChild("list");
            if (listNode == null) return results;

            foreach (var userNode in listNode.GetChildren())
            {
                if (!string.Equals(userNode.Tag, "user", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var jid = userNode.GetAttribute("jid");
                var contactNode = userNode.GetChild("contact");

                // In WhatsApp protocol: <contact type="in" /> indicates number exists on WA
                // <contact type="out" /> indicates number is NOT on WA
                var type = contactNode?.GetAttribute("type") ?? userNode.GetAttribute("type");
                bool isRegistered = string.Equals(type, "in", StringComparison.OrdinalIgnoreCase) || !string.IsNullOrEmpty(jid);

                string normalizedNumber = string.Empty;
                if (!string.IsNullOrEmpty(jid))
                {
                    int atIdx = jid!.IndexOf('@');
                    normalizedNumber = atIdx > 0 ? jid.Substring(0, atIdx) : jid;
                    int colonIdx = normalizedNumber.IndexOf(':');
                    if (colonIdx > 0) normalizedNumber = normalizedNumber.Substring(0, colonIdx);
                }
                else if (contactNode != null && contactNode.Content is string contactText)
                {
                    normalizedNumber = contactText.Replace("+", "").Trim();
                }

                results.Add(new WhatsAppCheckResult
                {
                    InputNumber = originalInput,
                    NormalizedNumber = normalizedNumber,
                    Exists = isRegistered,
                    Jid = isRegistered ? jid : null,
                    CheckedAtUtc = DateTime.UtcNow
                });
            }

            return results;
        }
    }
}
