namespace OutlookSync.AI.Models;

/// <summary>
/// Represents a request to the AI service.
/// </summary>
public sealed class AiRequest
{
    /// <summary>
    /// Gets or sets the prompt to send to the AI service.
    /// </summary>
    public required string Prompt { get; init; }

    /// <summary>
    /// Gets or sets the maximum number of tokens to generate.
    /// </summary>
    public int MaxTokens { get; init; } = 256;
}
