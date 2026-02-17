using Microsoft.AspNetCore.Components;
using OutlookSync.Domain.Aggregates;

namespace OutlookSync.Web.Components.Modals;

/// <summary>
/// Modal component for credential deletion confirmation
/// </summary>
public partial class DeleteConfirmationModal : ComponentBase
{
    /// <summary>
    /// Gets or sets whether the modal is visible
    /// </summary>
    [Parameter]
    public bool IsVisible { get; set; }

    /// <summary>
    /// Gets or sets the credential to be deleted
    /// </summary>
    [Parameter]
    public Credential? Credential { get; set; }

    /// <summary>
    /// Gets or sets the callback invoked when the deletion is confirmed
    /// </summary>
    [Parameter]
    public EventCallback OnConfirm { get; set; }

    /// <summary>
    /// Gets or sets the callback invoked when the deletion is cancelled
    /// </summary>
    [Parameter]
    public EventCallback OnCancel { get; set; }
}
