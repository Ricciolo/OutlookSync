using Microsoft.Extensions.Logging.Abstractions;
using OutlookSync.Infrastructure.Services;

namespace OutlookSync.Infrastructure.Tests.Services;

public class CredentialsServiceTests
{
    private readonly CredentialsService _service;

    public CredentialsServiceTests()
    {
        var logger = NullLogger<CredentialsService>.Instance;
        _service = new CredentialsService(logger);
    }

    [Fact]
    public async Task InitializeCredentialAsync_WithNullFriendlyName_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            async () => await _service.InitializeCredentialAsync(null!));
    }

    [Fact]
    public async Task InitializeCredentialAsync_WithEmptyFriendlyName_ThrowsArgumentException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            async () => await _service.InitializeCredentialAsync(string.Empty));
    }

    [Fact]
    public async Task InitializeCredentialAsync_WithWhitespaceFriendlyName_ThrowsArgumentException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            async () => await _service.InitializeCredentialAsync("   "));
    }

    [Fact]
    public async Task CompleteCredentialAsync_WithInvalidSessionId_ReturnsFailure()
    {
        // Arrange
        var invalidSessionId = Guid.NewGuid();

        // Act
        var result = await _service.CompleteCredentialAsync(invalidSessionId);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.False(result.IsPending);
        Assert.Contains("not found", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    // Note: Full integration test for device code flow would require actual authentication
    // which is not suitable for automated unit tests. These tests verify basic validation.
    // For full testing, create a manual integration test or use mock authentication.
}
