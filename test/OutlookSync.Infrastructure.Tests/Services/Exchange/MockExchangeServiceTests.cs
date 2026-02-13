using OutlookSync.Domain.ValueObjects;
using OutlookSync.Infrastructure.Services.Exchange;

namespace OutlookSync.Infrastructure.Tests.Services.Exchange;

public class MockExchangeServiceTests
{
    [Fact]
    public async Task InitializeAsync_ShouldSucceed()
    {
        // Arrange
        var service = new MockExchangeService();
        
        // Act
        await service.InitializeAsync("mock-token", "https://test.com/ews", CancellationToken.None);
        
        // Assert
        var result = await service.TestConnectionAsync();
        Assert.True(result);
    }
    
    [Fact]
    public async Task GetCalendarEventsAsync_ShouldReturnEmptyList_WhenNoEventsExist()
    {
        // Arrange
        var service = new MockExchangeService();
        await service.InitializeAsync("mock-token", "https://test.com/ews", CancellationToken.None);
        
        // Act
        var events = await service.GetCalendarEventsAsync(
            "calendar1",
            DateTime.Now,
            DateTime.Now.AddDays(7),
            CancellationToken.None);
        
        // Assert
        Assert.NotNull(events);
        Assert.Empty(events);
    }
    
    [Fact]
    public async Task CreateCalendarEventAsync_ShouldAddEvent()
    {
        // Arrange
        var service = new MockExchangeService();
        await service.InitializeAsync("mock-token", "https://test.com/ews", CancellationToken.None);
        
        var newEvent = new CalendarEvent
        {
            Id = Guid.NewGuid(),
            CalendarId = Guid.NewGuid(),
            ExternalId = "temp-id",
            Subject = "Test Meeting",
            Start = DateTime.Now.AddDays(1),
            End = DateTime.Now.AddDays(1).AddHours(1),
            Location = "Conference Room",
            Body = "Test meeting body",
            IsAllDay = false,
            IsRecurring = false,
            IsCopiedEvent = false
        };
        
        // Act
        var createdEvent = await service.CreateCalendarEventAsync("calendar1", newEvent, CancellationToken.None);
        
        // Assert
        Assert.NotNull(createdEvent);
        Assert.NotEqual("temp-id", createdEvent.ExternalId);
        Assert.Equal(newEvent.Subject, createdEvent.Subject);
    }
    
    [Fact]
    public async Task GetCalendarEventsAsync_ShouldReturnCreatedEvent()
    {
        // Arrange
        var service = new MockExchangeService();
        await service.InitializeAsync("mock-token", "https://test.com/ews", CancellationToken.None);
        
        var startDate = DateTime.Now.AddDays(1);
        var endDate = startDate.AddHours(1);
        
        var newEvent = new CalendarEvent
        {
            Id = Guid.NewGuid(),
            CalendarId = Guid.NewGuid(),
            ExternalId = "temp-id",
            Subject = "Test Meeting",
            Start = startDate,
            End = endDate,
            IsAllDay = false,
            IsRecurring = false,
            IsCopiedEvent = false
        };
        
        await service.CreateCalendarEventAsync("calendar1", newEvent, CancellationToken.None);
        
        // Act
        var events = await service.GetCalendarEventsAsync(
            "calendar1",
            DateTime.Now,
            DateTime.Now.AddDays(7),
            CancellationToken.None);
        
        // Assert
        Assert.Single(events);
        Assert.Equal(newEvent.Subject, events[0].Subject);
    }
    
    [Fact]
    public async Task UpdateCalendarEventAsync_ShouldModifyEvent()
    {
        // Arrange
        var service = new MockExchangeService();
        await service.InitializeAsync("mock-token", "https://test.com/ews", CancellationToken.None);
        
        var newEvent = new CalendarEvent
        {
            Id = Guid.NewGuid(),
            CalendarId = Guid.NewGuid(),
            ExternalId = "temp-id",
            Subject = "Original Subject",
            Start = DateTime.Now.AddDays(1),
            End = DateTime.Now.AddDays(1).AddHours(1),
            IsAllDay = false,
            IsRecurring = false,
            IsCopiedEvent = false
        };
        
        var createdEvent = await service.CreateCalendarEventAsync("calendar1", newEvent, CancellationToken.None);
        
        var updatedEvent = createdEvent with { Subject = "Updated Subject" };
        
        // Act
        await service.UpdateCalendarEventAsync("calendar1", updatedEvent, CancellationToken.None);
        
        // Assert
        var events = await service.GetCalendarEventsAsync(
            "calendar1",
            DateTime.Now,
            DateTime.Now.AddDays(7),
            CancellationToken.None);
        
        Assert.Single(events);
        Assert.Equal("Updated Subject", events[0].Subject);
    }
    
    [Fact]
    public async Task DeleteCalendarEventAsync_ShouldRemoveEvent()
    {
        // Arrange
        var service = new MockExchangeService();
        await service.InitializeAsync("mock-token", "https://test.com/ews", CancellationToken.None);
        
        var newEvent = new CalendarEvent
        {
            Id = Guid.NewGuid(),
            CalendarId = Guid.NewGuid(),
            ExternalId = "temp-id",
            Subject = "Test Meeting",
            Start = DateTime.Now.AddDays(1),
            End = DateTime.Now.AddDays(1).AddHours(1),
            IsAllDay = false,
            IsRecurring = false,
            IsCopiedEvent = false
        };
        
        var createdEvent = await service.CreateCalendarEventAsync("calendar1", newEvent, CancellationToken.None);
        
        // Act
        await service.DeleteCalendarEventAsync("calendar1", createdEvent.ExternalId, CancellationToken.None);
        
        // Assert
        var events = await service.GetCalendarEventsAsync(
            "calendar1",
            DateTime.Now,
            DateTime.Now.AddDays(7),
            CancellationToken.None);
        
        Assert.Empty(events);
    }
    
    [Fact]
    public async Task TestConnectionAsync_ShouldReturnFalse_WhenNotInitialized()
    {
        // Arrange
        var service = new MockExchangeService();
        
        // Act
        var result = await service.TestConnectionAsync();
        
        // Assert
        Assert.False(result);
    }
    
    [Fact]
    public async Task Reset_ShouldClearAllData()
    {
        // Arrange
        var service = new MockExchangeService();
        await service.InitializeAsync("mock-token", "https://test.com/ews", CancellationToken.None);
        
        var newEvent = new CalendarEvent
        {
            Id = Guid.NewGuid(),
            CalendarId = Guid.NewGuid(),
            ExternalId = "temp-id",
            Subject = "Test Meeting",
            Start = DateTime.Now.AddDays(1),
            End = DateTime.Now.AddDays(1).AddHours(1),
            IsAllDay = false,
            IsRecurring = false,
            IsCopiedEvent = false
        };
        
        await service.CreateCalendarEventAsync("calendar1", newEvent, CancellationToken.None);
        
        // Act
        service.Reset();
        
        // Assert
        Assert.Empty(service.CalendarEvents);
    }
}
