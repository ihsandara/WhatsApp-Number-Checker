using System;
using System.Collections.Generic;
using System.Text;

namespace WhatsAppNumberChecker.Protocol.WABinary
{
    /// <summary>
    /// Represents a hierarchical WhatsApp WABinary XML stanza node.
    /// </summary>
    public class BinaryNode
    {
        public string Tag { get; set; }
        public Dictionary<string, string> Attributes { get; set; }
        public object? Content { get; set; }

        public BinaryNode(string tag, Dictionary<string, string>? attributes = null, object? content = null)
        {
            Tag = tag ?? throw new ArgumentNullException(nameof(tag));
            Attributes = attributes ?? new Dictionary<string, string>();
            Content = content;
        }

        public string? GetAttribute(string key)
        {
            if (Attributes != null && Attributes.TryGetValue(key, out var val))
            {
                return val;
            }
            return null;
        }

        public BinaryNode[] GetChildren()
        {
            if (Content is BinaryNode[] nodes) return nodes;
            if (Content is IEnumerable<BinaryNode> enumNodes)
            {
                var list = new List<BinaryNode>(enumNodes);
                return list.ToArray();
            }
            return Array.Empty<BinaryNode>();
        }

        public BinaryNode? GetChild(string tag)
        {
            foreach (var child in GetChildren())
            {
                if (string.Equals(child.Tag, tag, StringComparison.OrdinalIgnoreCase))
                {
                    return child;
                }
            }
            return null;
        }

        public string? GetContentAsString()
        {
            if (Content is string s) return s;
            if (Content is byte[] b) return Encoding.UTF8.GetString(b);
            return null;
        }

        public byte[]? GetContentAsBytes()
        {
            if (Content is byte[] b) return b;
            if (Content is string s) return Encoding.UTF8.GetBytes(s);
            return null;
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append('<').Append(Tag);
            if (Attributes != null)
            {
                foreach (var kvp in Attributes)
                {
                    sb.Append(' ').Append(kvp.Key).Append("=\"").Append(kvp.Value).Append('"');
                }
            }

            if (Content is BinaryNode[] children && children.Length > 0)
            {
                sb.Append('>');
                foreach (var child in children)
                {
                    sb.Append(child.ToString());
                }
                sb.Append("</").Append(Tag).Append('>');
            }
            else if (Content is string text && !string.IsNullOrEmpty(text))
            {
                sb.Append('>').Append(text).Append("</").Append(Tag).Append('>');
            }
            else
            {
                sb.Append("/>");
            }

            return sb.ToString();
        }
    }
}
