using OutlookSync.Domain.Aggregates;
using OutlookSync.Domain.Repositories;
using OutlookSync.Domain.ValueObjects;

namespace OutlookSync.Infrastructure.Repositories;

/// <summary>
/// Mock implementation of calendar event repository for testing
/// </summary>
public class MockCalendarEventRepository : ICalendarEventRepository
{
    private readonly Dictionary<Guid, CalendarEvent> _events = [];
    private readonly Dictionary<string, CalendarEvent> _copiedEventsIndex = [];
    private readonly Guid _calendarId;

    public MockCalendarEventRepository(Guid calendarId)
    {
        _calendarId = calendarId;
    }

    public Task InitAsync(CancellationToken cancellationToken = default)
    {
        // No initialization needed for mock repository
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<CalendarEvent>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var events = _events.Values
            .Where(e => e.CalendarId == _calendarId)
            .ToList();
        return Task.FromResult<IReadOnlyList<CalendarEvent>>(events);
    }

    public Task<CalendarEvent?> FindCopiedEventAsync(
        CalendarEvent sourceEvent,
        Calendar sourceCalendar,
        CancellationToken cancellationToken = default)
    {
        var key = GetCopiedEventKey(_calendarId, sourceEvent.ExternalId, sourceCalendar.Id);
        _copiedEventsIndex.TryGetValue(key, out var calendarEvent);
        return Task.FromResult(calendarEvent);
    }

    public Task AddAsync(CalendarEvent calendarEvent, CancellationToken cancellationToken = default)
    {
        _events[calendarEvent.Id] = calendarEvent;
        
        if (calendarEvent.IsCopiedEvent && calendarEvent.OriginalEventId != null && calendarEvent.SourceCalendarId != null)
        {
            var key = GetCopiedEventKey(calendarEvent.CalendarId, calendarEvent.OriginalEventId, calendarEvent.SourceCalendarId.Value);
            _copiedEventsIndex[key] = calendarEvent;
        }
        
        return Task.CompletedTask;
    }

    private static string GetCopiedEventKey(Guid targetCalendarId, string originalEventId, Guid sourceCalendarId)
    {
        return $"{targetCalendarId}|{originalEventId}|{sourceCalendarId}";
    }
}

