using OutlookSync.Domain.Aggregates;
using OutlookSync.Domain.ValueObjects;

namespace OutlookSync.Application.Services;

/// <summary>
/// Service for filtering and transforming calendar events based on CalendarBinding configuration
/// </summary>
public class EventFilteringService
{
    /// <summary>
    /// Determines whether an event should be synchronized based on the binding's exclusion rules
    /// </summary>
    public static bool ShouldSyncEvent(CalendarEvent calendarEvent, CalendarBindingConfiguration config)
    {
        // Check RSVP exclusion
        if (config.RsvpExclusion.ExcludedResponses.Contains(calendarEvent.RsvpStatus))
        {
            return false;
        }
        
        // Check status exclusion
        if (config.StatusExclusion.ExcludedStatuses.Contains(calendarEvent.Status))
        {
            return false;
        }
        
        return true;
    }
    
    /// <summary>
    /// Transforms a source event into a target event based on the binding's configuration
    /// </summary>
    public static CalendarEvent TransformEvent(
        CalendarEvent sourceEvent,
        CalendarBinding binding,
        string sourceCalendarName,
        string newExternalId)
    {
        var config = binding.Configuration;
        
        // Transform title based on handling setting
        var subject = TransformTitle(sourceEvent.Subject, config, sourceCalendarName);
        
        // Apply custom tag if specified
        if (!string.IsNullOrWhiteSpace(config.CustomTag))
        {
            if (config.CustomTagInTitle)
            {
                subject = $"{config.CustomTag} {subject}";
            }
        }
        
        return new CalendarEvent
        {
            Id = Guid.NewGuid(),
            ExternalId = newExternalId,
            Subject = subject,
            Start = sourceEvent.Start,
            End = sourceEvent.End,
            Location = config.CopyLocation ? sourceEvent.Location : null,
            IsOnlineMeeting = config.CopyLocation && sourceEvent.IsOnlineMeeting,
            IsMeeting = config.CopyLocation && sourceEvent.IsMeeting,
            Body = TransformBody(sourceEvent, config),
            BodyType = sourceEvent.BodyType,
            Organizer = config.CopyParticipants ? sourceEvent.Organizer : null,
            IsAllDay = sourceEvent.IsAllDay,
            IsRecurring = sourceEvent.IsRecurring,
            Status = config.TargetStatus ?? sourceEvent.Status,
            RsvpStatus = sourceEvent.RsvpStatus,
            RequiredAttendees = config.CopyParticipants ? sourceEvent.RequiredAttendees : [],
            OptionalAttendees = config.CopyParticipants ? sourceEvent.OptionalAttendees : [],
            Categories = config.TargetCategory ?? sourceEvent.Categories,
            IsPrivate = config.MarkAsPrivate || sourceEvent.IsPrivate,
            HasAttachments = config.CopyAttachments && sourceEvent.HasAttachments,
            ReminderMinutesBeforeStart = TransformReminderMinutes(sourceEvent, config),
            OriginalEventId = sourceEvent.ExternalId,
            SourceCalendarBindingId = sourceEvent.SourceCalendarBindingId
        };
    }
    
    private static string TransformTitle(string originalTitle, CalendarBindingConfiguration config, string sourceCalendarName)
    {
        return config.TitleHandling switch
        {
            TitleHandling.Clone => originalTitle,
            TitleHandling.Rename => string.IsNullOrWhiteSpace(config.CustomTitle) 
                ? originalTitle 
                : $"{config.CustomTitle} {originalTitle}",
            TitleHandling.Hide => config.CustomTitle ?? $"Event from {sourceCalendarName}",
            _ => originalTitle
        };
    }
    
    private static string? TransformBody(CalendarEvent sourceEvent, CalendarBindingConfiguration config)
    {
        if (!config.CopyDescription)
        {
            return null;
        }
        
        var body = sourceEvent.Body;
        
        // If custom tag should be in description, add it
        if (!string.IsNullOrWhiteSpace(config.CustomTag) && !config.CustomTagInTitle)
        {
            body = string.IsNullOrWhiteSpace(body)
                ? config.CustomTag
                : $"{config.CustomTag}\n\n{body}";
        }
        
        return body;
    }

    private static int? TransformReminderMinutes(CalendarEvent sourceEvent, CalendarBindingConfiguration config)
    {
        return config.ReminderHandling switch
        {
            ReminderHandling.Copy => sourceEvent.ReminderMinutesBeforeStart,
            ReminderHandling.Disable => null,
            _ => sourceEvent.ReminderMinutesBeforeStart
        };
    }
}
