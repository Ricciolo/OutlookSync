namespace OutlookSync.Domain.ValueObjects;

/// <summary>
/// Retry policy for resilient operations
/// </summary>
public record RetryPolicy
{
    /// <summary>
    /// Maximum number of retry attempts
    /// </summary>
    public int MaxRetryAttempts { get; init; } = 3;
    
    /// <summary>
    /// Initial delay between retries in milliseconds
    /// </summary>
    public int InitialDelayMs { get; init; } = 1000;
    
    /// <summary>
    /// Multiplier for exponential backoff
    /// </summary>
    public double BackoffMultiplier { get; init; } = 2.0;
    
    /// <summary>
    /// Maximum delay between retries in milliseconds
    /// </summary>
    public int MaxDelayMs { get; init; } = 30000;
    
    /// <summary>
    /// Whether to add jitter to retry delays
    /// </summary>
    public bool UseJitter { get; init; } = true;
    
    /// <summary>
    /// Creates a default retry policy with exponential backoff
    /// </summary>
    public static RetryPolicy CreateDefault() => new()
    {
        MaxRetryAttempts = 3,
        InitialDelayMs = 1000,
        BackoffMultiplier = 2.0,
        MaxDelayMs = 30000,
        UseJitter = true
    };
    
    /// <summary>
    /// Calculates the delay for a given retry attempt
    /// </summary>
    public int CalculateDelay(int attemptNumber)
    {
        if (attemptNumber < 0 || attemptNumber >= MaxRetryAttempts)
        {
            return 0;
        }
        
        // Calculate exponential backoff
        var delay = InitialDelayMs * Math.Pow(BackoffMultiplier, attemptNumber);
        delay = Math.Min(delay, MaxDelayMs);
        
        // Add jitter if enabled
        if (UseJitter)
        {
            var jitter = Random.Shared.Next(0, (int)(delay * 0.1));
            delay += jitter;
        }
        
        return (int)delay;
    }
}
