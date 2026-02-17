namespace OutlookSync.Domain.ValueObjects;

/// <summary>
/// Value object for sync configuration
/// </summary>
public record SyncConfiguration
{
    public required SyncInterval Interval { get; init; }
    
    public required DateTime StartDate { get; init; }
    
    public required bool IsPrivate { get; init; }
    
    public required CalendarFieldSelection FieldSelection { get; init; }
    
    /// <summary>
    /// Gets the number of days forward to synchronize
    /// </summary>
    public int SyncDaysForward { get; init; } = 30;
}

/// <summary>
/// Sync interval configuration
/// </summary>
public record SyncInterval
{
    public required int Minutes { get; init; }
    
    public string? CronExpression { get; init; }
    
    public static SyncInterval Every15Minutes() => new() { Minutes = 15, CronExpression = "*/15 * * * *" };
    
    public static SyncInterval Every30Minutes() => new() { Minutes = 30, CronExpression = "*/30 * * * *" };
    
    public static SyncInterval Hourly() => new() { Minutes = 60, CronExpression = "0 * * * *" };
    
    public static SyncInterval Custom(int minutes, string? cronExpression = null) => 
        new() { Minutes = minutes, CronExpression = cronExpression };
}

/// <summary>
/// Calendar fields to sync
/// </summary>
public record CalendarFieldSelection
{
    public bool Subject { get; init; } = true;
    
    public bool StartTime { get; init; } = true;
    
    public bool EndTime { get; init; } = true;
    
    public bool Location { get; init; } = true;
    
    public bool Attendees { get; init; } = true;
    
    public bool Body { get; init; } = true;
    
    public bool Organizer { get; init; } = true;
    
    public bool IsAllDay { get; init; } = true;
    
    public bool Recurrence { get; init; } = true;
    
    public static CalendarFieldSelection All() => new();
    
    public static CalendarFieldSelection Essential() => new()
    {
        Subject = true,
        StartTime = true,
        EndTime = true,
        Location = true,
        Attendees = false,
        Body = false,
        Organizer = true,
        IsAllDay = true,
        Recurrence = true
    };
}
