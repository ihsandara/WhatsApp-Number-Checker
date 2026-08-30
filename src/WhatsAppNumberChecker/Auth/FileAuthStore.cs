using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace WhatsAppNumberChecker.Auth
{
    /// <summary>
    /// File-based implementation of <see cref="IWhatsAppAuthStore"/> that saves session credentials to disk.
    /// </summary>
    public class FileAuthStore : IWhatsAppAuthStore
    {
        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true
        };

        private readonly string _directoryPath;
        private readonly string _credentialsFilePath;

        public FileAuthStore(string? directoryPath = null)
        {
            _directoryPath = string.IsNullOrWhiteSpace(directoryPath)
                ? Path.Combine(Directory.GetCurrentDirectory(), "auth_data")
                : Path.GetFullPath(directoryPath);

            _credentialsFilePath = Path.Combine(_directoryPath, "creds.json");
        }

        public string DirectoryPath => _directoryPath;

        public async Task<AuthState?> LoadAsync(CancellationToken cancellationToken = default)
        {
            if (!File.Exists(_credentialsFilePath))
            {
                return null;
            }

            try
            {
                using var stream = new FileStream(_credentialsFilePath, FileMode.Open, FileAccess.Read, FileShare.Read);
                using var reader = new StreamReader(stream, Encoding.UTF8);
                var json = await reader.ReadToEndAsync().ConfigureAwait(false);
                return JsonSerializer.Deserialize<AuthState>(json, JsonOptions);
            }
            catch
            {
                return null;
            }
        }

        public async Task SaveAsync(AuthState state, CancellationToken cancellationToken = default)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));

            if (!Directory.Exists(_directoryPath))
            {
                Directory.CreateDirectory(_directoryPath);
            }

            var json = JsonSerializer.Serialize(state, JsonOptions);
            using var stream = new FileStream(_credentialsFilePath, FileMode.Create, FileAccess.Write, FileShare.None);
            using var writer = new StreamWriter(stream, Encoding.UTF8);
            await writer.WriteAsync(json).ConfigureAwait(false);
        }

        public Task ClearAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                if (File.Exists(_credentialsFilePath))
                {
                    File.Delete(_credentialsFilePath);
                }
            }
            catch
            {
                // Ignore cleanup errors
            }

            return Task.CompletedTask;
        }
    }
}
