using System;
using System.IO;
using WhatsAppNumberChecker.Auth;

namespace WhatsAppNumberChecker.Cryptography
{
    /// <summary>
    /// Minimal, high-speed Protocol Buffer encoder and decoder for WhatsApp Noise Handshake messages.
    /// </summary>
    public static class ProtobufHandshake
    {
        public static byte[] EncodeClientHello(byte[] ephemeralPublicKey)
        {
            if (ephemeralPublicKey == null || ephemeralPublicKey.Length != 32)
            {
                throw new ArgumentException("Ephemeral public key must be 32 bytes.", nameof(ephemeralPublicKey));
            }

            // HandshakeMessage:
            //   ClientHello clientHello = 2; -> Tag: (2 << 3) | 2 = 0x12
            //     bytes ephemeral = 1;       -> Tag: (1 << 3) | 2 = 0x0A, Len: 32 (0x20)
            var clientHelloPayload = new byte[1 + 1 + 32];
            clientHelloPayload[0] = 0x0A;
            clientHelloPayload[1] = 0x20;
            Buffer.BlockCopy(ephemeralPublicKey, 0, clientHelloPayload, 2, 32);

            var handshakeMessage = new byte[1 + 1 + clientHelloPayload.Length];
            handshakeMessage[0] = 0x12;
            handshakeMessage[1] = (byte)clientHelloPayload.Length;
            Buffer.BlockCopy(clientHelloPayload, 0, handshakeMessage, 2, clientHelloPayload.Length);

            return handshakeMessage;
        }

        public static (byte[] Ephemeral, byte[] Static, byte[] Payload) DecodeServerHello(byte[] data)
        {
            using var ms = new MemoryStream(data);
            byte[]? ephemeral = null;
            byte[]? staticKey = null;
            byte[]? payload = null;

            while (ms.Position < ms.Length)
            {
                int tag = ReadVarint(ms);
                int fieldNum = tag >> 3;
                int wireType = tag & 0x07;

                if (wireType != 2) // length-delimited
                {
                    SkipField(ms, wireType);
                    continue;
                }

                int len = ReadVarint(ms);
                var fieldBytes = new byte[len];
                ms.Read(fieldBytes, 0, len);

                if (fieldNum == 3) // ServerHello
                {
                    // Parse inner ServerHello
                    using var innerMs = new MemoryStream(fieldBytes);
                    while (innerMs.Position < innerMs.Length)
                    {
                        int innerTag = ReadVarint(innerMs);
                        int innerField = innerTag >> 3;
                        int innerWire = innerTag & 0x07;

                        if (innerWire != 2)
                        {
                            SkipField(innerMs, innerWire);
                            continue;
                        }

                        int innerLen = ReadVarint(innerMs);
                        var innerData = new byte[innerLen];
                        innerMs.Read(innerData, 0, innerLen);

                        if (innerField == 1) ephemeral = innerData;
                        else if (innerField == 2) staticKey = innerData;
                        else if (innerField == 3) payload = innerData;
                    }
                }
            }

            if (ephemeral == null || staticKey == null)
            {
                throw new InvalidOperationException("Invalid ServerHello: Missing ephemeral or static key in protobuf.");
            }

            return (ephemeral, staticKey, payload ?? Array.Empty<byte>());
        }

        public static byte[] EncodeClientFinish(byte[] encryptedStaticKey, byte[] encryptedPayload)
        {
            using var innerMs = new MemoryStream();

            // Field 1: static
            innerMs.WriteByte(0x0A);
            WriteVarint(innerMs, encryptedStaticKey.Length);
            innerMs.Write(encryptedStaticKey, 0, encryptedStaticKey.Length);

            // Field 2: payload
            if (encryptedPayload != null && encryptedPayload.Length > 0)
            {
                innerMs.WriteByte(0x12);
                WriteVarint(innerMs, encryptedPayload.Length);
                innerMs.Write(encryptedPayload, 0, encryptedPayload.Length);
            }

            var innerBytes = innerMs.ToArray();

            using var outerMs = new MemoryStream();
            // HandshakeMessage Field 4: ClientFinish -> Tag: (4 << 3) | 2 = 0x22
            outerMs.WriteByte(0x22);
            WriteVarint(outerMs, innerBytes.Length);
            outerMs.Write(innerBytes, 0, innerBytes.Length);

            return outerMs.ToArray();
        }

        public static byte[] EncodeClientPayload(AuthState authState)
        {
            using var ms = new MemoryStream();

            // Field 3: userAgent (embedded message)
            using (var uaMs = new MemoryStream())
            {
                // Field 1: platform = 1 (WEB)
                uaMs.WriteByte((1 << 3) | 0);
                WriteVarint(uaMs, 1);

                // Field 2: appVersion (Primary: 2, Secondary: 3000, Tertiary: 1015901307)
                using (var verMs = new MemoryStream())
                {
                    verMs.WriteByte((1 << 3) | 0); WriteVarint(verMs, 2);
                    verMs.WriteByte((2 << 3) | 0); WriteVarint(verMs, 3000);
                    verMs.WriteByte((3 << 3) | 0); WriteVarint(verMs, 1015901307);

                    var verBytes = verMs.ToArray();
                    uaMs.WriteByte((2 << 3) | 2);
                    WriteVarint(uaMs, verBytes.Length);
                    uaMs.Write(verBytes, 0, verBytes.Length);
                }

                // Field 5: osVersion = "0.1.0"
                var osVerBytes = System.Text.Encoding.UTF8.GetBytes("0.1.0");
                uaMs.WriteByte((5 << 3) | 2);
                WriteVarint(uaMs, osVerBytes.Length);
                uaMs.Write(osVerBytes, 0, osVerBytes.Length);

                // Field 7: device = "Desktop"
                var devBytes = System.Text.Encoding.UTF8.GetBytes("Desktop");
                uaMs.WriteByte((7 << 3) | 2);
                WriteVarint(uaMs, devBytes.Length);
                uaMs.Write(devBytes, 0, devBytes.Length);

                var uaBytes = uaMs.ToArray();
                ms.WriteByte((3 << 3) | 2);
                WriteVarint(ms, uaBytes.Length);
                ms.Write(uaBytes, 0, uaBytes.Length);
            }

            // Field 4: webInfo (Field 1: webSubPlatform = 0)
            using (var webMs = new MemoryStream())
            {
                webMs.WriteByte((1 << 3) | 0);
                WriteVarint(webMs, 0);
                var webBytes = webMs.ToArray();
                ms.WriteByte((4 << 3) | 2);
                WriteVarint(ms, webBytes.Length);
                ms.Write(webBytes, 0, webBytes.Length);
            }

            // Field 8: connectType = 1 (WIFI) -> Tag: (8 << 3) | 0 = 0x40
            ms.WriteByte((8 << 3) | 0);
            WriteVarint(ms, 1);

            // Field 9: connectReason = 1 (USER_ACTIVATE) -> Tag: (9 << 3) | 0 = 0x48
            ms.WriteByte((9 << 3) | 0);
            WriteVarint(ms, 1);

            // Field 15: devicePairingData (Companion Registration Data for QR link)
            if (!authState.Registered && authState.IdentityPrivateKey != null && authState.IdentityPublicKey != null)
            {
                var (_, preKeyPub) = Curve25519.GenerateKeyPair();

                var privParams = new Org.BouncyCastle.Crypto.Parameters.Ed25519PrivateKeyParameters(authState.IdentityPrivateKey, 0);
                var signer = new Org.BouncyCastle.Crypto.Signers.Ed25519Signer();
                signer.Init(true, privParams);
                signer.BlockUpdate(preKeyPub, 0, preKeyPub.Length);
                var preKeySig = signer.GenerateSignature();

                using (var pairMs = new MemoryStream())
                {
                    // Field 1: eRegid (4 bytes)
                    pairMs.WriteByte(0x0A);
                    pairMs.WriteByte(0x04);
                    pairMs.Write(new byte[] { 0x00, 0x01, 0x02, 0x03 }, 0, 4);

                    // Field 2: eKeytype (1 byte: 0x05)
                    pairMs.WriteByte(0x12);
                    pairMs.WriteByte(0x01);
                    pairMs.WriteByte(0x05);

                    // Field 3: eIdent (32 bytes)
                    pairMs.WriteByte(0x1A);
                    WriteVarint(pairMs, authState.IdentityPublicKey.Length);
                    pairMs.Write(authState.IdentityPublicKey, 0, authState.IdentityPublicKey.Length);

                    // Field 4: eSkeyId (3 bytes: [0, 0, 1])
                    pairMs.WriteByte(0x22);
                    pairMs.WriteByte(0x03);
                    pairMs.Write(new byte[] { 0x00, 0x00, 0x01 }, 0, 3);

                    // Field 5: eSkeyVal (32 bytes)
                    pairMs.WriteByte(0x2A);
                    WriteVarint(pairMs, preKeyPub.Length);
                    pairMs.Write(preKeyPub, 0, preKeyPub.Length);

                    // Field 6: eSkeySig (64 bytes)
                    pairMs.WriteByte(0x32);
                    WriteVarint(pairMs, preKeySig.Length);
                    pairMs.Write(preKeySig, 0, preKeySig.Length);

                    var pairBytes = pairMs.ToArray();

                    // Field 15 in ClientPayload: devicePairingData -> Tag: (15 << 3) | 2 = 0x7A
                    ms.WriteByte((15 << 3) | 2);
                    WriteVarint(ms, pairBytes.Length);
                    ms.Write(pairBytes, 0, pairBytes.Length);
                }
            }

            return ms.ToArray();
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
