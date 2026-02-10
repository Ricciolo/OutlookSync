namespace OutlookSync.AI.Models;

/// <summary>
/// Represents the result of an agent task execution.
/// </summary>
public sealed class AgentResult
{
    /// <summary>
    /// Gets or sets the task identifier this result belongs to.
    /// </summary>
    public required string TaskId { get; init; }

    /// <summary>
    /// Gets or sets a value indicating whether the task completed successfully.
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// Gets or sets the output produced by the task.
    /// </summary>
    public string? Output { get; init; }

    /// <summary>
    /// Gets or sets the error message if the task failed.
    /// </summary>
    public string? Error { get; init; }
}
