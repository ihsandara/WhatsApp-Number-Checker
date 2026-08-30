using System;
using System.IO;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Crypto.Signers;

namespace WhatsAppNumberChecker.Cryptography
{
    /// <summary>
    /// Handles WhatsApp Multi-Device Account Device Verification (ADV) companion signing.
    /// </summary>
    public static class AdvDeviceIdentitySigner
    {
        public static byte[] ProcessAndSignCompanionIdentity(
            byte[] companionIdentityBytes,
            byte[] identityPrivateKey,
            byte[] identityPublicKey)
        {
            if (companionIdentityBytes == null || companionIdentityBytes.Length == 0)
            {
                throw new ArgumentException("Companion identity bytes cannot be empty.", nameof(companionIdentityBytes));
            }

            // 1. Parse ADVDeviceIdentity Protobuf from companionIdentityBytes
            byte[]? details = null;
            byte[]? accountSignatureKey = null;
            byte[]? accountSignature = null;

            using (var ms = new MemoryStream(companionIdentityBytes))
            {
                while (ms.Position < ms.Length)
                {
                    int tag = ReadVarint(ms);
                    int field = tag >> 3;
                    int wire = tag & 0x07;

                    if (wire != 2)
                    {
                        SkipField(ms, wire);
                        continue;
                    }

                    int len = ReadVarint(ms);
                    var val = new byte[len];
                    ms.Read(val, 0, len);

                    if (field == 1) details = val;
                    else if (field == 3) accountSignatureKey = val;
                    else if (field == 4) accountSignature = val;
                }
            }

            if (details == null || accountSignatureKey == null || accountSignature == null)
            {
                throw new InvalidOperationException("Invalid ADV companion identity received from WhatsApp server.");
            }

            // 2. Build message to sign: [6, 0] + details + identityPublicKey + accountSignatureKey
            var msgToSign = new byte[2 + details.Length + identityPublicKey.Length + accountSignatureKey.Length];
            msgToSign[0] = 0x06;
            msgToSign[1] = 0x00;
            Buffer.BlockCopy(details, 0, msgToSign, 2, details.Length);
            Buffer.BlockCopy(identityPublicKey, 0, msgToSign, 2 + details.Length, identityPublicKey.Length);
            Buffer.BlockCopy(accountSignatureKey, 0, msgToSign, 2 + details.Length + identityPublicKey.Length, accountSignatureKey.Length);

            // 3. Ed25519 Sign with client identity private key
            var privParams = new Ed25519PrivateKeyParameters(identityPrivateKey, 0);
            var signer = new Ed25519Signer();
            signer.Init(true, privParams);
            signer.BlockUpdate(msgToSign, 0, msgToSign.Length);
            var deviceSignature = signer.GenerateSignature();

            // 4. Encode ADVSignedDeviceIdentity Protobuf
            using var outMs = new MemoryStream();

            // Field 1: details (Tag 0x0A)
            outMs.WriteByte(0x0A);
            WriteVarint(outMs, details.Length);
            outMs.Write(details, 0, details.Length);

            // Field 2: accountSignatureKey (Tag 0x12)
            outMs.WriteByte(0x12);
            WriteVarint(outMs, accountSignatureKey.Length);
            outMs.Write(accountSignatureKey, 0, accountSignatureKey.Length);

            // Field 3: accountSignature (Tag 0x1A)
            outMs.WriteByte(0x1A);
            WriteVarint(outMs, accountSignature.Length);
            outMs.Write(accountSignature, 0, accountSignature.Length);

            // Field 4: deviceSignature (Tag 0x22)
            outMs.WriteByte(0x22);
            WriteVarint(outMs, deviceSignature.Length);
            outMs.Write(deviceSignature, 0, deviceSignature.Length);

            return outMs.ToArray();
        }

        private static int ReadVarint(MemoryStream ms)
        {
            int result = 0;
            int shift = 0;
            while (true)
            {
                int b = ms.ReadByte();
                if (b == -1) throw new EndOfStreamException();
                result |= (b & 0x7F) << shift;
                if ((b & 0x80) == 0) break;
                shift += 7;
            }
            return result;
        }

        private static void WriteVarint(MemoryStream ms, int value)
        {
            while (value >= 0x80)
            {
                ms.WriteByte((byte)((value & 0x7F) | 0x80));
                value >>= 7;
            }
            ms.WriteByte((byte)(value & 0x7F));
        }

        private static void SkipField(MemoryStream ms, int wireType)
        {
            switch (wireType)
            {
                case 0: ReadVarint(ms); break;
                case 1: ms.Seek(8, SeekOrigin.Current); break;
                case 2:
                    int len = ReadVarint(ms);
                    ms.Seek(len, SeekOrigin.Current);
                    break;
                case 5: ms.Seek(4, SeekOrigin.Current); break;
                default: throw new InvalidOperationException($"Unsupported wire type {wireType}");
            }
        }
    }
}
