using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace WhatsAppNumberChecker.Protocol.WABinary
{
    /// <summary>
    /// Decodes WhatsApp WABinary tokenized byte streams into <see cref="BinaryNode"/> stanzas.
    /// </summary>
    public static class BinaryDecoder
    {
        public static BinaryNode Decode(byte[] buffer)
        {
            if (buffer == null || buffer.Length == 0)
            {
                throw new ArgumentException("Buffer cannot be null or empty.", nameof(buffer));
            }

            using var ms = new MemoryStream(buffer);
            return ReadNode(ms);
        }

        private static BinaryNode ReadNode(MemoryStream ms)
        {
            int listSize = ReadListSize(ms);
            var tag = ReadString(ms);

            var attributes = new Dictionary<string, string>();
            int readItems = 1;

            while (readItems < listSize)
            {
                if (readItems == listSize - 1 && ms.Position < ms.Length)
                {
                    // Peek if the next element is child nodes or binary content
                    int nextByte = ms.ReadByte();
                    ms.Seek(-1, SeekOrigin.Current);

                    if (nextByte == BinaryEncoder.List8 || nextByte == BinaryEncoder.List16)
                    {
                        // Child nodes
                        int childCount = ReadListSize(ms);
                        var children = new List<BinaryNode>(childCount);
                        for (int i = 0; i < childCount; i++)
                        {
                            children.Add(ReadNode(ms));
                        }
                        return new BinaryNode(tag, attributes, children.ToArray());
                    }
                    else if (nextByte == BinaryEncoder.Binary8 ||
                             nextByte == BinaryEncoder.Binary20 ||
                             nextByte == BinaryEncoder.Binary32)
                    {
                        var contentBytes = ReadBytes(ms);
                        return new BinaryNode(tag, attributes, contentBytes);
                    }
                    else
                    {
                        // String content or final attribute pair
                        var strContent = ReadString(ms);
                        return new BinaryNode(tag, attributes, strContent);
                    }
                }

                var key = ReadString(ms);
                var value = ReadString(ms);
                attributes[key] = value;
                readItems += 2;
            }

            return new BinaryNode(tag, attributes, null);
        }

        private static int ReadListSize(MemoryStream ms)
        {
            int tag = ms.ReadByte();
            if (tag == BinaryEncoder.List8)
            {
                return ms.ReadByte();
            }
            if (tag == BinaryEncoder.List16)
            {
                int b1 = ms.ReadByte();
                int b2 = ms.ReadByte();
                return (b1 << 8) | b2;
            }

            throw new InvalidOperationException($"Expected list tag (0xF8 or 0xF9) but found 0x{tag:X2}");
        }

        private static string ReadString(MemoryStream ms)
        {
            int b = ms.ReadByte();
            if (b == -1) throw new EndOfStreamException();

            if (TokenDictionary.TryGetByteToken((byte)b, out var token))
            {
                return token;
            }

            if (b == BinaryEncoder.Binary8 || b == BinaryEncoder.Binary20 || b == BinaryEncoder.Binary32)
            {
                ms.Seek(-1, SeekOrigin.Current);
                var bytes = ReadBytes(ms);
                return Encoding.UTF8.GetString(bytes);
            }

            return string.Empty;
        }

        private static byte[] ReadBytes(MemoryStream ms)
        {
            int tag = ms.ReadByte();
            int length;

            if (tag == BinaryEncoder.Binary8)
            {
                length = ms.ReadByte();
            }
            else if (tag == BinaryEncoder.Binary20)
            {
                int b1 = ms.ReadByte();
                int b2 = ms.ReadByte();
                int b3 = ms.ReadByte();
                length = ((b1 & 0x0F) << 16) | (b2 << 8) | b3;
            }
            else if (tag == BinaryEncoder.Binary32)
            {
                int b1 = ms.ReadByte();
                int b2 = ms.ReadByte();
                int b3 = ms.ReadByte();
                int b4 = ms.ReadByte();
                length = (b1 << 24) | (b2 << 16) | (b3 << 8) | b4;
            }
            else
            {
                throw new InvalidOperationException($"Invalid binary tag: 0x{tag:X2}");
            }

            var buffer = new byte[length];
            int read = ms.Read(buffer, 0, length);
            if (read != length)
            {
                throw new EndOfStreamException($"Expected {length} bytes but read {read}");
            }

            return buffer;
        }
    }
}
