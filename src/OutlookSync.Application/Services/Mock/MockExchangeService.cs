using OutlookSync.Application.DTOs;
using OutlookSync.Application.Interfaces;

namespace OutlookSync.Application.Services.Mock;

/// <summary>
/// Mock implementation of Exchange service for testing
/// </summary>
public class MockExchangeService : IExchangeService
{
    private readonly Dictionary<string, string> _deviceCodes = new();
    private readonly Dictionary<string, DateTime> _tokens = new();

    public Task<string> AcquireDeviceCodeAsync(CancellationToken cancellationToken = default)
    {
        var deviceCode = Guid.NewGuid().ToString();
        _deviceCodes[deviceCode] = "mock_user_code";
        return Task.FromResult(deviceCode);
    }

    public Task<(string AccessToken, DateTime ExpiresAt)> AcquireTokenByDeviceCodeAsync(
        string deviceCode,
        CancellationToken cancellationToken = default)
    {
        if (!_deviceCodes.ContainsKey(deviceCode))
            throw new InvalidOperationException("Invalid device code");

        var accessToken = Guid.NewGuid().ToString();
        var expiresAt = DateTime.UtcNow.AddHours(1);
        _tokens[accessToken] = expiresAt;

        return Task.FromResult((accessToken, expiresAt));
    }

    public Task<IEnumerable<CalendarItemDto>> GetCalendarItemsAsync(
        string accessToken,
        string calendarId,
        DateTime startDate,
        CancellationToken cancellationToken = default)
    {
        if (!ValidateTokenInternal(accessToken))
            throw new UnauthorizedAccessException("Invalid or expired token");

        var items = new List<CalendarItemDto>
        {
            new()
            {
                Id = "item1",
                Subject = "Mock Meeting 1",
                Start = DateTime.UtcNow.AddDays(1),
                End = DateTime.UtcNow.AddDays(1).AddHours(1),
                Location = "Conference Room A",
                Body = "This is a mock meeting",
                Organizer = "organizer@example.com",
                Attendees = ["attendee1@example.com", "attendee2@example.com"],
                IsAllDay = false,
                IsRecurring = false
            },
            new()
            {
                Id = "item2",
                Subject = "Mock Meeting 2",
                Start = DateTime.UtcNow.AddDays(2),
                End = DateTime.UtcNow.AddDays(2).AddHours(2),
                Location = "Conference Room B",
                Body = "Another mock meeting",
                Organizer = "organizer@example.com",
                Attendees = ["attendee3@example.com"],
                IsAllDay = false,
                IsRecurring = true
            }
        };

        return Task.FromResult<IEnumerable<CalendarItemDto>>(items);
    }

    public Task<bool> ValidateTokenAsync(string accessToken, CancellationToken cancellationToken = default) =>
        Task.FromResult(ValidateTokenInternal(accessToken));

    private bool ValidateTokenInternal(string accessToken)
    {
        if (!_tokens.TryGetValue(accessToken, out var expiresAt))
            return false;

        return expiresAt > DateTime.UtcNow;
    }
}
