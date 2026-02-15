using Microsoft.Extensions.Logging;
using Moq;
using OutlookSync.Application.Services;
using OutlookSync.Domain.Aggregates;
using OutlookSync.Domain.Repositories;
using OutlookSync.Domain.Services;
using OutlookSync.Domain.ValueObjects;

namespace OutlookSync.Application.Tests.Services;

/// <summary>
/// Unit tests for CalendarsSyncService
/// </summary>
public class CalendarsSyncServiceTests
{
    private readonly Mock<ICalendarRepository> _mockCalendarRepository;
    private readonly Mock<ICredentialRepository> _mockCredentialRepository;
    private readonly Mock<ICalendarEventRepositoryFactory> _mockCalendarEventRepositoryFactory;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<ILogger<CalendarsSyncService>> _mockLogger;
    private readonly CalendarsSyncService _service;

    public CalendarsSyncServiceTests()
    {
        _mockCalendarRepository = new Mock<ICalendarRepository>();
        _mockCredentialRepository = new Mock<ICredentialRepository>();
        _mockCalendarEventRepositoryFactory = new Mock<ICalendarEventRepositoryFactory>();
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockLogger = new Mock<ILogger<CalendarsSyncService>>();

        _service = new CalendarsSyncService(
            _mockCalendarRepository.Object,
            _mockCredentialRepository.Object,
            _mockCalendarEventRepositoryFactory.Object,
            _mockUnitOfWork.Object,
            _mockLogger.Object
        );
    }

    [Fact]
    public async Task SyncAllCalendarsAsync_WithNoEnabledCalendars_ShouldReturnSuccessWithZeroItems()
    {
        // Arrange
        var mockQueryable = new List<Calendar>().AsQueryable();
        _mockCalendarRepository.Setup(r => r.Query).Returns(mockQueryable);

        // Act
        var result = await _service.SyncAllCalendarsAsync();

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(0, result.TotalCalendarsProcessed);
        Assert.Equal(0, result.TotalEventsCopied);
        Assert.Equal(0, result.SuccessfulSyncs);
        Assert.Equal(0, result.FailedSyncs);
    }

    [Fact]
    public async Task SyncAllCalendarsAsync_WithOneEnabledCalendar_ShouldReturnSuccessWithZeroEventsCopied()
    {
        // Arrange
        var calendar = CreateTestCalendar("Calendar1", isEnabled: true);
        var mockQueryable = new List<Calendar> { calendar }.AsQueryable();
        _mockCalendarRepository.Setup(r => r.Query).Returns(mockQueryable);

        var credential = CreateTestCredential();
        _mockCredentialRepository
            .Setup(r => r.GetByIdAsync(calendar.CredentialId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(credential);

        var mockEventRepository = new Mock<ICalendarEventRepository>();
        mockEventRepository
            .Setup(r => r.InitAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        mockEventRepository
            .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        _mockCalendarEventRepositoryFactory
            .Setup(f => f.Create(It.IsAny<Calendar>(), It.IsAny<Credential>()))
            .Returns(mockEventRepository.Object);

        // Act
        var result = await _service.SyncAllCalendarsAsync();

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.TotalCalendarsProcessed);
        Assert.Equal(0, result.TotalEventsCopied);
        Assert.Equal(1, result.SuccessfulSyncs);
        Assert.Equal(0, result.FailedSyncs);
    }

    [Fact]
    public async Task SyncAllCalendarsAsync_WithMultipleCalendarsAndEvents_ShouldCopyEventsBetweenCalendars()
    {
        // Arrange
        var calendar1 = CreateTestCalendar("Calendar1", isEnabled: true);
        var calendar2 = CreateTestCalendar("Calendar2", isEnabled: true);
        var mockQueryable = new List<Calendar> { calendar1, calendar2 }.AsQueryable();
        _mockCalendarRepository.Setup(r => r.Query).Returns(mockQueryable);

        var credential1 = CreateTestCredential();
        var credential2 = CreateTestCredential();
        
        _mockCredentialRepository
            .Setup(r => r.GetByIdAsync(calendar1.CredentialId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(credential1);
        _mockCredentialRepository
            .Setup(r => r.GetByIdAsync(calendar2.CredentialId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(credential2);

        // Setup event repository for calendar1
        var event1 = CreateTestEvent(calendar1.Id, "Meeting 1");
        var event2 = CreateTestEvent(calendar1.Id, "Meeting 2");
        var mockEventRepo1 = CreateMockEventRepository([event1, event2]);
        
        // Setup event repository for calendar2
        var event3 = CreateTestEvent(calendar2.Id, "Meeting 3");
        var mockEventRepo2 = CreateMockEventRepository([event3]);

        _mockCalendarEventRepositoryFactory
            .Setup(f => f.Create(calendar1, credential1))
            .Returns(mockEventRepo1.Object);
        _mockCalendarEventRepositoryFactory
            .Setup(f => f.Create(calendar2, credential2))
            .Returns(mockEventRepo2.Object);

        // Act
        var result = await _service.SyncAllCalendarsAsync();

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.TotalCalendarsProcessed);
        Assert.True(result.TotalEventsCopied > 0);
        Assert.Equal(2, result.SuccessfulSyncs);
        Assert.Equal(0, result.FailedSyncs);

        // Verify events were added to target repositories
        mockEventRepo1.Verify(r => r.AddAsync(
            It.Is<CalendarEvent>(e => e.Subject.Contains("[SYNCED]")),
            It.IsAny<CancellationToken>()), 
            Times.AtLeastOnce);
        mockEventRepo2.Verify(r => r.AddAsync(
            It.Is<CalendarEvent>(e => e.Subject.Contains("[SYNCED]")),
            It.IsAny<CancellationToken>()), 
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task SyncAllCalendarsAsync_WithMissingCredential_ShouldReturnPartialResult()
    {
        // Arrange
        var calendar1 = CreateTestCalendar("Calendar1", isEnabled: true);
        var calendar2 = CreateTestCalendar("Calendar2", isEnabled: true);
        var mockQueryable = new List<Calendar> { calendar1, calendar2 }.AsQueryable();
        _mockCalendarRepository.Setup(r => r.Query).Returns(mockQueryable);

        var credential2 = CreateTestCredential();
        
        _mockCredentialRepository
            .Setup(r => r.GetByIdAsync(calendar1.CredentialId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Credential?)null); // Missing credential
        _mockCredentialRepository
            .Setup(r => r.GetByIdAsync(calendar2.CredentialId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(credential2);

        var mockEventRepo2 = CreateMockEventRepository([]);
        _mockCalendarEventRepositoryFactory
            .Setup(f => f.Create(calendar2, credential2))
            .Returns(mockEventRepo2.Object);

        // Act
        var result = await _service.SyncAllCalendarsAsync();

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(2, result.TotalCalendarsProcessed);
        Assert.Equal(1, result.SuccessfulSyncs);
        Assert.Equal(1, result.FailedSyncs);
        Assert.NotEmpty(result.Errors);
    }

    [Fact]
    public async Task SyncAllCalendarsAsync_WithInvalidToken_ShouldReturnPartialResult()
    {
        // Arrange
        var calendar = CreateTestCalendar("Calendar1", isEnabled: true);
        var mockQueryable = new List<Calendar> { calendar }.AsQueryable();
        _mockCalendarRepository.Setup(r => r.Query).Returns(mockQueryable);

        var invalidCredential = CreateTestCredential();
        invalidCredential.UpdateStatusData([]); // Empty status data
        
        _mockCredentialRepository
            .Setup(r => r.GetByIdAsync(calendar.CredentialId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(invalidCredential);

        // Act
        var result = await _service.SyncAllCalendarsAsync();

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(1, result.TotalCalendarsProcessed);
        Assert.Equal(0, result.SuccessfulSyncs);
        Assert.Equal(1, result.FailedSyncs);
        Assert.NotEmpty(result.Errors);
        Assert.Contains("Invalid token or missing status data", result.Errors[0]);
    }

    [Fact]
    public async Task SyncAllCalendarsAsync_ShouldSkipCopiedEvents()
    {
        // Arrange
        var calendar1 = CreateTestCalendar("Calendar1", isEnabled: true);
        var calendar2 = CreateTestCalendar("Calendar2", isEnabled: true);
        var mockQueryable = new List<Calendar> { calendar1, calendar2 }.AsQueryable();
        _mockCalendarRepository.Setup(r => r.Query).Returns(mockQueryable);

        var credential1 = CreateTestCredential();
        var credential2 = CreateTestCredential();
        
        _mockCredentialRepository
            .Setup(r => r.GetByIdAsync(calendar1.CredentialId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(credential1);
        _mockCredentialRepository
            .Setup(r => r.GetByIdAsync(calendar2.CredentialId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(credential2);

        // Create an original event and a copied event
        var originalEvent = CreateTestEvent(calendar1.Id, "Original Meeting");
        var copiedEvent = new CalendarEvent
        {
            Id = Guid.CreateVersion7(),
            CalendarId = calendar1.Id,
            ExternalId = "copied_123",
            Subject = "[SYNCED] Copied Meeting",
            Start = DateTime.UtcNow,
            End = DateTime.UtcNow.AddHours(1),
            OriginalEventId = "original_event_id",
            SourceCalendarId = calendar2.Id
        };

        var mockEventRepo1 = CreateMockEventRepository([originalEvent, copiedEvent]);
        var mockEventRepo2 = CreateMockEventRepository([]);

        _mockCalendarEventRepositoryFactory
            .Setup(f => f.Create(calendar1, credential1))
            .Returns(mockEventRepo1.Object);
        _mockCalendarEventRepositoryFactory
            .Setup(f => f.Create(calendar2, credential2))
            .Returns(mockEventRepo2.Object);

        // Act
        var result = await _service.SyncAllCalendarsAsync();

        // Assert
        Assert.True(result.IsSuccess);
        // Should only copy the original event, not the already copied one
        mockEventRepo2.Verify(r => r.AddAsync(
            It.IsAny<CalendarEvent>(),
            It.IsAny<CancellationToken>()), 
            Times.Once); // Only one event should be copied
    }

    [Fact]
    public async Task SyncAllCalendarsAsync_ShouldNotCopyDuplicateEvents()
    {
        // Arrange
        var calendar1 = CreateTestCalendar("Calendar1", isEnabled: true);
        var calendar2 = CreateTestCalendar("Calendar2", isEnabled: true);
        var mockQueryable = new List<Calendar> { calendar1, calendar2 }.AsQueryable();
        _mockCalendarRepository.Setup(r => r.Query).Returns(mockQueryable);

        var credential1 = CreateTestCredential();
        var credential2 = CreateTestCredential();
        
        _mockCredentialRepository
            .Setup(r => r.GetByIdAsync(calendar1.CredentialId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(credential1);
        _mockCredentialRepository
            .Setup(r => r.GetByIdAsync(calendar2.CredentialId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(credential2);

        var event1 = CreateTestEvent(calendar1.Id, "Meeting");
        var mockEventRepo1 = CreateMockEventRepository([event1]);
        var mockEventRepo2 = CreateMockEventRepository([]);

        // Setup FindCopiedEventAsync to return existing copy
        var existingCopy = CreateTestEvent(calendar2.Id, "[SYNCED] Meeting");
        mockEventRepo2
            .Setup(r => r.FindCopiedEventAsync(
                It.IsAny<CalendarEvent>(),
                It.IsAny<Calendar>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingCopy);

        _mockCalendarEventRepositoryFactory
            .Setup(f => f.Create(calendar1, credential1))
            .Returns(mockEventRepo1.Object);
        _mockCalendarEventRepositoryFactory
            .Setup(f => f.Create(calendar2, credential2))
            .Returns(mockEventRepo2.Object);

        // Act
        var result = await _service.SyncAllCalendarsAsync();

        // Assert
        Assert.True(result.IsSuccess);
        // Should not add any events since they already exist
        mockEventRepo2.Verify(r => r.AddAsync(
            It.IsAny<CalendarEvent>(),
            It.IsAny<CancellationToken>()), 
            Times.Never);
    }

    [Fact]
    public async Task SyncAllCalendarsAsync_ShouldCallUnitOfWorkSaveChanges()
    {
        // Arrange
        var calendar = CreateTestCalendar("Calendar1", isEnabled: true);
        var mockQueryable = new List<Calendar> { calendar }.AsQueryable();
        _mockCalendarRepository.Setup(r => r.Query).Returns(mockQueryable);

        var credential = CreateTestCredential();
        _mockCredentialRepository
            .Setup(r => r.GetByIdAsync(calendar.CredentialId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(credential);

        var mockEventRepo = CreateMockEventRepository([]);
        _mockCalendarEventRepositoryFactory
            .Setup(f => f.Create(calendar, credential))
            .Returns(mockEventRepo.Object);

        // Act
        await _service.SyncAllCalendarsAsync();

        // Assert
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SyncAllCalendarsAsync_WhenExceptionDuringSave_ShouldThrowException()
    {
        // Arrange
        var calendar = CreateTestCalendar("Calendar1", isEnabled: true);
        var mockQueryable = new List<Calendar> { calendar }.AsQueryable();
        _mockCalendarRepository.Setup(r => r.Query).Returns(mockQueryable);

        var credential = CreateTestCredential();
        _mockCredentialRepository
            .Setup(r => r.GetByIdAsync(calendar.CredentialId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(credential);

        var mockEventRepo = CreateMockEventRepository([]);
        _mockCalendarEventRepositoryFactory
            .Setup(f => f.Create(calendar, credential))
            .Returns(mockEventRepo.Object);

        _mockUnitOfWork
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Database error"));

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.SyncAllCalendarsAsync());
    }

    [Fact]
    public async Task SyncAllCalendarsAsync_ShouldUpdateCalendarWithSuccessfulSync()
    {
        // Arrange
        var calendar1 = CreateTestCalendar("Calendar1", isEnabled: true);
        var calendar2 = CreateTestCalendar("Calendar2", isEnabled: true);
        var mockQueryable = new List<Calendar> { calendar1, calendar2 }.AsQueryable();
        _mockCalendarRepository.Setup(r => r.Query).Returns(mockQueryable);

        var credential1 = CreateTestCredential();
        var credential2 = CreateTestCredential();
        
        _mockCredentialRepository
            .Setup(r => r.GetByIdAsync(calendar1.CredentialId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(credential1);
        _mockCredentialRepository
            .Setup(r => r.GetByIdAsync(calendar2.CredentialId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(credential2);

        var mockEventRepo1 = CreateMockEventRepository([]);
        var mockEventRepo2 = CreateMockEventRepository([]);
        
        _mockCalendarEventRepositoryFactory
            .Setup(f => f.Create(calendar1, credential1))
            .Returns(mockEventRepo1.Object);
        _mockCalendarEventRepositoryFactory
            .Setup(f => f.Create(calendar2, credential2))
            .Returns(mockEventRepo2.Object);

        // Act
        await _service.SyncAllCalendarsAsync();

        // Assert
        _mockCalendarRepository.Verify(r => r.UpdateAsync(
            It.IsAny<Calendar>(),
            It.IsAny<CancellationToken>()), 
            Times.AtLeast(2)); // Both calendars should be updated
    }

    [Fact]
    public async Task SyncAllCalendarsAsync_WhenSyncFails_ShouldUpdateCredentialInFinally()
    {
        // Arrange
        var calendar = CreateTestCalendar("Calendar1", isEnabled: true);
        var mockQueryable = new List<Calendar> { calendar }.AsQueryable();
        _mockCalendarRepository.Setup(r => r.Query).Returns(mockQueryable);

        var credential = CreateTestCredential();
        _mockCredentialRepository
            .Setup(r => r.GetByIdAsync(calendar.CredentialId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(credential);

        var mockEventRepo = new Mock<ICalendarEventRepository>();
        mockEventRepo
            .Setup(r => r.InitAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Connection failed"));

        _mockCalendarEventRepositoryFactory
            .Setup(f => f.Create(calendar, credential))
            .Returns(mockEventRepo.Object);

        // Act
        await _service.SyncAllCalendarsAsync();

        // Assert - Credential should still be updated even on failure
        _mockCredentialRepository.Verify(r => r.UpdateAsync(
            credential,
            It.IsAny<CancellationToken>()), 
            Times.Once);
    }

    // Helper methods

    private static Calendar CreateTestCalendar(string name, bool isEnabled)
    {
        var calendar = new Calendar
        {
            Name = name,
            ExternalId = $"ext_{Guid.NewGuid()}",
            CredentialId = Guid.CreateVersion7(),
            Configuration = new SyncConfiguration
            {
                Interval = SyncInterval.Every30Minutes(),
                StartDate = DateTime.UtcNow,
                IsPrivate = false,
                FieldSelection = CalendarFieldSelection.All()
            }
        };

        if (isEnabled)
        {
            calendar.Enable();
        }
        else
        {
            calendar.Disable();
        }

        return calendar;
    }

    private static Credential CreateTestCredential()
    {
        var credential = new Credential();
        
        // Set valid token data
        credential.UpdateStatusData([1, 2, 3, 4]); // Non-empty byte array

        return credential;
    }

    private static CalendarEvent CreateTestEvent(Guid calendarId, string subject)
    {
        return new CalendarEvent
        {
            Id = Guid.CreateVersion7(),
            CalendarId = calendarId,
            ExternalId = $"event_{Guid.NewGuid()}",
            Subject = subject,
            Start = DateTime.UtcNow.AddHours(1),
            End = DateTime.UtcNow.AddHours(2),
            Location = "Meeting Room",
            Body = "Test meeting body",
            Organizer = "organizer@example.com",
            IsAllDay = false,
            IsRecurring = false
        };
    }

    private static Mock<ICalendarEventRepository> CreateMockEventRepository(IReadOnlyList<CalendarEvent> events)
    {
        var mockRepo = new Mock<ICalendarEventRepository>();
        
        mockRepo
            .Setup(r => r.InitAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        
        mockRepo
            .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(events);
        
        mockRepo
            .Setup(r => r.AddAsync(It.IsAny<CalendarEvent>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        
        mockRepo
            .Setup(r => r.FindCopiedEventAsync(
                It.IsAny<CalendarEvent>(),
                It.IsAny<Calendar>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((CalendarEvent?)null);

        return mockRepo;
    }
}
