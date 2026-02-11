using OutlookSync.Domain.Aggregates;
using OutlookSync.Domain.ValueObjects;
using OutlookSync.Domain.Events;

namespace OutlookSync.Domain.Tests.Aggregates;

public class DeviceTests
{
    [Fact]
    public void AcquireToken_WithValidParameters_ShouldSetTokenAndRaiseEvent()
    {
        // Arrange
        var device = new Device
        {
            Info = DeviceInfo.Create("Test Device", "Desktop")
        };
        var token = "test_token_123";
        var expiresAt = DateTime.UtcNow.AddHours(1);

        // Act
        device.AcquireToken(token, expiresAt);

        // Assert
        Assert.Equal(TokenStatus.Valid, device.TokenStatus);
        Assert.Equal(token, device.AccessToken);
        Assert.NotNull(device.TokenAcquiredAt);
        Assert.Equal(expiresAt, device.TokenExpiresAt);
        Assert.Single(device.DomainEvents);
        Assert.IsType<TokenAcquiredEvent>(device.DomainEvents.First());
    }

    [Fact]
    public void AcquireToken_WithExpiredDate_ShouldThrowException()
    {
        // Arrange
        var device = new Device
        {
            Info = DeviceInfo.Create("Test Device", "Desktop")
        };
        var token = "test_token_123";
        var expiresAt = DateTime.UtcNow.AddHours(-1);

        // Act & Assert
        Assert.Throws<ArgumentException>(() => device.AcquireToken(token, expiresAt));
    }

    [Fact]
    public void MarkTokenAsInvalid_ShouldUpdateStatusAndRaiseEvent()
    {
        // Arrange
        var device = new Device
        {
            Info = DeviceInfo.Create("Test Device", "Desktop")
        };
        device.AcquireToken("test_token", DateTime.UtcNow.AddHours(1));
        device.ClearEvents();

        // Act
        device.MarkTokenAsInvalid();

        // Assert
        Assert.Equal(TokenStatus.Invalid, device.TokenStatus);
        Assert.Single(device.DomainEvents);
        Assert.IsType<TokenExpiredEvent>(device.DomainEvents.First());
    }

    [Fact]
    public void IsTokenValid_WithValidToken_ShouldReturnTrue()
    {
        // Arrange
        var device = new Device
        {
            Info = DeviceInfo.Create("Test Device", "Desktop")
        };
        device.AcquireToken("test_token", DateTime.UtcNow.AddHours(1));

        // Act
        var isValid = device.IsTokenValid();

        // Assert
        Assert.True(isValid);
    }

    [Fact]
    public void IsTokenValid_WithExpiredToken_ShouldReturnFalseAndMarkAsExpired()
    {
        // Arrange
        var device = new Device
        {
            Info = DeviceInfo.Create("Test Device", "Desktop")
        };
        device.AcquireToken("test_token", DateTime.UtcNow.AddMilliseconds(1));
        Thread.Sleep(10); // Wait for token to expire

        // Act
        var isValid = device.IsTokenValid();

        // Assert
        Assert.False(isValid);
        Assert.Equal(TokenStatus.Expired, device.TokenStatus);
    }
}
