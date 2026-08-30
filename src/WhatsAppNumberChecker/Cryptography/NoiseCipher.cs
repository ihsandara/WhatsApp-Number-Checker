using System;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Modes;
using Org.BouncyCastle.Crypto.Parameters;

namespace WhatsAppNumberChecker.Cryptography
{
    /// <summary>
    /// AES-GCM AEAD Cipher State with incrementing 64-bit nonce for WhatsApp Noise Protocol.
    /// </summary>
    public class NoiseCipher
    {
        private readonly byte[] _key;
        private ulong _counter;

        public NoiseCipher(byte[] key, ulong initialCounter = 0)
        {
            if (key == null || key.Length != 32)
            {
                throw new ArgumentException("Noise key must be 32 bytes (256 bits).", nameof(key));
            }

            _key = (byte[])key.Clone();
            _counter = initialCounter;
        }

        public byte[] Key => (byte[])_key.Clone();
        public ulong Counter => _counter;

        public byte[] EncryptWithAd(byte[] associatedData, byte[] plaintext)
        {
            var iv = GenerateIv(_counter);
            _counter++;

            var cipher = new GcmBlockCipher(new AesEngine());
            var parameters = new AeadParameters(new KeyParameter(_key), 128, iv, associatedData);
            cipher.Init(true, parameters);

            var output = new byte[cipher.GetOutputSize(plaintext.Length)];
            int len = cipher.ProcessBytes(plaintext, 0, plaintext.Length, output, 0);
            cipher.DoFinal(output, len);

            return output;
        }

        public byte[] DecryptWithAd(byte[] associatedData, byte[] ciphertext)
        {
            var iv = GenerateIv(_counter);
            _counter++;

            var cipher = new GcmBlockCipher(new AesEngine());
            var parameters = new AeadParameters(new KeyParameter(_key), 128, iv, associatedData);
            cipher.Init(false, parameters);

            var output = new byte[cipher.GetOutputSize(ciphertext.Length)];
            int len = cipher.ProcessBytes(ciphertext, 0, ciphertext.Length, output, 0);
            cipher.DoFinal(output, len);

            return output;
        }

        private static byte[] GenerateIv(ulong counter)
        {
            var iv = new byte[12];
            // Noise standard: 4 bytes zero + 8 bytes big-endian counter
            iv[4] = (byte)(counter >> 56);
            iv[5] = (byte)(counter >> 48);
            iv[6] = (byte)(counter >> 40);
            iv[7] = (byte)(counter >> 32);
            iv[8] = (byte)(counter >> 24);
            iv[9] = (byte)(counter >> 16);
            iv[10] = (byte)(counter >> 8);
            iv[11] = (byte)counter;
            return iv;
        }
    }
}
