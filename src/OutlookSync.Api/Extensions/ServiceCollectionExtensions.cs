using Microsoft.EntityFrameworkCore;
using OutlookSync.Application.Extensions;
using OutlookSync.Infrastructure.Extensions;
using OutlookSync.Infrastructure.Persistence;

namespace OutlookSync.Api.Extensions;

public static class ServiceCollectionExtensions
{
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

    public static async Task<WebApplication> ApplyMigrationsAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<OutlookSyncDbContext>();
        await dbContext.Database.MigrateAsync();
        return app;
    }
}
