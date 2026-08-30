using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using WhatsAppNumberChecker.Abstractions;
using WhatsAppNumberChecker.Auth;
using WhatsAppNumberChecker.Exceptions;
using WhatsAppNumberChecker.Models;

namespace WhatsAppChecker.SampleConsole
{
    internal class Program
    {
        private static async Task Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            Console.WriteLine("╔═════════════════════════════════════════════════════════════╗");
            Console.WriteLine("║   WhatsApp Number Checker - Pure Native C# (.NET Engine)   ║");
            Console.WriteLine("╚═════════════════════════════════════════════════════════════╝\n");

            var builder = Host.CreateApplicationBuilder(args);

            // Configure clean, premium console logger
            builder.Services.AddLogging(logging =>
            {
                logging.ClearProviders();
                logging.AddSimpleConsole(opt =>
                {
                    opt.IncludeScopes = false;
                    opt.SingleLine = true;
                    opt.TimestampFormat = "HH:mm:ss ";
                });
                logging.SetMinimumLevel(LogLevel.Information);
            });

            // Register pure C# in-process engine
            builder.Services.AddWhatsAppChecker(options =>
            {
                options.AuthDirectory = "./whatsapp_session";
                options.DefaultBatchDelay = TimeSpan.FromMilliseconds(750);
                options.ConnectTimeout = TimeSpan.FromSeconds(30);
            });

            using var host = builder.Build();
            var checker = host.Services.GetRequiredService<IWhatsAppChecker>();

            Console.CancelKeyPress += (s, e) =>
            {
                checker.Dispose();
            };

            // Subscribe to QR Code pairing events for first-time authentication
            checker.QrCodeReceived += (sender, qrCodeString) =>
            {
                QrCodeRenderer.RenderToConsole(qrCodeString);
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine(" [⏳] WAITING: Point your WhatsApp camera at the QR code above.");
                Console.WriteLine("      (WhatsApp -> Settings -> Linked Devices -> Link a Device)");
                Console.ResetColor();
            };

            checker.StateChanged += (sender, state) =>
            {
                if (state == WhatsAppConnectionState.Connecting)
                {
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    Console.WriteLine(" [ℹ] Status: Establishing secure connection to WhatsApp network...");
                    Console.ResetColor();
                }
                else if (state == WhatsAppConnectionState.Connected)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine(" [✓] Status: Authenticated & Connected to WhatsApp!");
                    Console.ResetColor();
                }
            };

            try
            {
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine(" [1/2] Connecting directly to WhatsApp Web Network (In-Process)...");
                Console.ResetColor();

                await checker.ConnectAsync();

                if (checker.State != WhatsAppConnectionState.Connected)
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("\n [!] Awaiting scan completion. When you scan the QR code, re-run this app to check numbers!");
                    Console.ResetColor();
                    return;
                }

                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("\n [2/2] [✓] WhatsApp client authenticated successfully!");
                Console.ResetColor();

                // Interactive Phone Number Checker Loop
                Console.WriteLine("\n┌─────────────────────────────────────────────────────────────┐");
                Console.WriteLine("│              INTERACTIVE PHONE NUMBER CHECKER               │");
                Console.WriteLine("└─────────────────────────────────────────────────────────────┘");
                Console.WriteLine(" • Enter any phone number (e.g. +1 555 123 4567 or +964...)");
                Console.WriteLine(" • Type 'batch' to run a multi-number check with progress");
                Console.WriteLine(" • Type 'exit' to quit\n");

                while (true)
                {
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.Write("Enter Phone Number > ");
                    Console.ResetColor();

                    var input = Console.ReadLine();
                    if (string.IsNullOrWhiteSpace(input) || input.Trim().Equals("exit", StringComparison.OrdinalIgnoreCase))
                    {
                        break;
                    }

                    if (input.Trim().Equals("batch", StringComparison.OrdinalIgnoreCase))
                    {
                        Console.WriteLine("\n[⏳] Executing Batch Verification with Anti-Ban Throttling...");
                        var numbers = new[]
                        {
                            "+15551234567",
                            "+447911123456",
                            "+971501234567",
                            "+33612345678",
                            "+4915123456789"
                        };

                        var progress = new Progress<WhatsAppBatchProgress>(p =>
                        {
                            Console.WriteLine($"  [{p.Percentage,5:F1}%] ({p.Processed}/{p.Total}) {p.LatestResult.NormalizedNumber,-15} -> {(p.LatestResult.Exists ? "ACTIVE" : "NOT ON WA")}");
                        });

                        var batchResult = await checker.CheckBatchAsync(numbers, new WhatsAppBatchOptions
                        {
                            DelayBetweenChecks = TimeSpan.FromMilliseconds(750),
                            Jitter = TimeSpan.FromMilliseconds(200),
                            Progress = progress
                        });

                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine($"\n[✓] Batch complete: {batchResult.ExistingCount} active, {batchResult.InactiveCount} inactive in {batchResult.Duration.TotalSeconds:F2}s\n");
                        Console.ResetColor();
                        continue;
                    }

                    try
                    {
                        var result = await checker.CheckNumberAsync(input.Trim());
                        if (result.Exists)
                        {
                            Console.ForegroundColor = ConsoleColor.Green;
                            Console.WriteLine("  ┌─────────────────────────────────────────────────────────────┐");
                            Console.WriteLine("  │ [✓] STATUS: ACTIVE ON WHATSAPP                              │");
                            Console.WriteLine($"  │  • Phone Number: {result.NormalizedNumber,-43}│");
                            Console.WriteLine($"  │  • WhatsApp JID: {result.Jid ?? (result.NormalizedNumber + "@c.us"),-43}│");
                            Console.WriteLine($"  │  • Checked At:   {result.CheckedAtUtc:yyyy-MM-dd HH:mm:ss} UTC                     │");
                            Console.WriteLine("  └─────────────────────────────────────────────────────────────┘");
                            Console.ResetColor();
                        }
                        else
                        {
                            Console.ForegroundColor = ConsoleColor.Yellow;
                            Console.WriteLine("  ┌─────────────────────────────────────────────────────────────┐");
                            Console.WriteLine("  │ [✗] STATUS: NOT REGISTERED ON WHATSAPP                      │");
                            Console.WriteLine($"  │  • Phone Number: {result.NormalizedNumber,-43}│");
                            Console.WriteLine("  │  • Account:      No WhatsApp account associated with number │");
                            Console.WriteLine($"  │  • Checked At:   {result.CheckedAtUtc:yyyy-MM-dd HH:mm:ss} UTC                     │");
                            Console.WriteLine("  └─────────────────────────────────────────────────────────────┘");
                            Console.ResetColor();
                        }
                        Console.WriteLine();
                    }
                    catch (Exception ex)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"  [!] Error: {ex.Message}\n");
                        Console.ResetColor();
                    }
                }
            }
            catch (WhatsAppNotAuthenticatedException ex)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"\n[!] AUTHENTICATION REQUIRED: {ex.Message}");
                Console.ResetColor();
            }
            catch (WhatsAppConnectionException ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"\n[!] CONNECTION ERROR: {ex.Message}");
                Console.ResetColor();
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"\n[!] ERROR: {ex.Message}");
                Console.ResetColor();
            }
            finally
            {
                await checker.DisconnectAsync();
            }
        }
    }
}
