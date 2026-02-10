using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using OutlookSync.AI.Configuration;
using OutlookSync.AI.Interfaces;
using OutlookSync.AI.Models;
using OutlookSync.AI.Services;

namespace OutlookSync.AI.Tests.Services;

public class AiServiceTests
{
    private readonly Mock<ILogger<AiService>> _loggerMock = new();
    private readonly Mock<IPrivacyService> _privacyMock = new();
    private readonly IOptions<AiOptions> _options = Options.Create(new AiOptions { ModelId = "test-model" });

    private AiService CreateSut() => new(_loggerMock.Object, _options, _privacyMock.Object);

    [Fact]
    public async Task GetCompletionAsync_WhenDataSharingAllowed_ReturnsSuccess()
    {
        _privacyMock.Setup(p => p.IsDataSharingAllowedAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var sut = CreateSut();
        var result = await sut.GetCompletionAsync(new AiRequest { Prompt = "Hello" });

        Assert.True(result.Success);
    }

    [Fact]
    public async Task GetCompletionAsync_WhenDataSharingBlocked_ReturnsFailure()
    {
        _privacyMock.Setup(p => p.IsDataSharingAllowedAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var sut = CreateSut();
        var result = await sut.GetCompletionAsync(new AiRequest { Prompt = "Hello" });

        Assert.False(result.Success);
        Assert.Equal(string.Empty, result.Content);
    }

    [Fact]
    public async Task GetCompletionAsync_NullRequest_ThrowsArgumentNullException()
    {
        var sut = CreateSut();
        await Assert.ThrowsAsync<ArgumentNullException>(() => sut.GetCompletionAsync(null!));
    }
}
