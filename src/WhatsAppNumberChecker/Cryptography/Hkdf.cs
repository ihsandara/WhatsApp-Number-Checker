using System;
using Org.BouncyCastle.Crypto.Digests;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Parameters;

namespace WhatsAppNumberChecker.Cryptography
{
    /// <summary>
    /// RFC 5869 HMAC-SHA256 Key Derivation Function (HKDF).
    /// </summary>
    public static class Hkdf
    {
        public static byte[] DeriveKey(byte[] ikm, byte[]? salt, byte[]? info, int length)
        {
            if (ikm == null) throw new ArgumentNullException(nameof(ikm));
            if (length <= 0) throw new ArgumentOutOfRangeException(nameof(length));

            var generator = new HkdfBytesGenerator(new Sha256Digest());
            var parameters = new HkdfParameters(ikm, salt, info);
            generator.Init(parameters);

            var result = new byte[length];
            generator.GenerateBytes(result, 0, length);
            return result;
        }

        public static (byte[] Key1, byte[] Key2) DeriveTwoKeys(byte[] ikm, byte[]? salt, byte[]? info, int keyLength = 32)
        {
            var bytes = DeriveKey(ikm, salt, info, keyLength * 2);
            var key1 = new byte[keyLength];
            var key2 = new byte[keyLength];

            Buffer.BlockCopy(bytes, 0, key1, 0, keyLength);
            Buffer.BlockCopy(bytes, keyLength, key2, 0, keyLength);

            return (key1, key2);
        }

        public static (byte[] Key1, byte[] Key2, byte[] Key3) DeriveThreeKeys(byte[] ikm, byte[]? salt, byte[]? info, int keyLength = 32)
        {
            var bytes = DeriveKey(ikm, salt, info, keyLength * 3);
            var key1 = new byte[keyLength];
            var key2 = new byte[keyLength];
            var key3 = new byte[keyLength];

            Buffer.BlockCopy(bytes, 0, key1, 0, keyLength);
            Buffer.BlockCopy(bytes, keyLength, key2, 0, keyLength);
            Buffer.BlockCopy(bytes, keyLength * 2, key3, 0, keyLength);

            return (key1, key2, key3);
        }
    }
}
