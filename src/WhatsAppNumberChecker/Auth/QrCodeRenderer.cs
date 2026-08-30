using System;
using System.Text;
using QRCoder;

namespace WhatsAppNumberChecker.Auth
{
    /// <summary>
    /// Utility for rendering compact, perfectly square, high-contrast 2D QR codes directly in the terminal console.
    /// </summary>
    public static class QrCodeRenderer
    {
        /// <summary>
        /// Renders a compact, perfectly square 2D QR code with white background and black modules.
        /// </summary>
        /// <param name="qrCodeString">The raw QR pairing payload emitted by WhatsApp.</param>
        public static void RenderToConsole(string qrCodeString)
        {
            if (string.IsNullOrWhiteSpace(qrCodeString)) return;

            try
            {
                using var qrGenerator = new QRCodeGenerator();
                // ECCLevel.L creates the smallest matrix size with maximum density
                using var qrCodeData = qrGenerator.CreateQrCode(qrCodeString, QRCodeGenerator.ECCLevel.L);

                var matrix = qrCodeData.ModuleMatrix;
                int size = matrix.Count;
                int border = 1; // Minimal 1-module border for ultra-compact 1:1 square size
                int totalSize = size + (border * 2);

                var sb = new StringBuilder();

                // ANSI Color Codes: White Background (\u001b[47m), Black Foreground (\u001b[30m)
                const string whiteBgBlackFg = "\u001b[47m\u001b[30m";
                const string resetColor = "\u001b[0m";

                for (int y = 0; y < totalSize; y += 2)
                {
                    sb.Append("  "); // Small left indent
                    sb.Append(whiteBgBlackFg);

                    for (int x = 0; x < totalSize; x++)
                    {
                        int matrixX = x - border;
                        int matrixYTop = y - border;
                        int matrixYBottom = y + 1 - border;

                        bool topDark = matrixX >= 0 && matrixX < size && matrixYTop >= 0 && matrixYTop < size && matrix[matrixYTop][matrixX];
                        bool bottomDark = matrixX >= 0 && matrixX < size && matrixYBottom >= 0 && matrixYBottom < size && matrix[matrixYBottom][matrixX];

                        if (topDark && bottomDark)
                        {
                            sb.Append('█'); // Both top and bottom are dark
                        }
                        else if (topDark && !bottomDark)
                        {
                            sb.Append('▀'); // Top is dark, bottom is white
                        }
                        else if (!topDark && bottomDark)
                        {
                            sb.Append('▄'); // Top is white, bottom is dark
                        }
                        else
                        {
                            sb.Append(' '); // Both are white
                        }
                    }

                    sb.Append(resetColor);
                    sb.AppendLine();
                }

                Console.WriteLine();
                Console.WriteLine("==================================================================");
                Console.WriteLine("                 WHATSAPP AUTHENTICATION REQUIRED                ");
                Console.WriteLine("==================================================================");
                Console.WriteLine("1. Open WhatsApp > Linked Devices > Link a Device.");
                Console.WriteLine("2. Point your camera at the QR code below:");
                Console.WriteLine("------------------------------------------------------------------");
                Console.Write(sb.ToString());
                Console.WriteLine("------------------------------------------------------------------");
                Console.WriteLine("Token: " + qrCodeString);
                Console.WriteLine("==================================================================");
                Console.WriteLine();
            }
            catch
            {
                // Fallback to text token
                Console.WriteLine();
                Console.WriteLine("==================================================================");
                Console.WriteLine("                 WHATSAPP AUTHENTICATION REQUIRED                ");
                Console.WriteLine("==================================================================");
                Console.WriteLine("Token: " + qrCodeString);
                Console.WriteLine("==================================================================");
                Console.WriteLine();
            }
        }

        /// <summary>
        /// Formats a WhatsApp Multi-Device (MD) QR code string from cryptographic keys.
        /// </summary>
        public static string FormatQrPayload(
            string serverEphemeralBase64,
            string clientStaticPubBase64,
            string clientIdentityPubBase64,
            string advSecretKeyBase64)
        {
            return $"{serverEphemeralBase64},{clientStaticPubBase64},{clientIdentityPubBase64},{advSecretKeyBase64}";
        }
    }
}
