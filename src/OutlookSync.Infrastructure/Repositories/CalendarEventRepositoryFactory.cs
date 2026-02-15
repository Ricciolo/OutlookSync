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
    public ICalendarEventRepository Create(Calendar calendar, Credential credential)
    {
        // Validate credential and token
        if (credential == null)
        {
            throw new InvalidOperationException($"Credential not found for calendar '{calendar.Name}'");
        }

        if (!credential.IsTokenValid())
        {
            throw new InvalidOperationException($"Token is invalid or expired for calendar '{calendar.Name}'");
        }

        if (string.IsNullOrEmpty(credential.AccessToken))
        {
            throw new InvalidOperationException($"Access token is missing for calendar '{calendar.Name}'");
        }

        // Create Exchange calendar event repository
        return new ExchangeCalendarEventRepository(
            calendar,
            credential,
            logger,
            RetryPolicy.CreateDefault());
    }
}
