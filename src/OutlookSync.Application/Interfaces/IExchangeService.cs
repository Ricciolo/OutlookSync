using OutlookSync.Application.DTOs;

namespace OutlookSync.Application.Interfaces;

/// <summary>
/// Interface for Exchange/Outlook calendar service
/// </summary>
public interface IExchangeService
{
    Task<string> AcquireDeviceCodeAsync(CancellationToken cancellationToken = default);
    
    Task<(string AccessToken, DateTime ExpiresAt)> AcquireTokenByDeviceCodeAsync(
        string deviceCode,
        CancellationToken cancellationToken = default);
    
    Task<IEnumerable<CalendarItemDto>> GetCalendarItemsAsync(
        string accessToken,
        string calendarId,
        DateTime startDate,
        CancellationToken cancellationToken = default);
    
    Task<bool> ValidateTokenAsync(string accessToken, CancellationToken cancellationToken = default);
}
