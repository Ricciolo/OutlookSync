using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OutlookSync.AI.Configuration;
using OutlookSync.AI.Interfaces;
using OutlookSync.AI.Models;

namespace OutlookSync.AI.Services;

/// <summary>
/// Default implementation of <see cref="IAgentExecutor"/> that runs agent tasks.
/// </summary>
public sealed class AgentExecutor : IAgentExecutor
{
    private readonly ILogger<AgentExecutor> _logger;
    private readonly IAiService _aiService;
    private readonly AgentOptions _options;

    public AgentExecutor(
        ILogger<AgentExecutor> logger,
        IAiService aiService,
        IOptions<AgentOptions> options)
    {
        _logger = logger;
        _aiService = aiService;
        _options = options.Value;
    }

    /// <inheritdoc />
    public async Task<AgentResult> ExecuteAsync(AgentTask task, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(task);

        _logger.LogInformation("Executing agent task {TaskName} ({TaskId}).", task.Name, task.Id);

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(TimeSpan.FromSeconds(_options.TaskTimeoutSeconds));

        try
        {
            var prompt = $"Task: {task.Name}. {task.Description ?? string.Empty} Payload: {task.Payload ?? string.Empty}";
            var aiRequest = new AiRequest { Prompt = prompt };
            var aiResponse = await _aiService.GetCompletionAsync(aiRequest, cts.Token);

            return new AgentResult
            {
                TaskId = task.Id,
                Success = aiResponse.Success,
                Output = aiResponse.Content
            };
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Agent task {TaskId} timed out.", task.Id);
            return new AgentResult
            {
                TaskId = task.Id,
                Success = false,
                Error = "Task timed out."
            };
        }
    }
}
