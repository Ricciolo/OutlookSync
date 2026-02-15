namespace OutlookSync.Domain.Common;

/// <summary>
/// Base class for all entities with identity
/// </summary>
public abstract class Entity
{
    /// <summary>
    /// Gets the unique identifier for this entity.
    /// </summary>
    public Guid Id { get; protected init; } = Guid.CreateVersion7();
    
    /// <summary>
    /// Gets the date and time when this entity was created.
    /// </summary>
    public DateTime CreatedAt { get; protected init; } = DateTime.UtcNow;
    
    /// <summary>
    /// Gets the date and time when this entity was last updated.
    /// </summary>
    public DateTime? UpdatedAt { get; protected set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="Entity"/> class.
    /// </summary>
    protected Entity()
    {
        
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Entity"/> class with a specified identifier.
    /// </summary>
    /// <param name="id">The unique identifier for the entity.</param>
    protected Entity(Guid id)
    {
        Id = id;
    }

    /// <summary>
    /// Marks this entity as updated by setting the UpdatedAt timestamp to the current UTC time.
    /// </summary>
    protected void MarkAsUpdated()
    {
        UpdatedAt = DateTime.UtcNow;
    }
    
    /// <summary>
    /// Determines whether the specified object is equal to the current entity.
    /// </summary>
    /// <param name="obj">The object to compare with the current entity.</param>
    /// <returns>True if the specified object is equal to the current entity; otherwise, false.</returns>
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
    
    /// <summary>
    /// Determines whether two entity instances are not equal.
    /// </summary>
    /// <param name="left">The first entity to compare.</param>
    /// <param name="right">The second entity to compare.</param>
    /// <returns>True if the entities are not equal; otherwise, false.</returns>
    public static bool operator !=(Entity? left, Entity? right) => !(left == right);
}
