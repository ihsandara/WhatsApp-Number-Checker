# WhatsAppNumberChecker

[![NuGet](https://img.shields.io/nuget/v/WhatsAppNumberChecker.svg)](https://www.nuget.org/packages/WhatsAppNumberChecker/)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)
[![Target: .NET Standard 2.0](https://img.shields.io/badge/.NET%20Standard-2.0-purple.svg)](https://dotnet.microsoft.com/)
[![Build & Test](https://img.shields.io/badge/tests-35%20passed-brightgreen.svg)]()

A high-performance, in-process .NET Standard 2.0 / .NET 8.0 library for verifying whether phone numbers have active, registered WhatsApp accounts.

---

> [!WARNING]
> ### Rate Limits, Anti-Ban & Terms of Service Notice
>
> 1. **Unofficial Integration**: This library interacts with the WhatsApp Web interface. It is not endorsed by or affiliated with Meta Platforms, Inc. or WhatsApp LLC.
> 2. **Terms of Service**: Automated bulk phone number enumeration or sending unsolicited messages violates the [WhatsApp Terms of Service](https://www.whatsapp.com/legal/terms-of-service).
> 3. **Account Risk**: Rapid or concurrent verification of phone numbers can result in temporary or permanent WhatsApp account restrictions.
> 4. **Operational Guidelines**:
>    - Always use the built-in batch throttling delays (recommended: 750ms to 1500ms between lookups).
>    - Keep jitter enabled so request intervals simulate natural human behavior.
>    - Use a dedicated WhatsApp account for lookup operations.
>    - Avoid validating thousands of phone numbers in rapid bursts.

---

## Architecture and Features

- **In-Process Execution**: Runs directly within your .NET process. No Docker containers or external sidecars required.
- **Session Persistence**: Preserves paired session credentials across application restarts in the configured storage directory.
- **Automatic Recovery**: Cleans stale lock files and recovers orphan processes on unexpected terminations.
- **Anti-Ban Batch Throttling**: Sequential batch processor with configurable delays, random jitter, and live progress reporting.
- **Dependency Injection**: First-class integration via `services.AddWhatsAppChecker()`.

```
+----------------------------------------------------------+
|                   .NET Application                       |
|  (ASP.NET Core / Worker / Console / .NET Framework 4.6+) |
+----------------------------+-----------------------------+
                             |
 +---------------------------+-----------------------------+
 |           WhatsAppNumberChecker (.NET Engine)           |
 |                                                         |
 |  |-- In-Process Browser Runtime                         |
 |  |-- Dynamic QR Code Scanner and Renderer               |
 |  |-- Native Contact Verification Pipeline               |
 |  |-- Persistent Authentication Store                    |
 |  `-- Anti-Ban Batch Rate Limiter and Progress Reporter  |
 +---------------------------+-----------------------------+
                             |
                             v
 +---------------------------------------------------------+
 |               Official WhatsApp Network                 |
 +---------------------------------------------------------+
```

---

## Installation

Install the package via the .NET CLI:

```bash
dotnet add package WhatsAppNumberChecker
```

Or via the Package Manager Console in Visual Studio:

```powershell
Install-Package WhatsAppNumberChecker
```

---

## Quick Start

### 1. In-Process Console Example

```csharp
using System;
using System.Threading.Tasks;
using WhatsAppNumberChecker.Auth;
using WhatsAppNumberChecker.Internal;
using WhatsAppNumberChecker.Options;

class Program
{
    static async Task Main(string[] args)
    {
        // 1. Initialize in-process engine
        var checker = new WhatsAppCheckerEngine(new WhatsAppCheckerOptions
        {
            AuthDirectory = "./whatsapp_session" // Persists session to disk
        });

        // 2. Listen for QR Code on first-time login
        checker.QrCodeReceived += (sender, qrCodeString) =>
        {
            QrCodeRenderer.RenderToConsole(qrCodeString);
        };

        // 3. Connect to WhatsApp Web
        Console.WriteLine("Connecting to WhatsApp...");
        await checker.ConnectAsync();

        // 4. Verify a phone number
        var result = await checker.CheckNumberAsync("+1 (555) 123-4567");
        
        Console.WriteLine($"Number:   {result.NormalizedNumber}");
        Console.WriteLine($"Exists:   {result.Exists}");
        Console.WriteLine($"JID:      {result.Jid}");
    }
}
```

---

### 2. Dependency Injection Setup (ASP.NET Core / Generic Host)

In your `Program.cs`:

```csharp
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

// Register WhatsApp Number Checker
builder.Services.AddWhatsAppChecker(options =>
{
    options.AuthDirectory = "./whatsapp_session";
    options.DefaultBatchDelay = TimeSpan.FromMilliseconds(750);
    options.ConnectTimeout = TimeSpan.FromSeconds(30);
    options.Headless = true;
});

var app = builder.Build();
```

---

### 3. Checking Single Numbers

```csharp
public class UserVerificationService
{
    private readonly IWhatsAppChecker _whatsAppChecker;
    private readonly ILogger<UserVerificationService> _logger;

    public UserVerificationService(IWhatsAppChecker whatsAppChecker, ILogger<UserVerificationService> logger)
    {
        _whatsAppChecker = whatsAppChecker;
        _logger = logger;
    }

    public async Task<bool> IsUserOnWhatsAppAsync(string rawPhoneNumber)
    {
        try
        {
            var result = await _whatsAppChecker.CheckNumberAsync(rawPhoneNumber);
            return result.Exists;
        }
        catch (WhatsAppNotAuthenticatedException)
        {
            _logger.LogWarning("WhatsApp session requires QR code scan.");
            throw;
        }
        catch (WhatsAppCheckerException ex)
        {
            _logger.LogError(ex, "Lookup failed for {Number}", rawPhoneNumber);
            throw;
        }
    }
}
```

---

### 4. Throttled Batch Number Verification with Progress

```csharp
var numbers = new[]
{
    "+15551234567",
    "+447911123456",
    "+971501234567",
    "+33612345678"
};

var progress = new Progress<WhatsAppBatchProgress>(p =>
{
    Console.WriteLine($"Progress: {p.Percentage:F1}% ({p.Processed}/{p.Total}) - Latest: {p.LatestResult.NormalizedNumber} -> Active: {p.LatestResult.Exists}");
});

var batchResult = await _whatsAppChecker.CheckBatchAsync(numbers, new WhatsAppBatchOptions
{
    DelayBetweenChecks = TimeSpan.FromMilliseconds(750), // Delay between lookups
    Jitter = TimeSpan.FromMilliseconds(200),             // Random jitter (+/- 200ms)
    ContinueOnError = true,
    Progress = progress
});

Console.WriteLine($"Checked {batchResult.TotalProcessed} numbers in {batchResult.Duration.TotalSeconds:F2}s.");
Console.WriteLine($"Active WhatsApp accounts found: {batchResult.ExistingCount}");
```

---

## Session Persistence & Authentication

- On initial startup, the `QrCodeReceived` event is raised with the pairing string.
- Scan the rendered QR code with your mobile app (**Settings > Linked Devices > Link a Device**).
- Session state is saved to `options.AuthDirectory` (`./whatsapp_session`).
- Subsequent application runs automatically resume the active session without prompting for a QR scan.

---

## Exception Handling Reference

All library exceptions derive from `WhatsAppCheckerException`:

| Exception Type | Condition |
| :--- | :--- |
| `WhatsAppNotAuthenticatedException` | Client is not authenticated (QR code scan required). |
| `WhatsAppConnectionException` | Connection error or browser launch failure. |
| `WhatsAppRateLimitedException` | WhatsApp rate limit triggered. |
| `WhatsAppValidationException` | Input string fails phone number normalization or E.164 constraints. |
| `WhatsAppCheckerException` | Base exception for all library errors. |

---

## Testing

Run the automated test suite with the .NET CLI:

```bash
dotnet test
```

---

## Packaging

To create a release NuGet package:

```bash
dotnet pack src/WhatsAppNumberChecker/WhatsAppNumberChecker.csproj -c Release -o ./artifacts
```

---

## License

This project is licensed under the [MIT License](LICENSE).
