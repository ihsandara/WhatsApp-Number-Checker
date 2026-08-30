using System;
using System.Collections.Generic;

namespace WhatsAppNumberChecker.Protocol.WABinary
{
    /// <summary>
    /// WhatsApp WABinary Token Dictionary lookup tables.
    /// </summary>
    public static class TokenDictionary
    {
        public static readonly string[] SingleByteTokens = new[]
        {
            "", "", "", "account", "ack", "action", "active", "add", "after", "all",
            "allow", "apple", "auth", "author", "available", "bad-protocol", "bad-request", "before", "body", "broadcast",
            "cancel", "category", "challenge", "chat", "clean", "code", "composing", "config", "contacts", "count",
            "create", "creation", "debug", "default", "delete", "delivery", "delta", "deny", "digest", "dirty",
            "duplicate", "elapsed", "enable", "encoding", "error", "event", "expiration", "expired", "fail", "failure",
            "false", "favorites", "feature", "features", "feature-not-implemented", "field", "first", "free", "from", "g.us",
            "get", "google", "group", "groups", "have", "hlr", "http", "id", "image", "in",
            "index", "invaliddomain", "item", "items", "iq", "jid", "key", "kind", "last", "leave",
            "list", "max", "media", "message", "message_acks", "missing", "modify", "name", "nat", "never",
            "new", "next", "no", "no-such-biz", "none", "nonce", "not-acceptable", "not-allowed", "not-authorized", "notification",
            "notify", "off", "offline", "order", "owner", "owning", "paid", "participant", "participants", "passive",
            "password", "paused", "picture", "pin", "ping", "platform", "port", "presence", "preview", "probe",
            "prop", "props", "query", "raw", "read", "readreceipts", "reason", "receipt", "relay", "remote-resource",
            "remove", "request", "required", "resource", "resource-constraint", "response", "result", "retry", "s.whatsapp.net", "seconds",
            "server", "server-error", "service-unavailable", "set", "show", "silent", "stat", "status", "stream:error", "stream:features",
            "subject", "subscribe", "success", "sync", "t", "text", "timeout", "to", "true", "type",
            "unavailable", "unsubscribe", "uri", "url", "user", "usync", "v", "value", "version", "voip",
            "wait", "w:b", "w:biz", "w:c", "w:g", "w:p", "w:profile", "w:pv", "w:r", "w:stats",
            "w:t", "xmlns", "xmlns:bk", "1", "2", "3", "0", "contact", "interactive", "query"
        };

        private static readonly Dictionary<string, byte> TokenToByteMap = new Dictionary<string, byte>(StringComparer.Ordinal);
        private static readonly Dictionary<byte, string> ByteToTokenMap = new Dictionary<byte, string>();

        static TokenDictionary()
        {
            for (int i = 0; i < SingleByteTokens.Length; i++)
            {
                var token = SingleByteTokens[i];
                if (!string.IsNullOrEmpty(token))
                {
                    if (!TokenToByteMap.ContainsKey(token))
                    {
                        TokenToByteMap[token] = (byte)i;
                    }
                    ByteToTokenMap[(byte)i] = token;
                }
            }
        }

        public static bool TryGetTokenByte(string token, out byte tokenByte)
        {
            return TokenToByteMap.TryGetValue(token, out tokenByte);
        }

        public static bool TryGetByteToken(byte tokenByte, out string token)
        {
            return ByteToTokenMap.TryGetValue(tokenByte, out token!);
        }
    }
}
