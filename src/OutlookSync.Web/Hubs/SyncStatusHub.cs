using Microsoft.AspNetCore.SignalR;
using OutlookSync.Domain.ValueObjects;

namespace OutlookSync.Web.Hubs;

/// <summary>
/// SignalR hub for real-time sync status notifications.
/// </summary>
public class SyncStatusHub : Hub
{
    /// <summary>
    /// Sends a sync started notification to all connected clients.
    /// </summary>
    /// <param name="calendarId">The ID of the calendar being synced.</param>
    /// <param name="calendarName">The name of the calendar being synced.</param>
    public async Task NotifySyncStarted(Guid calendarId, string calendarName)
    {
        await Clients.All.SendAsync("SyncStarted", new
        {
            CalendarId = calendarId,
            CalendarName = calendarName,
            Timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Sends a sync completed notification to all connected clients.
    /// </summary>
    /// <param name="calendarId">The ID of the calendar that was synced.</param>
    /// <param name="calendarName">The name of the calendar that was synced.</param>
    /// <param name="itemsSynced">The number of items synced.</param>
    public async Task NotifySyncCompleted(Guid calendarId, string calendarName, int itemsSynced)
    {
        await Clients.All.SendAsync("SyncCompleted", new
        {
            CalendarId = calendarId,
            CalendarName = calendarName,
            ItemsSynced = itemsSynced,
            Timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Sends a sync failed notification to all connected clients.
    /// </summary>
    /// <param name="calendarId">The ID of the calendar that failed to sync.</param>
    /// <param name="calendarName">The name of the calendar that failed to sync.</param>
    /// <param name="errorMessage">The error message.</param>
    public async Task NotifySyncFailed(Guid calendarId, string calendarName, string errorMessage)
    {
        await Clients.All.SendAsync("SyncFailed", new
        {
            CalendarId = calendarId,
            CalendarName = calendarName,
            Error = errorMessage,
            Timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Sends a token status update notification to all connected clients.
    /// </summary>
    /// <param name="credentialId">The ID of the credential.</param>
    /// <param name="tokenStatus">The new token status.</param>
    public async Task NotifyTokenStatusChanged(Guid credentialId, TokenStatus tokenStatus)
    {
        await Clients.All.SendAsync("TokenStatusChanged", new
        {
            CredentialId = credentialId,
            Status = tokenStatus.ToString(),
            Timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Called when a client connects to the hub.
    /// </summary>
    public override async Task OnConnectedAsync()
    {
        await base.OnConnectedAsync();
        
        // Optionally send current status to newly connected client
        await Clients.Caller.SendAsync("Connected", new
        {
            Message = "Connected to sync status hub",
            Timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Called when a client disconnects from the hub.
    /// </summary>
    /// <param name="exception">The exception that caused the disconnection, if any.</param>
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        await base.OnDisconnectedAsync(exception);
    }
}
