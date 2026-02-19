using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using OutlookSync.Application.Extensions;
using OutlookSync.Infrastructure.Extensions;
using OutlookSync.Infrastructure.Persistence;
using OutlookSync.Web.Authentication;

namespace OutlookSync.Web.Extensions;

/// <summary>
/// Extension methods for configuring OutlookSync services in the Web layer.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds all OutlookSync services (Domain, Application, Infrastructure) to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration.</param>
    /// <returns>The service collection for chaining.</returns>
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
    /// Adds HTTP basic authentication using settings from the <c>BasicAuth</c> configuration section.
    /// When <see cref="BasicAuthSettings.Username"/> is empty, authentication is disabled.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The application configuration.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddBasicAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<BasicAuthSettings>(configuration.GetSection(BasicAuthSettings.SectionName));

        services.AddAuthentication(BasicAuthenticationHandler.SchemeName)
            .AddScheme<AuthenticationSchemeOptions, BasicAuthenticationHandler>(
                BasicAuthenticationHandler.SchemeName, _ => { });

        services.AddAuthorization();

        return services;
    }

    /// <summary>
    /// Applies database migrations asynchronously during application startup.
    /// </summary>
    /// <param name="app">The web application.</param>
    /// <returns>The web application for chaining.</returns>
    public static async Task<WebApplication> ApplyMigrationsAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<OutlookSyncDbContext>();
        await dbContext.Database.MigrateAsync();
        return app;
    }
}
