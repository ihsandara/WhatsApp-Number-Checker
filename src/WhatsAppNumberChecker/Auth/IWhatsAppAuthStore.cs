using System.Threading;
using System.Threading.Tasks;

namespace WhatsAppNumberChecker.Auth
{
    /// <summary>
    /// Contract for persisting and retrieving WhatsApp session credentials.
    /// </summary>
    public interface IWhatsAppAuthStore
    {
        /// <summary>
        /// Loads saved session credentials, or returns null if no session exists.
        /// </summary>
        Task<AuthState?> LoadAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Saves session credentials to persistent storage.
        /// </summary>
        Task SaveAsync(AuthState state, CancellationToken cancellationToken = default);

        /// <summary>
        /// Clears persisted session credentials (e.g. on logout).
        /// </summary>
        Task ClearAsync(CancellationToken cancellationToken = default);
    }
}
