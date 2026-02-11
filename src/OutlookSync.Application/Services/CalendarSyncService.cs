using OutlookSync.Application.Interfaces;
using OutlookSync.Domain.Aggregates;
using OutlookSync.Domain.Services;

namespace OutlookSync.Application.Services;

/// <summary>
/// Service for calendar synchronization
/// </summary>
public class CalendarSyncService(IExchangeService exchangeService) : ICalendarSyncService
{
    private readonly IExchangeService _exchangeService = exchangeService;

    public async Task<SyncResult> SyncCalendarAsync(
        Calendar calendar,
        Device device,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (!device.IsTokenValid())
                return SyncResult.Failure("Device token is invalid or expired");

            if (string.IsNullOrEmpty(device.AccessToken))
                return SyncResult.Failure("Device access token is missing");

            var items = await _exchangeService.GetCalendarItemsAsync(
                device.AccessToken,
                calendar.ExternalId,
                DateTime.UtcNow.AddDays(-30),
                cancellationToken);

            var itemCount = items.Count();
            calendar.RecordSuccessfulSync(itemCount);

            return SyncResult.Success(itemCount);
        }
        catch (UnauthorizedAccessException)
        {
            device.MarkTokenAsInvalid();
            calendar.RecordFailedSync("Unauthorized access - token invalid");
            return SyncResult.Failure("Unauthorized access");
        }
        catch (Exception ex)
        {
            calendar.RecordFailedSync($"Error: {ex.Message}");
            return SyncResult.Failure(ex.Message);
        }
    }
}
