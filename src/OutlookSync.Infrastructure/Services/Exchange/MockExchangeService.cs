using OutlookSync.Domain.Services;
using OutlookSync.Domain.ValueObjects;

namespace OutlookSync.Infrastructure.Services.Exchange;

/// <summary>
/// Mock implementation of Exchange service for testing purposes
/// </summary>
public class MockExchangeService : IExchangeService
{
    private readonly Dictionary<string, List<CalendarEvent>> _calendarEvents = [];
    private bool _isInitialized;
    
    /// <summary>
    /// Gets all events stored in the mock service (for testing)
    /// </summary>
    public IReadOnlyDictionary<string, List<CalendarEvent>> CalendarEvents => _calendarEvents;
    
    /// <inheritdoc/>
    public Task InitializeAsync(string accessToken, string serviceUrl, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(accessToken, nameof(accessToken));
        ArgumentException.ThrowIfNullOrWhiteSpace(serviceUrl, nameof(serviceUrl));
        
        _isInitialized = true;
        return Task.CompletedTask;
    }
    
    /// <inheritdoc/>
    public Task<IReadOnlyList<CalendarEvent>> GetCalendarEventsAsync(
        string calendarId,
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default)
    {
        EnsureInitialized();
        ArgumentException.ThrowIfNullOrWhiteSpace(calendarId, nameof(calendarId));
        
        if (!_calendarEvents.TryGetValue(calendarId, out var events))
        {
            return Task.FromResult<IReadOnlyList<CalendarEvent>>([]);
        }
        
        var filteredEvents = events
            .Where(e => e.Start >= startDate && e.End <= endDate)
            .ToList();
        
        return Task.FromResult<IReadOnlyList<CalendarEvent>>(filteredEvents);
    }
    
    /// <inheritdoc/>
    public Task<CalendarEvent> CreateCalendarEventAsync(
        string calendarId,
        CalendarEvent calendarEvent,
        CancellationToken cancellationToken = default)
    {
        EnsureInitialized();
        ArgumentException.ThrowIfNullOrWhiteSpace(calendarId, nameof(calendarId));
        ArgumentNullException.ThrowIfNull(calendarEvent, nameof(calendarEvent));
        
        if (!_calendarEvents.ContainsKey(calendarId))
        {
            _calendarEvents[calendarId] = [];
        }
        
        var eventWithId = calendarEvent with { ExternalId = Guid.NewGuid().ToString() };
        _calendarEvents[calendarId].Add(eventWithId);
        
        return Task.FromResult(eventWithId);
    }
    
    /// <inheritdoc/>
    public Task UpdateCalendarEventAsync(
        string calendarId,
        CalendarEvent calendarEvent,
        CancellationToken cancellationToken = default)
    {
        EnsureInitialized();
        ArgumentException.ThrowIfNullOrWhiteSpace(calendarId, nameof(calendarId));
        ArgumentNullException.ThrowIfNull(calendarEvent, nameof(calendarEvent));
        ArgumentException.ThrowIfNullOrWhiteSpace(calendarEvent.ExternalId, nameof(calendarEvent.ExternalId));
        
        if (!_calendarEvents.TryGetValue(calendarId, out var events))
        {
            throw new InvalidOperationException($"Calendar '{calendarId}' not found");
        }
        
        var existingEvent = events.FirstOrDefault(e => e.ExternalId == calendarEvent.ExternalId);
        if (existingEvent is null)
        {
            throw new InvalidOperationException($"Event '{calendarEvent.ExternalId}' not found");
        }
        
        events.Remove(existingEvent);
        events.Add(calendarEvent);
        
        return Task.CompletedTask;
    }
    
    /// <inheritdoc/>
    public Task DeleteCalendarEventAsync(
        string calendarId,
        string eventExternalId,
        CancellationToken cancellationToken = default)
    {
        EnsureInitialized();
        ArgumentException.ThrowIfNullOrWhiteSpace(calendarId, nameof(calendarId));
        ArgumentException.ThrowIfNullOrWhiteSpace(eventExternalId, nameof(eventExternalId));
        
        if (!_calendarEvents.TryGetValue(calendarId, out var events))
        {
            throw new InvalidOperationException($"Calendar '{calendarId}' not found");
        }
        
        var existingEvent = events.FirstOrDefault(e => e.ExternalId == eventExternalId);
        if (existingEvent is null)
        {
            throw new InvalidOperationException($"Event '{eventExternalId}' not found");
        }
        
        events.Remove(existingEvent);
        
        return Task.CompletedTask;
    }
    
    /// <inheritdoc/>
    public Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_isInitialized);
    }
    
    /// <summary>
    /// Resets the mock service state (for testing)
    /// </summary>
    public void Reset()
    {
        _calendarEvents.Clear();
        _isInitialized = false;
    }
    
    private void EnsureInitialized()
    {
        if (!_isInitialized)
        {
            throw new InvalidOperationException(
                "Exchange service is not initialized. Call InitializeAsync first.");
        }
    }
}
