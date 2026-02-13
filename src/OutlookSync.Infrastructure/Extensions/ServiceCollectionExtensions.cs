using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OutlookSync.Domain.Repositories;
using OutlookSync.Domain.Services;
using OutlookSync.Domain.ValueObjects;
using OutlookSync.Infrastructure.Persistence;
using OutlookSync.Infrastructure.Repositories;
using OutlookSync.Infrastructure.Services.Exchange;

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
        
        // Exchange Configuration
        var exchangeConfig = ExchangeConfiguration.CreateDefault();
        services.AddSingleton(exchangeConfig);
        
        // Exchange Authentication Service
        services.AddSingleton<IExchangeAuthenticationService, ExchangeAuthenticationService>();
        
        // Calendar Event Repository Factory
        services.AddSingleton<ICalendarEventRepositoryFactory, CalendarEventRepositoryFactory>();

        return services;
    }
}
