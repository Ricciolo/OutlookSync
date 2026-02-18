using OutlookSync.Domain.Aggregates;
using OutlookSync.Domain.ValueObjects;

namespace OutlookSync.Domain.Tests.Aggregates;

/// <summary>
/// Unit tests for CalendarBinding aggregate
/// </summary>
public class CalendarBindingTests
{
    [Fact]
    public void Create_WithDefaultConfiguration_ShouldHaveDefaultSyncInterval()
    {
        // Arrange & Act
        var binding = new CalendarBinding
        {
            Name = "Test Binding",
            SourceCredentialId = Guid.CreateVersion7(),
            SourceCalendarExternalId = "source-calendar-id",
            TargetCredentialId = Guid.CreateVersion7(),
            TargetCalendarExternalId = "target-calendar-id",
            Configuration = CalendarBindingConfiguration.Default()
        };

        // Assert
        Assert.NotNull(binding.Configuration.Interval);
        Assert.Equal(30, binding.Configuration.Interval.Minutes);
        Assert.Equal(30, binding.Configuration.SyncDaysForward);
    }

    [Fact]
    public void Create_WithCustomSyncInterval_ShouldSetCorrectInterval()
    {
        // Arrange
        var customInterval = SyncInterval.Every15Minutes();
        var config = CalendarBindingConfiguration.Default() with { Interval = customInterval };

        // Act
        var binding = new CalendarBinding
        {
            Name = "Test Binding",
            SourceCredentialId = Guid.CreateVersion7(),
            SourceCalendarExternalId = "source-calendar-id",
            TargetCredentialId = Guid.CreateVersion7(),
            TargetCalendarExternalId = "target-calendar-id",
            Configuration = config
        };

        // Assert
        Assert.Equal(15, binding.Configuration.Interval.Minutes);
        Assert.Equal("*/15 * * * *", binding.Configuration.Interval.CronExpression);
    }

    [Fact]
    public void Create_WithCustomSyncDaysForward_ShouldSetCorrectValue()
    {
        // Arrange
        var config = CalendarBindingConfiguration.Default() with { SyncDaysForward = 60 };

        // Act
        var binding = new CalendarBinding
        {
            Name = "Test Binding",
            SourceCredentialId = Guid.CreateVersion7(),
            SourceCalendarExternalId = "source-calendar-id",
            TargetCredentialId = Guid.CreateVersion7(),
            TargetCalendarExternalId = "target-calendar-id",
            Configuration = config
        };

        // Assert
        Assert.Equal(60, binding.Configuration.SyncDaysForward);
    }

    [Fact]
    public void UpdateConfiguration_WithDifferentInterval_ShouldUpdateSuccessfully()
    {
        // Arrange
        var binding = new CalendarBinding
        {
            Name = "Test Binding",
            SourceCredentialId = Guid.CreateVersion7(),
            SourceCalendarExternalId = "source-calendar-id",
            TargetCredentialId = Guid.CreateVersion7(),
            TargetCalendarExternalId = "target-calendar-id",
            Configuration = CalendarBindingConfiguration.Default()
        };

        var newConfig = binding.Configuration with 
        { 
            Interval = SyncInterval.Hourly(),
            SyncDaysForward = 90
        };

        // Act
        binding.UpdateConfiguration(newConfig);

        // Assert
        Assert.Equal(60, binding.Configuration.Interval.Minutes);
        Assert.Equal(90, binding.Configuration.SyncDaysForward);
    }

    [Fact]
    public void PrivacyFocusedConfiguration_ShouldHaveDefaultInterval()
    {
        // Arrange & Act
        var config = CalendarBindingConfiguration.PrivacyFocused();

        // Assert
        Assert.NotNull(config.Interval);
        Assert.Equal(30, config.Interval.Minutes);
        Assert.Equal(TitleHandling.Hide, config.TitleHandling);
        Assert.Equal("Busy", config.CustomTitle);
        Assert.False(config.CopyDescription);
        Assert.True(config.MarkAsPrivate);
    }

    [Fact]
    public void Enable_ShouldSetIsEnabledToTrue()
    {
        // Arrange
        var binding = CreateTestBinding();
        binding.Disable();

        // Act
        binding.Enable();

        // Assert
        Assert.True(binding.IsEnabled);
    }

    [Fact]
    public void Disable_ShouldSetIsEnabledToFalse()
    {
        // Arrange
        var binding = CreateTestBinding();

        // Act
        binding.Disable();

        // Assert
        Assert.False(binding.IsEnabled);
    }

    [Fact]
    public void RecordSuccessfulSync_ShouldUpdateSyncMetrics()
    {
        // Arrange
        var binding = CreateTestBinding();
        var eventCount = 42;

        // Act
        binding.RecordSuccessfulSync(eventCount);

        // Assert
        Assert.NotNull(binding.LastSyncAt);
        Assert.Equal(eventCount, binding.LastSyncEventCount);
        Assert.Null(binding.LastSyncError);
    }

    [Fact]
    public void RecordFailedSync_ShouldUpdateSyncError()
    {
        // Arrange
        var binding = CreateTestBinding();
        var errorMessage = "Test error message";

        // Act
        binding.RecordFailedSync(errorMessage);

        // Assert
        Assert.NotNull(binding.LastSyncAt);
        Assert.Equal(errorMessage, binding.LastSyncError);
    }

    private static CalendarBinding CreateTestBinding()
    {
        return new CalendarBinding
        {
            Name = "Test Binding",
            SourceCredentialId = Guid.CreateVersion7(),
            SourceCalendarExternalId = "source-calendar-id",
            TargetCredentialId = Guid.CreateVersion7(),
            TargetCalendarExternalId = "target-calendar-id",
            Configuration = CalendarBindingConfiguration.Default()
        };
    }
}
