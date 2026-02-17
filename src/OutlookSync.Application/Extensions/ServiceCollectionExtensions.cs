using Microsoft.Extensions.DependencyInjection;
using OutlookSync.Application.Services;
using OutlookSync.Domain.Services;

namespace OutlookSync.Application.Extensions;

/// <summary>
/// Extension methods for configuring Application services in the dependency injection container.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers Application layer services including business logic and use case orchestration.
    /// </summary>
    /// <param name="services">The service collection to add services to.</param>
    /// <returns>The updated service collection for method chaining.</returns>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Application Services
        services.AddScoped<ICalendarsSyncService, CalendarsSyncService>();

        return services;
    }
}
