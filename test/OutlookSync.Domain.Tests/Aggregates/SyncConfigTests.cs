using OutlookSync.Domain.Aggregates;
using OutlookSync.Domain.ValueObjects;

namespace OutlookSync.Domain.Tests.Aggregates;

public class SyncConfigTests
{
    [Fact]
    public void UpdateConfiguration_WithValidConfiguration_ShouldUpdate()
    {
        // Arrange
        var config = new SyncConfig
        {
            CalendarId = Guid.NewGuid(),
            Configuration = new SyncConfiguration
            {
                Interval = SyncInterval.Every15Minutes(),
                StartDate = DateTime.UtcNow,
                IsPrivate = false,
                FieldSelection = CalendarFieldSelection.All()
            }
        };
        
        var newConfiguration = new SyncConfiguration
        {
            Interval = SyncInterval.Hourly(),
            StartDate = DateTime.UtcNow,
            IsPrivate = true,
            FieldSelection = CalendarFieldSelection.Essential()
        };

        // Act
        config.UpdateConfiguration(newConfiguration);

        // Assert
        Assert.Equal(newConfiguration, config.Configuration);
        Assert.NotNull(config.UpdatedAt);
    }

    [Fact]
    public void Enable_ShouldSetIsEnabledToTrue()
    {
        // Arrange
        var config = new SyncConfig
        {
            CalendarId = Guid.NewGuid(),
            Configuration = new SyncConfiguration
            {
                Interval = SyncInterval.Every15Minutes(),
                StartDate = DateTime.UtcNow,
                IsPrivate = false,
                FieldSelection = CalendarFieldSelection.All()
            }
        };
        config.Disable();

        // Act
        config.Enable();

        // Assert
        Assert.True(config.IsEnabled);
    }

    [Fact]
    public void RecordSyncSuccess_ShouldUpdateLastSyncInfo()
    {
        // Arrange
        var config = new SyncConfig
        {
            CalendarId = Guid.NewGuid(),
            Configuration = new SyncConfiguration
            {
                Interval = SyncInterval.Every15Minutes(),
                StartDate = DateTime.UtcNow,
                IsPrivate = false,
                FieldSelection = CalendarFieldSelection.All()
            }
        };

        // Act
        config.RecordSyncSuccess();

        // Assert
        Assert.NotNull(config.LastSyncAt);
        Assert.Equal("Success", config.LastSyncStatus);
    }

    [Fact]
    public void RecordSyncFailure_WithReason_ShouldUpdateStatus()
    {
        // Arrange
        var config = new SyncConfig
        {
            CalendarId = Guid.NewGuid(),
            Configuration = new SyncConfiguration
            {
                Interval = SyncInterval.Every15Minutes(),
                StartDate = DateTime.UtcNow,
                IsPrivate = false,
                FieldSelection = CalendarFieldSelection.All()
            }
        };
        var reason = "Token expired";

        // Act
        config.RecordSyncFailure(reason);

        // Assert
        Assert.NotNull(config.LastSyncAt);
        Assert.Contains(reason, config.LastSyncStatus);
    }
}
