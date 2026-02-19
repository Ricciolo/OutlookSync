using OutlookSync.Domain.ValueObjects;

namespace OutlookSync.Domain.Tests.ValueObjects;

/// <summary>
/// Unit tests for SyncInterval value object
/// </summary>
public class SyncIntervalTests
{
    [Fact]
    public void Every15Minutes_ShouldCreateCorrectInterval()
    {
        // Act
        var interval = SyncInterval.Every15Minutes();

        // Assert
        Assert.Equal(15, interval.Minutes);
        Assert.Equal("*/15 * * * *", interval.CronExpression);
    }

    [Fact]
    public void Every30Minutes_ShouldCreateCorrectInterval()
    {
        // Act
        var interval = SyncInterval.Every30Minutes();

        // Assert
        Assert.Equal(30, interval.Minutes);
        Assert.Equal("*/30 * * * *", interval.CronExpression);
    }

    [Fact]
    public void Hourly_ShouldCreateCorrectInterval()
    {
        // Act
        var interval = SyncInterval.Hourly();

        // Assert
        Assert.Equal(60, interval.Minutes);
        Assert.Equal("0 * * * *", interval.CronExpression);
    }

    [Fact]
    public void Custom_WithMinutesOnly_ShouldCreateInterval()
    {
        // Act
        var interval = SyncInterval.Custom(45);

        // Assert
        Assert.Equal(45, interval.Minutes);
        Assert.Null(interval.CronExpression);
    }

    [Fact]
    public void Custom_WithMinutesAndCron_ShouldCreateInterval()
    {
        // Act
        var interval = SyncInterval.Custom(90, "0 */1 * * *");

        // Assert
        Assert.Equal(90, interval.Minutes);
        Assert.Equal("0 */1 * * *", interval.CronExpression);
    }

    [Fact]
    public void ValueEquality_SameValues_ShouldBeEqual()
    {
        // Arrange
        var interval1 = SyncInterval.Every15Minutes();
        var interval2 = SyncInterval.Every15Minutes();

        // Assert
        Assert.Equal(interval1, interval2);
        Assert.True(interval1 == interval2);
    }

    [Fact]
    public void ValueEquality_DifferentValues_ShouldNotBeEqual()
    {
        // Arrange
        var interval1 = SyncInterval.Every15Minutes();
        var interval2 = SyncInterval.Every30Minutes();

        // Assert
        Assert.NotEqual(interval1, interval2);
        Assert.False(interval1 == interval2);
    }

    [Fact]
    public void With_ShouldCreateNewInstanceWithModifiedValue()
    {
        // Arrange
        var original = SyncInterval.Every15Minutes();

        // Act
        var modified = original with { Minutes = 20 };

        // Assert
        Assert.Equal(15, original.Minutes);
        Assert.Equal(20, modified.Minutes);
        Assert.Equal("*/15 * * * *", modified.CronExpression);
    }
}
