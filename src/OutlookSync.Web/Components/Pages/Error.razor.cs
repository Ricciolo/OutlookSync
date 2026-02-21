using System.Diagnostics;
using Microsoft.AspNetCore.Components;

namespace OutlookSync.Web.Components.Pages;

/// <summary>
/// Code-behind for Error page component
/// </summary>
#pragma warning disable CA1716 // Identifier conflicts with keyword
public partial class Error
#pragma warning restore CA1716
{
    [CascadingParameter]
    private HttpContext? HttpContext { get; set; }

    private string? RequestId { get; set; }
    private bool ShowRequestId => !string.IsNullOrEmpty(RequestId);

    /// <inheritdoc/>
    protected override void OnInitialized() =>
        RequestId = Activity.Current?.Id ?? HttpContext?.TraceIdentifier;
}
