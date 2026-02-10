namespace OutlookSync.AI.Models;

/// <summary>
/// Represents a task to be executed by an agent.
/// </summary>
public sealed class AgentTask
{
    /// <summary>
    /// Gets the unique identifier for this task.
    /// </summary>
    public string Id { get; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Gets or sets the name of the task.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets or sets the description of what this task does.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Gets or sets the input payload for the task.
    /// </summary>
    public string? Payload { get; init; }
}
