using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using WhatsAppNumberChecker.Abstractions;
using WhatsAppNumberChecker.Auth;
using WhatsAppNumberChecker.Internal;
using WhatsAppNumberChecker.Options;
using Xunit;

namespace WhatsAppNumberChecker.Tests
{
    public class ServiceCollectionExtensionsTests
    {
        [Fact]
        public void AddWhatsAppChecker_RegistersServicesCorrectly()
        {
            // Arrange
            var services = new ServiceCollection();

            services.AddWhatsAppChecker(options =>
            {
                options.AuthDirectory = "./test_auth";
                options.ConnectTimeout = TimeSpan.FromSeconds(45);
                options.DefaultBatchDelay = TimeSpan.FromMilliseconds(600);
            });

            var serviceProvider = services.BuildServiceProvider();

            // Act
            var normalizer = serviceProvider.GetService<IWhatsAppNumberNormalizer>();
            var authStore = serviceProvider.GetService<IWhatsAppAuthStore>();
            var options = serviceProvider.GetService<IOptions<WhatsAppCheckerOptions>>();
            var checker = serviceProvider.GetService<IWhatsAppChecker>();

            // Assert
            Assert.NotNull(normalizer);
            Assert.IsType<WhatsAppNumberNormalizer>(normalizer);

            Assert.NotNull(authStore);
            Assert.IsType<FileAuthStore>(authStore);

            Assert.NotNull(options);
            Assert.Equal("./test_auth", options?.Value.AuthDirectory);
            Assert.Equal(TimeSpan.FromSeconds(45), options?.Value.ConnectTimeout);
            Assert.Equal(TimeSpan.FromMilliseconds(600), options?.Value.DefaultBatchDelay);

            Assert.NotNull(checker);
            Assert.IsType<WhatsAppCheckerEngine>(checker);
        }
    }
}
