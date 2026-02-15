using OutlookSync.Domain.Aggregates;
using OutlookSync.Domain.ValueObjects;
using System.Text;

namespace OutlookSync.Domain.Tests.Aggregates;

public class CredentialTests
{
    [Fact]
    public void Constructor_WithValidFriendlyName_ShouldCreateCredential()
    {
        // Arrange & Act
        var credential = new Credential { FriendlyName = "My Outlook Account" };

        // Assert
        Assert.Equal("My Outlook Account", credential.FriendlyName);
    }

    [Fact]
    public void Constructor_WithNullFriendlyName_ShouldThrowException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new Credential { FriendlyName = null! });
    }

    [Fact]
    public void Constructor_WithEmptyFriendlyName_ShouldThrowException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => new Credential { FriendlyName = string.Empty });
    }

    [Fact]
    public void Constructor_WithWhiteSpaceFriendlyName_ShouldThrowException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => new Credential { FriendlyName = "   " });
    }

    [Fact]
    public void UpdateFriendlyName_WithValidName_ShouldUpdateName()
    {
        // Arrange
        var credential = new Credential { FriendlyName = "Original Name" };

        // Act
        credential.UpdateFriendlyName("Updated Name");

        // Assert
        Assert.Equal("Updated Name", credential.FriendlyName);
    }

    [Fact]
    public void UpdateFriendlyName_WithNullName_ShouldThrowException()
    {
        // Arrange
        var credential = new Credential { FriendlyName = "Original Name" };

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => credential.UpdateFriendlyName(null!));
    }

    [Fact]
    public void UpdateFriendlyName_WithEmptyName_ShouldThrowException()
    {
        // Arrange
        var credential = new Credential { FriendlyName = "Original Name" };

        // Act & Assert
        Assert.Throws<ArgumentException>(() => credential.UpdateFriendlyName(string.Empty));
    }

    [Fact]
    public void UpdateFriendlyName_WithWhiteSpaceName_ShouldThrowException()
    {
        // Arrange
        var credential = new Credential { FriendlyName = "Original Name" };

        // Act & Assert
        Assert.Throws<ArgumentException>(() => credential.UpdateFriendlyName("   "));
    }

    [Fact]
    public void UpdateStatusData_WithValidData_ShouldSetStatusData()
    {
        // Arrange
        var credential = new Credential { FriendlyName = "Test Account" };
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
        var credential = new Credential { FriendlyName = "Test Account" };

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => credential.UpdateStatusData(null!));
    }

    [Fact]
    public void MarkTokenAsInvalid_ShouldUpdateStatus()
    {
        // Arrange
        var credential = new Credential { FriendlyName = "Test Account" };
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
        var credential = new Credential { FriendlyName = "Test Account" };
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
        var credential = new Credential { FriendlyName = "Test Account" };
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
        var credential = new Credential { FriendlyName = "Test Account" };
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
        var credential = new Credential { FriendlyName = "Test Account" };
        credential.UpdateStatusData(Encoding.UTF8.GetBytes("test_data"));
        credential.MarkTokenAsExpired();

        // Act
        var isValid = credential.IsTokenValid();

        // Assert
        Assert.False(isValid);
    }
}
