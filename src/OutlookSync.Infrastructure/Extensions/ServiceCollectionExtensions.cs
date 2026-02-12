using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OutlookSync.Domain.Repositories;
using OutlookSync.Infrastructure.Persistence;
using OutlookSync.Infrastructure.Repositories;

namespace OutlookSync.Infrastructure.Extensions;

public static class ServiceCollectionExtensions
{
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
        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        
        // Calendar Event Repository Factory
        services.AddSingleton<ICalendarEventRepositoryFactory, CalendarEventRepositoryFactory>();

        return services;
    }
}
