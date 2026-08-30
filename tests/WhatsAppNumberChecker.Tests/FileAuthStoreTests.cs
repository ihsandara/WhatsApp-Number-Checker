using System;
using System.IO;
using System.Threading.Tasks;
using WhatsAppNumberChecker.Auth;
using Xunit;

namespace WhatsAppNumberChecker.Tests
{
    public class FileAuthStoreTests : IDisposable
    {
        private readonly string _testDir;

        public FileAuthStoreTests()
        {
            _testDir = Path.Combine(Path.GetTempPath(), "WhatsAppAuthTests_" + Guid.NewGuid().ToString("N"));
        }

        public void Dispose()
        {
            try
            {
                if (Directory.Exists(_testDir))
                {
                    Directory.Delete(_testDir, true);
                }
            }
            catch
            {
                // Ignore cleanup error
            }
        }

        [Fact]
        public async Task FileAuthStore_SaveAndLoad_PersistsCredentials()
        {
            var store = new FileAuthStore(_testDir);

            var state = new AuthState
            {
                NoiseStaticPrivateKeyBase64 = Convert.ToBase64String(new byte[32]),
                NoiseStaticPublicKeyBase64 = Convert.ToBase64String(new byte[32]),
                IdentityPrivateKeyBase64 = Convert.ToBase64String(new byte[32]),
                IdentityPublicKeyBase64 = Convert.ToBase64String(new byte[32]),
                Registered = true,
                MeJid = "15551234567@s.whatsapp.net",
                MeName = "TestUser",
                LastConnectedUtc = DateTime.UtcNow
            };

            await store.SaveAsync(state);

            var loaded = await store.LoadAsync();
            Assert.NotNull(loaded);
            Assert.True(loaded!.Registered);
            Assert.Equal("15551234567@s.whatsapp.net", loaded.MeJid);
            Assert.Equal("TestUser", loaded.MeName);
            Assert.NotNull(loaded.NoiseStaticPrivateKey);
            Assert.Equal(32, loaded.NoiseStaticPrivateKey.Length);
        }

        [Fact]
        public async Task FileAuthStore_ClearAsync_RemovesFile()
        {
            var store = new FileAuthStore(_testDir);
            var state = new AuthState { Registered = true };
            await store.SaveAsync(state);

            Assert.NotNull(await store.LoadAsync());

            await store.ClearAsync();
            Assert.Null(await store.LoadAsync());
        }
    }
}
