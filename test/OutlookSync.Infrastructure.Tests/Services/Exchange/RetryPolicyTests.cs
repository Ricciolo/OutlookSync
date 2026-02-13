using OutlookSync.Domain.ValueObjects;

namespace OutlookSync.Infrastructure.Tests.Services.Exchange;

public class RetryPolicyTests
{
    [Fact]
    public void CreateDefault_ShouldReturnValidPolicy()
    {
        // Act
        var policy = RetryPolicy.CreateDefault();
        
        // Assert
        Assert.Equal(3, policy.MaxRetryAttempts);
        Assert.Equal(1000, policy.InitialDelayMs);
        Assert.Equal(2.0, policy.BackoffMultiplier);
        Assert.Equal(30000, policy.MaxDelayMs);
        Assert.True(policy.UseJitter);
    }
    
    [Fact]
    public void CalculateDelay_ShouldUseExponentialBackoff()
    {
        // Arrange
        var policy = new RetryPolicy
        {
            MaxRetryAttempts = 3,
            InitialDelayMs = 1000,
            BackoffMultiplier = 2.0,
            MaxDelayMs = 10000,
            UseJitter = false
        };
        
        // Act
        var delay0 = policy.CalculateDelay(0);
        var delay1 = policy.CalculateDelay(1);
        var delay2 = policy.CalculateDelay(2);
        
        // Assert
        Assert.Equal(1000, delay0);
        Assert.Equal(2000, delay1);
        Assert.Equal(4000, delay2);
    }
    
    [Fact]
    public void CalculateDelay_ShouldRespectMaxDelay()
    {
        // Arrange
        var policy = new RetryPolicy
        {
            MaxRetryAttempts = 10,
            InitialDelayMs = 1000,
            BackoffMultiplier = 2.0,
            MaxDelayMs = 5000,
            UseJitter = false
        };
        
        // Act
        var delay5 = policy.CalculateDelay(5); // Would be 32000 without max
        
        // Assert
        Assert.Equal(5000, delay5);
    }
    
    [Fact]
    public void CalculateDelay_WithJitter_ShouldAddRandomness()
    {
        // Arrange
        var policy = new RetryPolicy
        {
            MaxRetryAttempts = 3,
            InitialDelayMs = 1000,
            BackoffMultiplier = 2.0,
            MaxDelayMs = 10000,
            UseJitter = true
        };
        
        // Act
        var delays = Enumerable.Range(0, 10)
            .Select(_ => policy.CalculateDelay(0))
            .ToList();
        
        // Assert - At least some values should be different due to jitter
        Assert.True(delays.Distinct().Count() > 1);
        Assert.All(delays, d => Assert.InRange(d, 1000, 1100)); // Base + 10% jitter
    }
    
    [Theory]
    [InlineData(-1)]
    [InlineData(3)]
    [InlineData(5)]
    public void CalculateDelay_OutOfRange_ShouldReturnZero(int attemptNumber)
    {
        // Arrange
        var policy = new RetryPolicy
        {
            MaxRetryAttempts = 3,
            InitialDelayMs = 1000,
            BackoffMultiplier = 2.0,
            MaxDelayMs = 10000,
            UseJitter = false
        };
        
        // Act
        var delay = policy.CalculateDelay(attemptNumber);
        
        // Assert
        Assert.Equal(0, delay);
    }
}
