using System;
using System.Text;
using WhatsAppNumberChecker.Cryptography;
using Xunit;

namespace WhatsAppNumberChecker.Tests
{
    public class CryptographyTests
    {
        [Fact]
        public void Curve25519_KeyAgreement_CalculatesIdenticalSharedSecret()
        {
            // Alice generates keypair
            var (alicePriv, alicePub) = Curve25519.GenerateKeyPair();
            Assert.Equal(32, alicePriv.Length);
            Assert.Equal(32, alicePub.Length);

            // Bob generates keypair
            var (bobPriv, bobPub) = Curve25519.GenerateKeyPair();
            Assert.Equal(32, bobPriv.Length);
            Assert.Equal(32, bobPub.Length);

            // Alice calculates secret using Bob's public key
            var aliceShared = Curve25519.CalculateSharedSecret(alicePriv, bobPub);

            // Bob calculates secret using Alice's public key
            var bobShared = Curve25519.CalculateSharedSecret(bobPriv, alicePub);

            // Assert
            Assert.Equal(32, aliceShared.Length);
            Assert.Equal(32, bobShared.Length);
            Assert.Equal(aliceShared, bobShared);
        }

        [Fact]
        public void Curve25519_GetPublicKey_MatchesDerivedPublicKey()
        {
            var (priv, pub) = Curve25519.GenerateKeyPair();
            var derivedPub = Curve25519.GetPublicKey(priv);
            Assert.Equal(pub, derivedPub);
        }

        [Fact]
        public void Hkdf_DeriveTwoKeys_ProducesDeterministicKeys()
        {
            var ikm = Encoding.UTF8.GetBytes("input-key-material-test");
            var salt = Encoding.UTF8.GetBytes("salt-test-value");
            var info = Encoding.UTF8.GetBytes("info-tag");

            var (key1A, key2A) = Hkdf.DeriveTwoKeys(ikm, salt, info, 32);
            var (key1B, key2B) = Hkdf.DeriveTwoKeys(ikm, salt, info, 32);

            Assert.Equal(32, key1A.Length);
            Assert.Equal(32, key2A.Length);
            Assert.Equal(key1A, key1B);
            Assert.Equal(key2A, key2B);
            Assert.NotEqual(key1A, key2A);
        }

        [Fact]
        public void NoiseCipher_EncryptAndDecrypt_RecoversOriginalPlaintext()
        {
            var key = new byte[32];
            new Random(42).NextBytes(key);

            var cipher = new NoiseCipher(key);
            var ad = Encoding.UTF8.GetBytes("associated-data-header");
            var plaintext = Encoding.UTF8.GetBytes("Hello WhatsApp Noise Protocol!");

            var ciphertext = cipher.EncryptWithAd(ad, plaintext);
            Assert.True(ciphertext.Length > plaintext.Length); // Plaintext + 16 byte tag

            // Decrypt with fresh cipher at counter 0
            var decryptCipher = new NoiseCipher(key);
            var decrypted = decryptCipher.DecryptWithAd(ad, ciphertext);

            Assert.Equal(plaintext, decrypted);
            Assert.Equal("Hello WhatsApp Noise Protocol!", Encoding.UTF8.GetString(decrypted));
        }

        [Fact]
        public void NoiseCipher_WithCorruptedTag_ThrowsCryptoException()
        {
            var key = new byte[32];
            new Random(42).NextBytes(key);

            var cipher = new NoiseCipher(key);
            var ad = Encoding.UTF8.GetBytes("header");
            var plaintext = Encoding.UTF8.GetBytes("Secret payload");

            var ciphertext = cipher.EncryptWithAd(ad, plaintext);
            ciphertext[ciphertext.Length - 1] ^= 0xFF; // Corrupt authentication tag

            var decryptCipher = new NoiseCipher(key);
            Assert.ThrowsAny<Exception>(() => decryptCipher.DecryptWithAd(ad, ciphertext));
        }

        [Fact]
        public void NoiseHandshake_InitializesWithCorrectParameters()
        {
            var handshake = new NoiseHandshake();

            Assert.NotNull(handshake.LocalEphemeralPublicKey);
            Assert.Equal(32, handshake.LocalEphemeralPublicKey.Length);
            Assert.NotNull(handshake.LocalStaticPublicKey);
            Assert.Equal(32, handshake.LocalStaticPublicKey.Length);
            Assert.Equal(32, handshake.Hash.Length);
            Assert.Equal(32, handshake.ChainingKey.Length);

            var ephemeralPub = handshake.StartHandshake();
            Assert.Equal(handshake.LocalEphemeralPublicKey, ephemeralPub);
        }
    }
}
