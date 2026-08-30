using System;
using System.Text;
using Org.BouncyCastle.Crypto.Digests;

namespace WhatsAppNumberChecker.Cryptography
{
    /// <summary>
    /// Noise_XX_25519_AESGCM_SHA256 Handshake state machine for WhatsApp protocol.
    /// </summary>
    public class NoiseHandshake
    {
        public static readonly byte[] ProtocolName = Encoding.UTF8.GetBytes("Noise_XX_25519_AESGCM_SHA256");
        public static readonly byte[] WhatsAppPrologue = new byte[] { (byte)'W', (byte)'A', 0x06, 0x02 };

        private byte[] _h;
        private byte[] _ck;
        private NoiseCipher? _cipher;

        public byte[] LocalEphemeralPrivateKey { get; }
        public byte[] LocalEphemeralPublicKey { get; }
        public byte[] LocalStaticPrivateKey { get; }
        public byte[] LocalStaticPublicKey { get; }

        public byte[]? RemoteEphemeralPublicKey { get; private set; }
        public byte[]? RemoteStaticPublicKey { get; private set; }

        public NoiseHandshake(
            byte[]? localStaticPrivateKey = null,
            byte[]? localStaticPublicKey = null)
        {
            if (localStaticPrivateKey != null && localStaticPublicKey != null)
            {
                LocalStaticPrivateKey = localStaticPrivateKey;
                LocalStaticPublicKey = localStaticPublicKey;
            }
            else
            {
                var (priv, pub) = Curve25519.GenerateKeyPair();
                LocalStaticPrivateKey = priv;
                LocalStaticPublicKey = pub;
            }

            var (ePriv, ePub) = Curve25519.GenerateKeyPair();
            LocalEphemeralPrivateKey = ePriv;
            LocalEphemeralPublicKey = ePub;

            // Noise Protocol Spec: If protocol name <= 32 bytes, h = protocol_name padded with zeros to 32 bytes (NOT SHA256 hashed)
            _h = new byte[32];
            var protoBytes = Encoding.UTF8.GetBytes("Noise_XX_25519_AESGCM_SHA256");
            Buffer.BlockCopy(protoBytes, 0, _h, 0, protoBytes.Length);
            _ck = (byte[])_h.Clone();

            // Mix prologue into h
            MixHash(WhatsAppPrologue);
        }

        public byte[] Hash => (byte[])_h.Clone();
        public byte[] ChainingKey => (byte[])_ck.Clone();

        public void MixHash(byte[] data)
        {
            var digest = new Sha256Digest();
            digest.BlockUpdate(_h, 0, _h.Length);
            digest.BlockUpdate(data, 0, data.Length);
            var nextH = new byte[32];
            digest.DoFinal(nextH, 0);
            _h = nextH;
        }

        public void MixKey(byte[] ikm)
        {
            var (nextCk, key) = Hkdf.DeriveTwoKeys(ikm, _ck, null, 32);
            _ck = nextCk;
            _cipher = new NoiseCipher(key);
        }

        public byte[] EncryptAndHash(byte[] plaintext)
        {
            if (_cipher == null)
            {
                MixHash(plaintext);
                return (byte[])plaintext.Clone();
            }

            var ciphertext = _cipher.EncryptWithAd(_h, plaintext);
            MixHash(ciphertext);
            return ciphertext;
        }

        public byte[] DecryptAndHash(byte[] ciphertext)
        {
            if (_cipher == null)
            {
                MixHash(ciphertext);
                return (byte[])ciphertext.Clone();
            }

            var plaintext = _cipher.DecryptWithAd(_h, ciphertext);
            MixHash(ciphertext);
            return plaintext;
        }

        /// <summary>
        /// Step 1 (Client to Server): Ephemeral public key.
        /// </summary>
        public byte[] StartHandshake()
        {
            // MixHash(e.pub)
            MixHash(LocalEphemeralPublicKey);
            return (byte[])LocalEphemeralPublicKey.Clone();
        }

        /// <summary>
        /// Step 2 (Server to Client): Server ephemeral key, encrypted static key, and encrypted payload.
        /// </summary>
        public byte[] ProcessServerHello(byte[] serverEphemeral, byte[] encryptedStatic, byte[] encryptedPayload)
        {
            if (serverEphemeral == null || serverEphemeral.Length != 32)
            {
                throw new ArgumentException("Server ephemeral public key must be 32 bytes.", nameof(serverEphemeral));
            }

            // 1. Server ephemeral public key
            RemoteEphemeralPublicKey = (byte[])serverEphemeral.Clone();
            MixHash(RemoteEphemeralPublicKey);

            // 2. ee (DH: client ephemeral priv + server ephemeral pub)
            var ee = Curve25519.CalculateSharedSecret(LocalEphemeralPrivateKey, RemoteEphemeralPublicKey);
            MixKey(ee);

            // 3. s (Encrypted server static key: 48 bytes)
            RemoteStaticPublicKey = DecryptAndHash(encryptedStatic);

            // 4. es (DH: client ephemeral priv + server static pub)
            var es = Curve25519.CalculateSharedSecret(LocalEphemeralPrivateKey, RemoteStaticPublicKey);
            MixKey(es);

            // 5. Encrypted server payload
            if (encryptedPayload != null && encryptedPayload.Length > 0)
            {
                return DecryptAndHash(encryptedPayload);
            }

            return Array.Empty<byte>();
        }

        /// <summary>
        /// Step 3 (Client to Server): Encrypted client static key and encrypted client payload.
        /// </summary>
        public void CreateClientFinish(byte[] clientPayload, out byte[] encryptedStatic, out byte[] encryptedClientPayload)
        {
            if (RemoteEphemeralPublicKey == null)
            {
                throw new InvalidOperationException("Cannot finish handshake before processing server hello.");
            }

            // 1. Encrypted client static public key
            encryptedStatic = EncryptAndHash(LocalStaticPublicKey);

            // 2. se (DH: client static priv + server ephemeral pub)
            var se = Curve25519.CalculateSharedSecret(LocalStaticPrivateKey, RemoteEphemeralPublicKey);
            MixKey(se);

            // 3. Encrypted client payload
            encryptedClientPayload = EncryptAndHash(clientPayload);
        }

        /// <summary>
        /// Finalizes the handshake and splits the state into independent write and read ciphers.
        /// </summary>
        public (NoiseCipher WriteCipher, NoiseCipher ReadCipher) Split()
        {
            var (writeKey, readKey) = Hkdf.DeriveTwoKeys(Array.Empty<byte>(), _ck, null, 32);
            return (new NoiseCipher(writeKey), new NoiseCipher(readKey));
        }

        private static byte[] Sha256(byte[] data)
        {
            var digest = new Sha256Digest();
            digest.BlockUpdate(data, 0, data.Length);
            var result = new byte[32];
            digest.DoFinal(result, 0);
            return result;
        }
    }
}
