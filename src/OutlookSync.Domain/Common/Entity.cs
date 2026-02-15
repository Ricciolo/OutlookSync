namespace OutlookSync.Domain.Common;

/// <summary>
/// Base class for all entities with identity
/// </summary>
public abstract class Entity
{
    public Guid Id { get; protected init; } = Guid.CreateVersion7();
    
    public DateTime CreatedAt { get; protected init; } = DateTime.UtcNow;
    
    public DateTime? UpdatedAt { get; protected set; }

    protected Entity()
    {
        
    }

    protected Entity(Guid id)
    {
        Id = id;
    }

    protected void MarkAsUpdated()
    {
        UpdatedAt = DateTime.UtcNow;
    }
    
    public override bool Equals(object? obj)
    {
        if (obj is not Entity other)
            return false;
            
        if (ReferenceEquals(this, other))
            return true;
            
        if (GetType() != other.GetType())
            return false;
            
        return Id == other.Id;
    }
    
    /// <summary>
    /// Serves as the default hash function.
    /// </summary>
    /// <returns>A hash code for the current entity.</returns>
    public override int GetHashCode() => Id.GetHashCode();
    
    /// <summary>
    /// Determines whether two entity instances are equal.
    /// </summary>
    /// <param name="left">The first entity to compare.</param>
    /// <param name="right">The second entity to compare.</param>
    /// <returns>True if the entities are equal; otherwise, false.</returns>
    public static bool operator ==(Entity? left, Entity? right)
    {
        if (left is null && right is null)
            return true;
            
        if (left is null || right is null)
            return false;
            
        return left.Equals(right);
    }
    
    public static bool operator !=(Entity? left, Entity? right) => !(left == right);
}
