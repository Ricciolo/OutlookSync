using OutlookSync.Domain.Aggregates;
using OutlookSync.Domain.ValueObjects;

namespace OutlookSync.Domain.Tests.Aggregates;

public class CalendarTests
{
    private static SyncConfiguration CreateDefaultConfiguration() => new()
    {
        Interval = SyncInterval.Every30Minutes(),
        StartDate = DateTime.UtcNow,
        IsPrivate = false,
        FieldSelection = CalendarFieldSelection.All()
    };

    [Fact]
    public void RecordSuccessfulSync_WithValidItems_ShouldUpdateStats()
    {
        // Arrange
        var calendar = new Calendar
        {
            Name = "Test Calendar",
            ExternalId = "cal_123",
            CredentialId = Guid.CreateVersion7(),
            Configuration = CreateDefaultConfiguration()
        };
        var itemsSynced = 10;

        // Act
        calendar.RecordSuccessfulSync(itemsSynced);

        // Assert
        Assert.NotNull(calendar.LastSyncAt);
    }

    [Fact]
    public void RecordFailedSync_WithReason_ShouldUpdateLastSync()
    {
        // Arrange
        var calendar = new Calendar
        {
            Name = "Test Calendar",
            ExternalId = "cal_123",
            CredentialId = Guid.CreateVersion7(),
            Configuration = CreateDefaultConfiguration()
        };
        var reason = "Network error";

        // Act
        calendar.RecordFailedSync(reason);

        // Assert
        Assert.NotNull(calendar.LastSyncAt);
    }

    [Fact]
    public void Enable_ShouldSetIsEnabledToTrue()
    {
        // Arrange
        var calendar = new Calendar
        {
            Name = "Test Calendar",
            ExternalId = "cal_123",
            CredentialId = Guid.CreateVersion7(),
            Configuration = CreateDefaultConfiguration()
        };
        calendar.Disable();

        // Act
        calendar.Enable();

        // Assert
        Assert.True(calendar.IsEnabled);
    }

    [Fact]
    public void Disable_ShouldSetIsEnabledToFalse()
    {
        // Arrange
        var calendar = new Calendar
        {
            Name = "Test Calendar",
            ExternalId = "cal_123",
            CredentialId = Guid.CreateVersion7(),
            Configuration = CreateDefaultConfiguration()
        };

        // Act
        calendar.Disable();

        // Assert
        Assert.False(calendar.IsEnabled);
    }

    [Fact]
    public void UpdateConfiguration_WithValidConfiguration_ShouldUpdateAndMarkAsUpdated()
    {
        // Arrange
        var calendar = new Calendar
        {
            Name = "Test Calendar",
            ExternalId = "cal_123",
            CredentialId = Guid.CreateVersion7(),
            Configuration = CreateDefaultConfiguration()
        };

        var newConfig = new SyncConfiguration
        {
            Interval = SyncInterval.Hourly(),
            StartDate = DateTime.UtcNow.AddDays(1),
            IsPrivate = true,
            FieldSelection = CalendarFieldSelection.Essential()
        };

        // Act
        calendar.UpdateConfiguration(newConfig);

        // Assert
        Assert.Equal(newConfig, calendar.Configuration);
        Assert.NotNull(calendar.UpdatedAt);
    }
}
