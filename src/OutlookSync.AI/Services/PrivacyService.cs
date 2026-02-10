using Microsoft.Extensions.Logging;
using OutlookSync.AI.Interfaces;
using OutlookSync.AI.Models;

namespace OutlookSync.AI.Services;

/// <summary>
/// Default implementation of <see cref="IPrivacyService"/> that manages user privacy settings.
/// </summary>
public sealed class PrivacyService : IPrivacyService
{
    private readonly ILogger<PrivacyService> _logger;
    private PrivacySettings _settings = new();

    public PrivacyService(ILogger<PrivacyService> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public Task<PrivacySettings> GetSettingsAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_settings);
    }

    /// <inheritdoc />
    public Task UpdateSettingsAsync(PrivacySettings settings, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(settings);

        _logger.LogInformation(
            "Privacy settings updated: AllowPersonalData={AllowPersonalData}, AllowCalendarDataSharing={AllowCalendarDataSharing}, DataRetentionDays={DataRetentionDays}.",
            settings.AllowPersonalData,
            settings.AllowCalendarDataSharing,
            settings.DataRetentionDays);

        _settings = settings;
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task<bool> IsDataSharingAllowedAsync(CancellationToken cancellationToken = default)
    {
        var settings = await GetSettingsAsync(cancellationToken);
        return settings.AllowPersonalData;
    }
}
