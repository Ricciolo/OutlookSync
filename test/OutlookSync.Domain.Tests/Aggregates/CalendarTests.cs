using OutlookSync.Domain.Aggregates;
using OutlookSync.Domain.Events;

namespace OutlookSync.Domain.Tests.Aggregates;

public class CalendarTests
{
    [Fact]
    public void RecordSuccessfulSync_WithValidItems_ShouldUpdateStatsAndRaiseEvent()
    {
        // Arrange
        var calendar = new Calendar
        {
            Name = "Test Calendar",
            ExternalId = "cal_123",
            DeviceId = Guid.NewGuid()
        };
        var itemsSynced = 10;

        // Act
        calendar.RecordSuccessfulSync(itemsSynced);

        // Assert
        Assert.NotNull(calendar.LastSyncAt);
        Assert.Equal(itemsSynced, calendar.TotalItemsSynced);
        Assert.Single(calendar.DomainEvents);
        
        var syncEvent = Assert.IsType<CalendarSyncedEvent>(calendar.DomainEvents.First());
        Assert.Equal(calendar.Id, syncEvent.CalendarId);
        Assert.Equal(itemsSynced, syncEvent.ItemsSynced);
    }

    [Fact]
    public void RecordSuccessfulSync_MultipleTimes_ShouldAccumulateTotal()
    {
        // Arrange
        var calendar = new Calendar
        {
            Name = "Test Calendar",
            ExternalId = "cal_123",
            DeviceId = Guid.NewGuid()
        };

        // Act
        calendar.RecordSuccessfulSync(10);
        calendar.RecordSuccessfulSync(5);

        // Assert
        Assert.Equal(15, calendar.TotalItemsSynced);
        Assert.Equal(2, calendar.DomainEvents.Count);
    }

    [Fact]
    public void RecordFailedSync_WithReason_ShouldRaiseFailedEvent()
    {
        // Arrange
        var calendar = new Calendar
        {
            Name = "Test Calendar",
            ExternalId = "cal_123",
            DeviceId = Guid.NewGuid()
        };
        var reason = "Network error";

        // Act
        calendar.RecordFailedSync(reason);

        // Assert
        Assert.NotNull(calendar.LastSyncAt);
        Assert.Single(calendar.DomainEvents);
        
        var failedEvent = Assert.IsType<CalendarSyncFailedEvent>(calendar.DomainEvents.First());
        Assert.Equal(calendar.Id, failedEvent.CalendarId);
        Assert.Equal(reason, failedEvent.Reason);
    }

    [Fact]
    public void Enable_ShouldSetIsEnabledToTrue()
    {
        // Arrange
        var calendar = new Calendar
        {
            Name = "Test Calendar",
            ExternalId = "cal_123",
            DeviceId = Guid.NewGuid()
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
            DeviceId = Guid.NewGuid()
        };

        // Act
        calendar.Disable();

        // Assert
        Assert.False(calendar.IsEnabled);
    }
}
