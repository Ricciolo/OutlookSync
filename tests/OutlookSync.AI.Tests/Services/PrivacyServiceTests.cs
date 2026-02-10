using Microsoft.Extensions.Logging;
using Moq;
using OutlookSync.AI.Models;
using OutlookSync.AI.Services;

namespace OutlookSync.AI.Tests.Services;

public class PrivacyServiceTests
{
    private readonly Mock<ILogger<PrivacyService>> _loggerMock = new();

    private PrivacyService CreateSut() => new(_loggerMock.Object);

    [Fact]
    public async Task GetSettingsAsync_Default_ReturnsSettingsWithSharingDisabled()
    {
        var sut = CreateSut();
        var settings = await sut.GetSettingsAsync();

        Assert.False(settings.AllowPersonalData);
        Assert.False(settings.AllowCalendarDataSharing);
        Assert.Equal(0, settings.DataRetentionDays);
    }

    [Fact]
    public async Task UpdateSettingsAsync_AppliesNewSettings()
    {
        var sut = CreateSut();
        var newSettings = new PrivacySettings
        {
            AllowPersonalData = true,
            AllowCalendarDataSharing = true,
            DataRetentionDays = 30
        };

        await sut.UpdateSettingsAsync(newSettings);
        var result = await sut.GetSettingsAsync();

        Assert.True(result.AllowPersonalData);
        Assert.True(result.AllowCalendarDataSharing);
        Assert.Equal(30, result.DataRetentionDays);
    }

    [Fact]
    public async Task UpdateSettingsAsync_NullSettings_ThrowsArgumentNullException()
    {
        var sut = CreateSut();
        await Assert.ThrowsAsync<ArgumentNullException>(() => sut.UpdateSettingsAsync(null!));
    }

    [Fact]
    public async Task IsDataSharingAllowedAsync_DefaultSettings_ReturnsFalse()
    {
        var sut = CreateSut();
        var allowed = await sut.IsDataSharingAllowedAsync();
        Assert.False(allowed);
    }

    [Fact]
    public async Task IsDataSharingAllowedAsync_AfterEnabling_ReturnsTrue()
    {
        var sut = CreateSut();
        await sut.UpdateSettingsAsync(new PrivacySettings { AllowPersonalData = true });
        var allowed = await sut.IsDataSharingAllowedAsync();
        Assert.True(allowed);
    }
}
