namespace OutlookSync.AI.Configuration;

/// <summary>
/// Configuration options for the Agent framework.
/// </summary>
public sealed class AgentOptions
{
    /// <summary>
    /// The configuration section name.
    /// </summary>
    public const string SectionName = "OutlookSync:Agent";

    /// <summary>
    /// Gets or sets the maximum number of concurrent agent tasks.
    /// </summary>
    public int MaxConcurrentTasks { get; set; } = 5;

    /// <summary>
    /// Gets or sets the task timeout in seconds.
    /// </summary>
    public int TaskTimeoutSeconds { get; set; } = 30;
}
