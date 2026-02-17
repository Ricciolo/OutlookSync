using Microsoft.EntityFrameworkCore;
using OutlookSync.Application.Extensions;
using OutlookSync.Infrastructure.Extensions;
using OutlookSync.Infrastructure.Persistence;

namespace OutlookSync.Api.Extensions;

/// <summary>
/// Extension methods for configuring OutlookSync services in the API layer.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds all OutlookSync services (Domain, Application, Infrastructure) to the service collection.
    /// </summary>
    /// <param name="services">The service collection to add services to.</param>
    /// <param name="configuration">The application configuration.</param>
    /// <returns>The updated service collection for method chaining.</returns>
    public static IServiceCollection AddOutlookSyncServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Register Infrastructure services (Database, Repositories)
        services.AddInfrastructure(configuration);

        // Register Application services (Business Logic, Use Cases)
        services.AddApplication();

        return services;
    }

    /// <summary>
    /// Applies database migrations asynchronously during application startup.
    /// </summary>
    /// <param name="app">The web application.</param>
    /// <returns>A task that represents the asynchronous operation, returning the web application for method chaining.</returns>
    public static async Task<WebApplication> ApplyMigrationsAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<OutlookSyncDbContext>();
        await dbContext.Database.MigrateAsync();
        return app;
    }
}
