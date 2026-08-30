using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace WhatsAppNumberChecker.Protocol.WABinary
{
    /// <summary>
    /// Encodes <see cref="BinaryNode"/> stanzas into WhatsApp WABinary tokenized byte streams.
    /// </summary>
    public static class BinaryEncoder
    {
        public const byte List8 = 0xF8;
        public const byte List16 = 0xF9;
        public const byte JidPair = 0xFA;
        public const byte Hex8 = 0xFF;
        public const byte Binary8 = 0xFC;
        public const byte Binary20 = 0xFD;
        public const byte Binary32 = 0xFE;
        public const byte Nibble8 = 0xFF;

        public static byte[] Encode(BinaryNode node)
        {
            if (node == null) throw new ArgumentNullException(nameof(node));

            using var ms = new MemoryStream();
            WriteNode(ms, node);
            return ms.ToArray();
        }

        private static void WriteNode(MemoryStream ms, BinaryNode node)
        {
            int attrCount = node.Attributes != null ? node.Attributes.Count : 0;
            bool hasContent = node.Content != null;
            int listSize = 1 + (attrCount * 2) + (hasContent ? 1 : 0);

            WriteListHeader(ms, listSize);
            WriteString(ms, node.Tag);

            if (node.Attributes != null)
            {
                foreach (var kvp in node.Attributes)
                {
                    WriteString(ms, kvp.Key);
                    WriteString(ms, kvp.Value);
                }
            }

            if (hasContent)
            {
                if (node.Content is BinaryNode[] children)
                {
                    WriteListHeader(ms, children.Length);
                    foreach (var child in children)
                    {
                        WriteNode(ms, child);
                    }
                }
                else if (node.Content is IEnumerable<BinaryNode> enumChildren)
                {
                    var list = new List<BinaryNode>(enumChildren);
                    WriteListHeader(ms, list.Count);
                    foreach (var child in list)
                    {
                        WriteNode(ms, child);
                    }
                }
                else if (node.Content is byte[] bytes)
                {
                    WriteBytes(ms, bytes);
                }
                else if (node.Content is string text)
                {
                    WriteBytes(ms, Encoding.UTF8.GetBytes(text));
                }
            }
        }

        private static void WriteListHeader(MemoryStream ms, int count)
        {
            if (count < 256)
            {
                ms.WriteByte(List8);
                ms.WriteByte((byte)count);
            }
            else
            {
                ms.WriteByte(List16);
                ms.WriteByte((byte)(count >> 8));
                ms.WriteByte((byte)count);
            }
        }

        private static void WriteString(MemoryStream ms, string str)
        {
            if (string.IsNullOrEmpty(str))
            {
                WriteBytes(ms, Array.Empty<byte>());
                return;
            }

            if (TokenDictionary.TryGetTokenByte(str, out byte tokenByte))
            {
                ms.WriteByte(tokenByte);
            }
            else
            {
                WriteBytes(ms, Encoding.UTF8.GetBytes(str));
            }
        }

        private static void WriteBytes(MemoryStream ms, byte[] bytes)
        {
            int length = bytes.Length;
            if (length < 256)
            {
                ms.WriteByte(Binary8);
                ms.WriteByte((byte)length);
            }
            else if (length < 1048576) // 20-bit
            {
                ms.WriteByte(Binary20);
                ms.WriteByte((byte)((length >> 16) & 0x0F));
                ms.WriteByte((byte)((length >> 8) & 0xFF));
                ms.WriteByte((byte)(length & 0xFF));
            }
            else
            {
                ms.WriteByte(Binary32);
                ms.WriteByte((byte)((length >> 24) & 0xFF));
                ms.WriteByte((byte)((length >> 16) & 0xFF));
                ms.WriteByte((byte)((length >> 8) & 0xFF));
                ms.WriteByte((byte)(length & 0xFF));
            }

            ms.Write(bytes, 0, bytes.Length);
        }
    }
}
