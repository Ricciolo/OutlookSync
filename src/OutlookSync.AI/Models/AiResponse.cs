namespace OutlookSync.AI.Models;

/// <summary>
/// Represents a response from the AI service.
/// </summary>
public sealed class AiResponse
{
    /// <summary>
    /// Gets or sets the generated content from the AI service.
    /// </summary>
    public required string Content { get; init; }

    /// <summary>
    /// Gets or sets a value indicating whether the request was successful.
    /// </summary>
    public bool Success { get; init; }
}
