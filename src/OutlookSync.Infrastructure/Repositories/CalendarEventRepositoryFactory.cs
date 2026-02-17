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
    public ICalendarEventRepository Create(Credential credential, string? calendarExternalId, string? calendarName = null)
    {
        // Validate credential and token
        if (credential == null)
        {
            throw new InvalidOperationException(
                calendarName is not null 
                    ? $"Credential not found for calendar '{calendarName}'"
                    : "Credential not found");
        }

        if (!credential.IsTokenValid())
        {
            throw new InvalidOperationException(
                calendarName is not null
                    ? $"Token is invalid or expired for calendar '{calendarName}'"
                    : "Token is invalid or expired");
        }

        if (credential.StatusData == null || credential.StatusData.Length == 0)
        {
            throw new InvalidOperationException(
                calendarName is not null
                    ? $"Status data is missing for calendar '{calendarName}'"
                    : "Status data is missing");
        }

        // Create Exchange calendar event repository
        var repository = new ExchangeCalendarEventRepository(
            calendarExternalId,
            calendarName,
            credential,
            logger,
            RetryPolicy.CreateDefault());

        return repository;
    }
}
