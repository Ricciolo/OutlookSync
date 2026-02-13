using OutlookSync.Domain.Aggregates;
using OutlookSync.Domain.ValueObjects;

namespace OutlookSync.Domain.Tests.Aggregates;

public class CredentialTests
{
    [Fact]
    public void AcquireToken_WithValidParameters_ShouldSetToken()
    {
        // Arrange
        var credential = new Credential
        {
            Name = "Test Credential"
        };
        var accessToken = "test_access_token_123";
        var refreshToken = "test_refresh_token_456";
        var expiresAt = DateTime.UtcNow.AddHours(1);

        // Act
        credential.AcquireToken(accessToken, refreshToken, expiresAt);

        // Assert
        Assert.Equal(TokenStatus.Valid, credential.TokenStatus);
        Assert.Equal(accessToken, credential.AccessToken);
        Assert.Equal(refreshToken, credential.RefreshToken);
        Assert.NotNull(credential.TokenAcquiredAt);
        Assert.Equal(expiresAt, credential.TokenExpiresAt);
    }

    [Fact]
    public void AcquireToken_WithExpiredDate_ShouldThrowException()
    {
        // Arrange
        var credential = new Credential
        {
            Name = "Test Credential"
        };
        var accessToken = "test_access_token_123";
        var refreshToken = "test_refresh_token_456";
        var expiresAt = DateTime.UtcNow.AddHours(-1);

        // Act & Assert
        Assert.Throws<ArgumentException>(() => credential.AcquireToken(accessToken, refreshToken, expiresAt));
    }

    [Fact]
    public void MarkTokenAsInvalid_ShouldUpdateStatus()
    {
        // Arrange
        var credential = new Credential
        {
            Name = "Test Credential"
        };
        credential.AcquireToken("test_access", "test_refresh", DateTime.UtcNow.AddHours(1));

        // Act
        credential.MarkTokenAsInvalid();

        // Assert
        Assert.Equal(TokenStatus.Invalid, credential.TokenStatus);
    }

    [Fact]
    public void IsTokenValid_WithValidToken_ShouldReturnTrue()
    {
        // Arrange
        var credential = new Credential
        {
            Name = "Test Credential"
        };
        credential.AcquireToken("test_access", "test_refresh", DateTime.UtcNow.AddHours(1));

        // Act
        var isValid = credential.IsTokenValid();

        // Assert
        Assert.True(isValid);
    }

    [Fact]
    public void IsTokenValid_WithExpiredToken_ShouldReturnFalseAndMarkAsExpired()
    {
        // Arrange
        var credential = new Credential
        {
            Name = "Test Credential"
        };
        credential.AcquireToken("test_access", "test_refresh", DateTime.UtcNow.AddMilliseconds(1));
        Thread.Sleep(10); // Wait for token to expire

        // Act
        var isValid = credential.IsTokenValid();

        // Assert
        Assert.False(isValid);
        Assert.Equal(TokenStatus.Expired, credential.TokenStatus);
    }
}
