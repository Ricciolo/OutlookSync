using Microsoft.Extensions.DependencyInjection;
using OutlookSync.AI.Configuration;
using OutlookSync.AI.Interfaces;
using OutlookSync.AI.Services;

namespace OutlookSync.AI.Extensions;

/// <summary>
/// Extension methods for registering AI module services with the DI container.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds the OutlookSync AI module services to the specified <see cref="IServiceCollection"/>.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configureAi">An optional delegate to configure <see cref="AiOptions"/>.</param>
    /// <param name="configureAgent">An optional delegate to configure <see cref="AgentOptions"/>.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddOutlookSyncAI(
        this IServiceCollection services,
        Action<AiOptions>? configureAi = null,
        Action<AgentOptions>? configureAgent = null)
    {
        if (configureAi is not null)
        {
            services.Configure(configureAi);
        }

        if (configureAgent is not null)
        {
            services.Configure(configureAgent);
        }

        services.AddSingleton<IPrivacyService, PrivacyService>();
        services.AddSingleton<IAiService, AiService>();
        services.AddSingleton<IAgentExecutor, AgentExecutor>();

        return services;
    }
}
