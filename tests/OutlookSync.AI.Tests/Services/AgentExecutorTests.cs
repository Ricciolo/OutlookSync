using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using OutlookSync.AI.Configuration;
using OutlookSync.AI.Interfaces;
using OutlookSync.AI.Models;
using OutlookSync.AI.Services;

namespace OutlookSync.AI.Tests.Services;

public class AgentExecutorTests
{
    private readonly Mock<ILogger<AgentExecutor>> _loggerMock = new();
    private readonly Mock<IAiService> _aiServiceMock = new();
    private readonly IOptions<AgentOptions> _options = Options.Create(new AgentOptions { TaskTimeoutSeconds = 5 });

    private AgentExecutor CreateSut() => new(_loggerMock.Object, _aiServiceMock.Object, _options);

    [Fact]
    public async Task ExecuteAsync_SuccessfulAiCall_ReturnsSuccessResult()
    {
        _aiServiceMock.Setup(a => a.GetCompletionAsync(It.IsAny<AiRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AiResponse { Content = "done", Success = true });

        var sut = CreateSut();
        var task = new AgentTask { Name = "TestTask" };
        var result = await sut.ExecuteAsync(task);

        Assert.True(result.Success);
        Assert.Equal(task.Id, result.TaskId);
        Assert.Equal("done", result.Output);
    }

    [Fact]
    public async Task ExecuteAsync_AiCallFails_ReturnsFailureResult()
    {
        _aiServiceMock.Setup(a => a.GetCompletionAsync(It.IsAny<AiRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AiResponse { Content = string.Empty, Success = false });

        var sut = CreateSut();
        var task = new AgentTask { Name = "FailingTask" };
        var result = await sut.ExecuteAsync(task);

        Assert.False(result.Success);
    }

    [Fact]
    public async Task ExecuteAsync_NullTask_ThrowsArgumentNullException()
    {
        var sut = CreateSut();
        await Assert.ThrowsAsync<ArgumentNullException>(() => sut.ExecuteAsync(null!));
    }

    [Fact]
    public async Task ExecuteAsync_Timeout_ReturnsTimeoutError()
    {
        var timeoutOptions = Options.Create(new AgentOptions { TaskTimeoutSeconds = 1 });
        _aiServiceMock.Setup(a => a.GetCompletionAsync(It.IsAny<AiRequest>(), It.IsAny<CancellationToken>()))
            .Returns(async (AiRequest _, CancellationToken ct) =>
            {
                await Task.Delay(TimeSpan.FromSeconds(5), ct);
                return new AiResponse { Content = "late", Success = true };
            });

        var sut = new AgentExecutor(_loggerMock.Object, _aiServiceMock.Object, timeoutOptions);
        var task = new AgentTask { Name = "SlowTask" };
        var result = await sut.ExecuteAsync(task);

        Assert.False(result.Success);
        Assert.Equal("Task timed out.", result.Error);
    }
}
