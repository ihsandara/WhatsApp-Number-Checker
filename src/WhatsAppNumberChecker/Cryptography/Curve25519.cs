using System;
using Org.BouncyCastle.Crypto.Agreement;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Security;

namespace WhatsAppNumberChecker.Cryptography
{
    /// <summary>
    /// Curve25519 (X25519) Diffie-Hellman key agreement operations.
    /// </summary>
    public static class Curve25519
    {
        public const int KeySize = 32;

        public static (byte[] PrivateKey, byte[] PublicKey) GenerateKeyPair()
        {
            var random = new SecureRandom();
            var generator = new X25519KeyPairGenerator();
            generator.Init(new X25519KeyGenerationParameters(random));
            var keyPair = generator.GenerateKeyPair();

            var privParams = (X25519PrivateKeyParameters)keyPair.Private;
            var pubParams = (X25519PublicKeyParameters)keyPair.Public;

            var privateKey = privParams.GetEncoded();
            var publicKey = pubParams.GetEncoded();

            return (privateKey, publicKey);
        }

        public static byte[] GetPublicKey(byte[] privateKey)
        {
            if (privateKey == null || privateKey.Length != KeySize)
            {
                throw new ArgumentException($"Private key must be {KeySize} bytes.", nameof(privateKey));
            }

            var privParams = new X25519PrivateKeyParameters(privateKey, 0);
            var pubParams = privParams.GeneratePublicKey();
            return pubParams.GetEncoded();
        }

        public static byte[] CalculateSharedSecret(byte[] privateKey, byte[] publicKey)
        {
            if (privateKey == null || privateKey.Length != KeySize)
                throw new ArgumentException($"Private key must be {KeySize} bytes.", nameof(privateKey));
            if (publicKey == null || publicKey.Length != KeySize)
                throw new ArgumentException($"Public key must be {KeySize} bytes.", nameof(publicKey));

            var privParams = new X25519PrivateKeyParameters(privateKey, 0);
            var pubParams = new X25519PublicKeyParameters(publicKey, 0);

            var agreement = new X25519Agreement();
            agreement.Init(privParams);

            var sharedSecret = new byte[KeySize];
            agreement.CalculateAgreement(pubParams, sharedSecret, 0);
            return sharedSecret;
        }
    }
}
