using Microsoft.Extensions.Logging;
using OutlookSync.Domain.Aggregates;
using OutlookSync.Domain.Repositories;
using OutlookSync.Domain.ValueObjects;

namespace OutlookSync.Infrastructure.Repositories;

/// <summary>
/// Factory implementation for creating calendar event repositories
/// </summary>
public class CalendarEventRepositoryFactory(
    ILogger<ExchangeCalendarEventRepository> logger) : ICalendarEventRepositoryFactory
{
    public ICalendarEventRepository Create(Credential credential)
    {
        ArgumentNullException.ThrowIfNull(credential);

        if (!credential.IsTokenValid())
        {
            throw new InvalidOperationException("Token is invalid or expired");
        }

        if (credential.StatusData == null || credential.StatusData.Length == 0)
        {
            throw new InvalidOperationException("Status data is missing");
        }

        // Create Exchange calendar event repository
        var repository = new ExchangeCalendarEventRepository(
            credential,
            logger,
            RetryPolicy.CreateDefault());

        return repository;
    }
}
