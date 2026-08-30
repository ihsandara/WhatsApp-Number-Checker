using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using WhatsAppNumberChecker.Models;

namespace WhatsAppNumberChecker.Abstractions
{
    /// <summary>
    /// Contract for the pure native C# in-process WhatsApp number verification engine.
    /// </summary>
    public interface IWhatsAppChecker : IDisposable
    {
        /// <summary>
        /// Gets the current connection state of the WhatsApp client.
        /// </summary>
        WhatsAppConnectionState State { get; }

        /// <summary>
        /// Event triggered when a new QR code is generated for pairing a mobile device.
        /// </summary>
        event EventHandler<string>? QrCodeReceived;

        /// <summary>
        /// Event triggered whenever the connection state changes.
        /// </summary>
        event EventHandler<WhatsAppConnectionState>? StateChanged;

        /// <summary>
        /// Connects to the WhatsApp WebSocket network and completes Noise protocol authentication.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task ConnectAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Disconnects from the WhatsApp network and closes the WebSocket connection.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task DisconnectAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Queries whether a single phone number is registered with an active WhatsApp account.
        /// </summary>
        /// <param name="phoneNumber">The raw or formatted phone number (e.g. "+1-555-123-4567").</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Result containing registration status, sanitized number, and WhatsApp JID.</returns>
        /// <exception cref="Exceptions.WhatsAppValidationException">Thrown when phone number format is invalid.</exception>
        /// <exception cref="Exceptions.WhatsAppNotAuthenticatedException">Thrown when the client is not authenticated.</exception>
        /// <exception cref="Exceptions.WhatsAppRateLimitedException">Thrown when rate limited.</exception>
        /// <exception cref="Exceptions.WhatsAppCheckerException">Thrown on socket or protocol errors.</exception>
        Task<WhatsAppCheckResult> CheckNumberAsync(string phoneNumber, CancellationToken cancellationToken = default);

        /// <summary>
        /// Sequentially checks a collection of phone numbers with configurable delays and jitter to prevent WhatsApp rate limits and anti-spam bans.
        /// </summary>
        /// <param name="phoneNumbers">The collection of phone numbers to verify.</param>
        /// <param name="options">Optional batch execution settings (delays, error handling, progress callback).</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A batch summary containing results for all checked numbers.</returns>
        Task<WhatsAppBatchResult> CheckBatchAsync(
            IEnumerable<string> phoneNumbers,
            WhatsAppBatchOptions? options = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Checks if the client is actively connected and authenticated with WhatsApp.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns><c>true</c> if connected and authenticated; otherwise <c>false</c>.</returns>
        Task<bool> IsReadyAsync(CancellationToken cancellationToken = default);
    }
}
