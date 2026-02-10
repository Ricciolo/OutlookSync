using OutlookSync.AI.Models;

namespace OutlookSync.AI.Interfaces;

/// <summary>
/// Defines the contract for managing user privacy settings.
/// </summary>
public interface IPrivacyService
{
    /// <summary>
    /// Gets the current privacy settings.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The current privacy settings.</returns>
    Task<PrivacySettings> GetSettingsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the privacy settings.
    /// </summary>
    /// <param name="settings">The new settings.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    Task UpdateSettingsAsync(PrivacySettings settings, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns whether AI data sharing is currently permitted based on privacy settings.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns><see langword="true"/> if data sharing is permitted; otherwise, <see langword="false"/>.</returns>
    Task<bool> IsDataSharingAllowedAsync(CancellationToken cancellationToken = default);
}
