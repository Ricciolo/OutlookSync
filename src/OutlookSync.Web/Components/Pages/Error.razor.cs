using System.Diagnostics;
using Microsoft.AspNetCore.Components;

namespace OutlookSync.Web.Components.Pages;

/// <summary>
/// Code-behind for Error page component
/// </summary>
public partial class Error
{
    [CascadingParameter]
    private HttpContext? HttpContext { get; set; }

    private string? RequestId { get; set; }
    private bool ShowRequestId => !string.IsNullOrEmpty(RequestId);

    /// <inheritdoc/>
    protected override void OnInitialized() =>
        RequestId = Activity.Current?.Id ?? HttpContext?.TraceIdentifier;
}
