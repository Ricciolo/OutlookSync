using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OutlookSync.Domain.Repositories;
using OutlookSync.Domain.Services;
using OutlookSync.Infrastructure.Persistence;
using OutlookSync.Infrastructure.Repositories;
using OutlookSync.Infrastructure.Services;

namespace OutlookSync.Infrastructure.Extensions;

/// <summary>
/// Extension methods for configuring Infrastructure services in the dependency injection container.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers Infrastructure layer services including database context, repositories, and Exchange services.
    /// </summary>
    /// <param name="services">The service collection to add services to.</param>
    /// <param name="configuration">The application configuration.</param>
    /// <returns>The updated service collection for method chaining.</returns>
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Database
        services.AddDbContext<OutlookSyncDbContext>(options =>
            options.UseSqlite(
                configuration.GetConnectionString("DefaultConnection") 
                ?? "Data Source=outlooksync.db"));

        // Repositories
        services.AddTransient<IUnitOfWork, UnitOfWork>();
        
        // Specific repositories
        services.AddTransient<Domain.Repositories.ICalendarRepository, CalendarRepository>();
        services.AddTransient<Domain.Repositories.ICredentialRepository, CredentialRepository>();
        
        // Calendar Event Repository Factory
        services.AddSingleton<ICalendarEventRepositoryFactory, CalendarEventRepositoryFactory>();
        
        // Domain Services
        services.AddTransient<ICredentialsService, CredentialsService>();

        return services;
    }
}
