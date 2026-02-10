namespace OutlookSync.AI.Configuration;

/// <summary>
/// Configuration options for the AI service.
/// </summary>
public sealed class AiOptions
{
    /// <summary>
    /// The configuration section name.
    /// </summary>
    public const string SectionName = "OutlookSync:AI";

    /// <summary>
    /// Gets or sets the AI service endpoint URL.
    /// </summary>
    public string? Endpoint { get; set; }

    /// <summary>
    /// Gets or sets the model identifier to use.
    /// </summary>
    public string ModelId { get; set; } = "default";
}
