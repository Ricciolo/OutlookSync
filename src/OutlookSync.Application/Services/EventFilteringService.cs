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
        // Check color exclusion
        if (config.ColorExclusion.ExcludedColors.Contains(calendarEvent.Color))
        {
            return false;
        }
        
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
            CalendarId = Guid.Empty, // No longer using calendar ID
            ExternalId = newExternalId,
            Subject = subject,
            Start = sourceEvent.Start,
            End = sourceEvent.End,
            Location = config.CopyLocation ? sourceEvent.Location : null,
            Body = TransformBody(sourceEvent, config),
            BodyType = sourceEvent.BodyType,
            Organizer = config.CopyParticipants ? sourceEvent.Organizer : null,
            IsAllDay = sourceEvent.IsAllDay,
            IsRecurring = sourceEvent.IsRecurring,
            Color = config.TargetEventColor ?? sourceEvent.Color,
            Status = config.TargetStatus ?? sourceEvent.Status,
            RsvpStatus = sourceEvent.RsvpStatus,
            Attendees = config.CopyParticipants ? sourceEvent.Attendees : null,
            ConferenceLink = config.CopyConferenceLink ? sourceEvent.ConferenceLink : null,
            Categories = config.TargetCategory ?? sourceEvent.Categories,
            IsPrivate = config.MarkAsPrivate || sourceEvent.IsPrivate,
            HasAttachments = config.CopyAttachments && sourceEvent.HasAttachments,
            HasReminders = TransformReminders(sourceEvent, config),
            OriginalEventId = sourceEvent.ExternalId,
            SourceCalendarId = sourceEvent.CalendarId
        };
    }
    
    private static string TransformTitle(string originalTitle, CalendarBindingConfiguration config, string sourceCalendarName)
    {
        return config.TitleHandling switch
        {
            TitleHandling.Clone => originalTitle,
            TitleHandling.Rename => config.CustomTitle ?? originalTitle,
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
    
    private static bool TransformReminders(CalendarEvent sourceEvent, CalendarBindingConfiguration config)
    {
        return config.ReminderHandling switch
        {
            ReminderHandling.Copy => sourceEvent.HasReminders,
            ReminderHandling.Disable => false,
            ReminderHandling.Move => sourceEvent.HasReminders,
            _ => sourceEvent.HasReminders
        };
    }
}
