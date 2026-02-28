using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using OutlookSync.Domain.Repositories;

namespace OutlookSync.Web.Components.Pages;

/// <summary>
/// Code-behind for the Home page (Dashboard)
/// </summary>
public partial class Home : ComponentBase
{
    private int _activeCalendarsCount;
    private int _credentialsCount;
    private string _lastSyncDisplay = "-";

    [Inject]
    private ICalendarBindingRepository CalendarBindingRepository { get; set; } = default!;

    [Inject]
    private ICredentialRepository CredentialRepository { get; set; } = default!;

    /// <summary>
    /// Gets the number of active calendar bindings
    /// </summary>
    protected int ActiveCalendarsCount => _activeCalendarsCount;

    /// <summary>
    /// Gets the number of credentials
    /// </summary>
    protected int CredentialsCount => _credentialsCount;

    /// <summary>
    /// Gets the last sync display text
    /// </summary>
    protected string LastSyncDisplay => _lastSyncDisplay;

    /// <inheritdoc />
    protected override async Task OnInitializedAsync()
    {
        await LoadDashboardDataAsync();
    }

    private async Task LoadDashboardDataAsync()
    {
        // Count active calendar bindings
        _activeCalendarsCount = await CalendarBindingRepository.Query
            .Where(c => c.IsEnabled)
            .CountAsync();

        // Count credentials
        _credentialsCount = await CredentialRepository.Query
            .CountAsync();

        // Get the most recent sync date from calendar bindings
        var lastSync = await CalendarBindingRepository.Query
            .Where(c => c.LastSyncAt != null)
            .OrderByDescending(c => c.LastSyncAt)
            .Select(c => c.LastSyncAt)
            .FirstOrDefaultAsync();

        _lastSyncDisplay = lastSync.HasValue
            ? FormatLastSync(lastSync.Value)
            : "-";
    }

    private static string FormatLastSync(DateTime lastSync)
    {
        var timeSpan = DateTime.UtcNow - lastSync;

        if (timeSpan.TotalMinutes < 1)
            return "Just now";

        if (timeSpan.TotalHours < 1)
            return $"{(int)timeSpan.TotalMinutes}m ago";

        if (timeSpan.TotalDays < 1)
            return $"{(int)timeSpan.TotalHours}h ago";

        if (timeSpan.TotalDays < 7)
            return $"{(int)timeSpan.TotalDays}d ago";

        return lastSync.ToLocalTime().ToString("MMM dd, yyyy", System.Globalization.CultureInfo.InvariantCulture);
    }
}
