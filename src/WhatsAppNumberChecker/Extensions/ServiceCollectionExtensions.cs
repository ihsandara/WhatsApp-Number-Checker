using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using WhatsAppNumberChecker.Abstractions;
using WhatsAppNumberChecker.Auth;
using WhatsAppNumberChecker.Internal;
using WhatsAppNumberChecker.Options;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Extension methods for setting up WhatsApp Number Checker services in an <see cref="IServiceCollection"/>.
    /// </summary>
    public static class WhatsAppNumberCheckerServiceCollectionExtensions
    {
        /// <summary>
        /// Adds the pure C# <see cref="IWhatsAppChecker"/> native engine and required dependencies to the <see cref="IServiceCollection"/>.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configure">Optional configuration action for <see cref="WhatsAppCheckerOptions"/>.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddWhatsAppChecker(
            this IServiceCollection services,
            Action<WhatsAppCheckerOptions>? configure = null)
        {
            if (services == null) throw new ArgumentNullException(nameof(services));

            var optionsBuilder = services.AddOptions<WhatsAppCheckerOptions>();
            if (configure != null)
            {
                optionsBuilder.Configure(configure);
            }

            services.TryAddSingleton<IWhatsAppNumberNormalizer, WhatsAppNumberNormalizer>();
            services.TryAddSingleton<IWhatsAppAuthStore>(sp =>
            {
                var options = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<WhatsAppCheckerOptions>>().Value;
                return new FileAuthStore(options.AuthDirectory);
            });

            services.TryAddSingleton<IWhatsAppChecker, WhatsAppCheckerEngine>();

            return services;
        }
    }
}
