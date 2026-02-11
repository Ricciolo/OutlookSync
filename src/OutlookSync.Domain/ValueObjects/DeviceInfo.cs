namespace OutlookSync.Domain.ValueObjects;

/// <summary>
/// Value object for device information
/// </summary>
public record DeviceInfo
{
    public required string Name { get; init; }
    
    public required string Type { get; init; }
    
    public string? Description { get; init; }
    
    public static DeviceInfo Create(string name, string type, string? description = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name, nameof(name));
        ArgumentException.ThrowIfNullOrWhiteSpace(type, nameof(type));
        
        return new DeviceInfo
        {
            Name = name,
            Type = type,
            Description = description
        };
    }
}
