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
    public ICalendarEventRepository Create(Credential credential, Calendar? calendar)
    {
        // Validate credential and token
        if (credential == null)
        {
            throw new InvalidOperationException(
                calendar is not null 
                    ? $"Credential not found for calendar '{calendar.Name}'"
                    : "Credential not found");
        }

        if (!credential.IsTokenValid())
        {
            throw new InvalidOperationException(
                calendar is not null
                    ? $"Token is invalid or expired for calendar '{calendar.Name}'"
                    : "Token is invalid or expired");
        }

        if (credential.StatusData == null || credential.StatusData.Length == 0)
        {
            throw new InvalidOperationException(
                calendar is not null
                    ? $"Status data is missing for calendar '{calendar.Name}'"
                    : "Status data is missing");
        }

        // Create Exchange calendar event repository
        var repository = new ExchangeCalendarEventRepository(
            calendar,
            credential,
            logger,
            RetryPolicy.CreateDefault());

        return repository;
    }
}
