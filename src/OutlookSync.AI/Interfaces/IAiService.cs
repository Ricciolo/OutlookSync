using OutlookSync.AI.Models;

namespace OutlookSync.AI.Interfaces;

/// <summary>
/// Defines the contract for AI completion services.
/// </summary>
public interface IAiService
{
    /// <summary>
    /// Sends a request to the AI service and returns a response.
    /// </summary>
    /// <param name="request">The AI request.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The AI response.</returns>
    Task<AiResponse> GetCompletionAsync(AiRequest request, CancellationToken cancellationToken = default);
}
