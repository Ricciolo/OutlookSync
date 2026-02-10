using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OutlookSync.AI.Configuration;
using OutlookSync.AI.Interfaces;
using OutlookSync.AI.Models;

namespace OutlookSync.AI.Services;

/// <summary>
/// Default implementation of <see cref="IAiService"/> that delegates to an AI backend.
/// </summary>
public sealed class AiService : IAiService
{
    private readonly ILogger<AiService> _logger;
    private readonly AiOptions _options;
    private readonly IPrivacyService _privacyService;

    public AiService(
        ILogger<AiService> logger,
        IOptions<AiOptions> options,
        IPrivacyService privacyService)
    {
        _logger = logger;
        _options = options.Value;
        _privacyService = privacyService;
    }

    /// <inheritdoc />
    public async Task<AiResponse> GetCompletionAsync(AiRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var allowed = await _privacyService.IsDataSharingAllowedAsync(cancellationToken);
        if (!allowed)
        {
            _logger.LogWarning("AI completion blocked by privacy settings.");
            return new AiResponse { Content = string.Empty, Success = false };
        }

        _logger.LogInformation("Sending AI completion request to model {ModelId}.", _options.ModelId);

        // Placeholder for actual AI backend call.
        return new AiResponse { Content = string.Empty, Success = true };
    }
}
