using OutlookSync.Domain.Aggregates;
using OutlookSync.Domain.ValueObjects;
using System.Text;

namespace OutlookSync.Domain.Tests.Aggregates;

public class CredentialTests
{
    [Fact]
    public void UpdateStatusData_WithValidData_ShouldSetStatusData()
    {
        // Arrange
        var credential = new Credential();
        var statusData = Encoding.UTF8.GetBytes("test_status_data");

        // Act
        credential.UpdateStatusData(statusData);

        // Assert
        Assert.Equal(TokenStatus.Valid, credential.TokenStatus);
        Assert.NotNull(credential.StatusData);
        Assert.Equal(statusData, credential.StatusData);
    }

    [Fact]
    public void UpdateStatusData_WithNullData_ShouldThrowException()
    {
        // Arrange
        var credential = new Credential();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => credential.UpdateStatusData(null!));
    }

    [Fact]
    public void MarkTokenAsInvalid_ShouldUpdateStatus()
    {
        // Arrange
        var credential = new Credential();
        credential.UpdateStatusData(Encoding.UTF8.GetBytes("test_data"));

        // Act
        credential.MarkTokenAsInvalid();

        // Assert
        Assert.Equal(TokenStatus.Invalid, credential.TokenStatus);
    }

    [Fact]
    public void MarkTokenAsExpired_ShouldUpdateStatus()
    {
        // Arrange
        var credential = new Credential();
        credential.UpdateStatusData(Encoding.UTF8.GetBytes("test_data"));

        // Act
        credential.MarkTokenAsExpired();

        // Assert
        Assert.Equal(TokenStatus.Expired, credential.TokenStatus);
    }

    [Fact]
    public void IsTokenValid_WithValidToken_ShouldReturnTrue()
    {
        // Arrange
        var credential = new Credential();
        credential.UpdateStatusData(Encoding.UTF8.GetBytes("test_data"));

        // Act
        var isValid = credential.IsTokenValid();

        // Assert
        Assert.True(isValid);
    }

    [Fact]
    public void IsTokenValid_WithInvalidToken_ShouldReturnFalse()
    {
        // Arrange
        var credential = new Credential();
        credential.UpdateStatusData(Encoding.UTF8.GetBytes("test_data"));
        credential.MarkTokenAsInvalid();

        // Act
        var isValid = credential.IsTokenValid();

        // Assert
        Assert.False(isValid);
    }

    [Fact]
    public void IsTokenValid_WithExpiredToken_ShouldReturnFalse()
    {
        // Arrange
        var credential = new Credential();
        credential.UpdateStatusData(Encoding.UTF8.GetBytes("test_data"));
        credential.MarkTokenAsExpired();

        // Act
        var isValid = credential.IsTokenValid();

        // Assert
        Assert.False(isValid);
    }
}
