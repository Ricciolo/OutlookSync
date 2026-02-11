using Microsoft.EntityFrameworkCore;
using OutlookSync.Application.Interfaces;
using OutlookSync.Application.Services;
using OutlookSync.Application.Services.Mock;
using OutlookSync.Domain.Services;
using OutlookSync.Infrastructure.Persistence;
using OutlookSync.Infrastructure.Repositories;

namespace OutlookSync.Api.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddOutlookSyncServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Database
        services.AddDbContext<OutlookSyncDbContext>(options =>
            options.UseSqlite(
                configuration.GetConnectionString("DefaultConnection") 
                ?? "Data Source=outlooksync.db"));

        // Repositories
        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
        services.AddScoped<ICalendarRepository, CalendarRepository>();
        services.AddScoped<IDeviceRepository, DeviceRepository>();
        services.AddScoped<ISyncConfigRepository, SyncConfigRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // Application Services
        services.AddScoped<IExchangeService, MockExchangeService>();
        services.AddScoped<ICalendarSyncService, CalendarSyncService>();

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
