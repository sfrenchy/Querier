using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Querier.Api.Common.Utilities
{
    /// <summary>
    /// Add static service resolver to use when dependencies injection is not available
    /// </summary>
    public static class ServiceActivator
    {
        private static IServiceProvider _serviceProvider;
        private static readonly ILogger LOGGER;

        static ServiceActivator()
        {
            var serviceProvider = new ServiceCollection()
                .AddLogging()
                .BuildServiceProvider();
            LOGGER = serviceProvider.GetService<ILoggerFactory>()?.CreateLogger(typeof(ServiceActivator));
        }

        /// <summary>
        /// Configure ServiceActivator with full serviceProvider
        /// </summary>
        /// <param name="serviceProvider">The service provider to use for activation</param>
        /// <exception cref="ArgumentNullException">Thrown when serviceProvider is null</exception>
        public static void Configure(IServiceProvider serviceProvider)
        {
            if (serviceProvider == null)
            {
                LOGGER?.LogError("Attempt to configure ServiceActivator with null serviceProvider");
                throw new ArgumentNullException(nameof(serviceProvider));
            }

            LOGGER?.LogInformation("Configuring ServiceActivator with new service provider");
            _serviceProvider = serviceProvider;
            LOGGER?.LogDebug("ServiceActivator configured successfully");
        }

        /// <summary>
        /// Create a scope where use this ServiceActivator
        /// </summary>
        /// <param name="serviceProvider">Optional service provider to use instead of the configured one</param>
        /// <returns>A new service scope, or null if no service provider is available</returns>
        public static IServiceScope GetScope(IServiceProvider serviceProvider = null)
        {
            try
            {
                var provider = serviceProvider ?? _serviceProvider;
                
                if (provider == null)
                {
                    LOGGER?.LogWarning("No service provider available for scope creation");
                    return null;
                }

                LOGGER?.LogTrace("Creating new service scope");
                var scopeFactory = provider.GetRequiredService<IServiceScopeFactory>();
                var scope = scopeFactory.CreateScope();
                
                LOGGER?.LogTrace("Service scope created successfully");
                return scope;
            }
            catch (Exception ex)
            {
                LOGGER?.LogError(ex, "Error creating service scope");
                throw;
            }
        }
    }
}
