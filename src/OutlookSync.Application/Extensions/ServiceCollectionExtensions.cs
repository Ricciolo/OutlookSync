using Microsoft.Extensions.DependencyInjection;
using OutlookSync.Application.Services;
using OutlookSync.Domain.Services;

namespace OutlookSync.Application.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Application Services
        services.AddScoped<ICalendarsSyncService, CalendarsSyncService>();

        return services;
    }
}
