using Microsoft.EntityFrameworkCore;
using OutlookSync.Domain.Aggregates;
using OutlookSync.Domain.Repositories;
using OutlookSync.Domain.ValueObjects;
using OutlookSync.Infrastructure.Persistence;
using OutlookSync.Infrastructure.Repositories;

namespace OutlookSync.Infrastructure.Tests.Persistence;

/// <summary>
/// Integration tests for the persistence layer including DbContext, Repository, and Unit of Work
/// </summary>
public class PersistenceLayerTests : IDisposable
{
    private readonly OutlookSyncDbContext _context;
    private readonly IRepository<Calendar> _calendarRepository;
    private readonly IRepository<Credential> _credentialRepository;
    private readonly IUnitOfWork _unitOfWork;

    public PersistenceLayerTests()
    {
        var options = new DbContextOptionsBuilder<OutlookSyncDbContext>()
            .UseSqlite("DataSource=:memory:")
            .Options;

        _context = new OutlookSyncDbContext(options);
        _context.Database.OpenConnection();
        _context.Database.EnsureCreated();

        _calendarRepository = new Repository<Calendar>(_context);
        _credentialRepository = new Repository<Credential>(_context);
        _unitOfWork = new UnitOfWork(_context);
    }

    [Fact]
    public async Task AddCredential_WithStatusData_ShouldPersist()
    {
        // Arrange
        var credential = new Credential();
        var statusData = "test_status_data"u8.ToArray();
        credential.UpdateStatusData(statusData);

        // Act
        await _credentialRepository.AddAsync(credential);
        await _unitOfWork.SaveChangesAsync();

        // Assert
        var savedCredential = await _credentialRepository.GetByIdAsync(credential.Id);
        Assert.NotNull(savedCredential);
        Assert.Equal(TokenStatus.Valid, savedCredential.TokenStatus);
        Assert.NotNull(savedCredential.StatusData);
        Assert.Equal(statusData, savedCredential.StatusData);
    }

    [Fact]
    public async Task UpdateCredentialStatus_ShouldPersist()
    {
        // Arrange
        var credential = new Credential();
        credential.UpdateStatusData("test_data"u8.ToArray());
        await _credentialRepository.AddAsync(credential);
        await _unitOfWork.SaveChangesAsync();

        // Act
        credential.MarkTokenAsExpired();
        await _credentialRepository.UpdateAsync(credential);
        await _unitOfWork.SaveChangesAsync();

        // Clear context to force reload from DB
        _context.ChangeTracker.Clear();

        // Assert
        var updatedCredential = await _credentialRepository.GetByIdAsync(credential.Id);
        Assert.NotNull(updatedCredential);
        Assert.Equal(TokenStatus.Expired, updatedCredential.TokenStatus);
        Assert.NotNull(updatedCredential.UpdatedAt);
    }

    [Fact]
    public async Task AddCalendar_WithConfiguration_ShouldPersist()
    {
        // Arrange
        var credential = new Credential();
        credential.UpdateStatusData("test_data"u8.ToArray());
        await _credentialRepository.AddAsync(credential);
        await _unitOfWork.SaveChangesAsync();

        var calendar = new Calendar
        {
            Name = "Test Calendar",
            ExternalId = "test-calendar-123",
            CredentialId = credential.Id,
            Configuration = new SyncConfiguration
            {
                Interval = SyncInterval.Every30Minutes(),
                StartDate = DateTime.UtcNow,
                IsPrivate = false,
                FieldSelection = CalendarFieldSelection.Essential()
            }
        };

        // Act
        await _calendarRepository.AddAsync(calendar);
        await _unitOfWork.SaveChangesAsync();

        // Clear context to force reload from DB
        _context.ChangeTracker.Clear();

        // Assert
        var savedCalendar = await _calendarRepository.GetByIdAsync(calendar.Id);
        Assert.NotNull(savedCalendar);
        Assert.Equal("Test Calendar", savedCalendar.Name);
        Assert.Equal("test-calendar-123", savedCalendar.ExternalId);
        Assert.NotNull(savedCalendar.Configuration);
        Assert.Equal(30, savedCalendar.Configuration.Interval.Minutes);
        Assert.Equal("*/30 * * * *", savedCalendar.Configuration.Interval.CronExpression);
        Assert.False(savedCalendar.Configuration.IsPrivate);
    }

    [Fact]
    public async Task UpdateCalendarConfiguration_ShouldPersist()
    {
        // Arrange
        var credential = new Credential();
        credential.UpdateStatusData("test_data"u8.ToArray());
        await _credentialRepository.AddAsync(credential);

        var calendar = new Calendar
        {
            Name = "Test Calendar",
            ExternalId = "test-calendar-456",
            CredentialId = credential.Id,
            Configuration = new SyncConfiguration
            {
                Interval = SyncInterval.Every15Minutes(),
                StartDate = DateTime.UtcNow,
                IsPrivate = false,
                FieldSelection = CalendarFieldSelection.All()
            }
        };

        await _calendarRepository.AddAsync(calendar);
        await _unitOfWork.SaveChangesAsync();

        // Act - Update configuration
        var newConfig = new SyncConfiguration
        {
            Interval = SyncInterval.Hourly(),
            StartDate = DateTime.UtcNow.AddDays(1),
            IsPrivate = true,
            FieldSelection = CalendarFieldSelection.Essential()
        };
        calendar.UpdateConfiguration(newConfig);
        await _calendarRepository.UpdateAsync(calendar);
        await _unitOfWork.SaveChangesAsync();

        // Clear context to force reload from DB
        _context.ChangeTracker.Clear();

        // Assert
        var updatedCalendar = await _calendarRepository.GetByIdAsync(calendar.Id);
        Assert.NotNull(updatedCalendar);
        Assert.Equal(60, updatedCalendar.Configuration.Interval.Minutes);
        Assert.Equal("0 * * * *", updatedCalendar.Configuration.Interval.CronExpression);
        Assert.True(updatedCalendar.Configuration.IsPrivate);
    }

    [Fact]
    public async Task DeleteCalendar_ShouldRemoveFromDatabase()
    {
        // Arrange
        var credential = new Credential();
        credential.UpdateStatusData("test_data"u8.ToArray());
        await _credentialRepository.AddAsync(credential);

        var calendar = new Calendar
        {
            Name = "Calendar to Delete",
            ExternalId = "calendar-to-delete-789",
            CredentialId = credential.Id,
            Configuration = new SyncConfiguration
            {
                Interval = SyncInterval.Every30Minutes(),
                StartDate = DateTime.UtcNow,
                IsPrivate = false,
                FieldSelection = CalendarFieldSelection.All()
            }
        };

        await _calendarRepository.AddAsync(calendar);
        await _unitOfWork.SaveChangesAsync();

        // Act
        await _calendarRepository.DeleteAsync(calendar);
        await _unitOfWork.SaveChangesAsync();

        // Clear context
        _context.ChangeTracker.Clear();

        // Assert
        var deletedCalendar = await _calendarRepository.GetByIdAsync(calendar.Id);
        Assert.Null(deletedCalendar);
    }

    [Fact]
    public async Task UnitOfWork_Transaction_ShouldCommit()
    {
        // Arrange
        var credential = new Credential();
        credential.UpdateStatusData("test_data"u8.ToArray());

        // Act
        await _unitOfWork.BeginTransactionAsync();
        await _credentialRepository.AddAsync(credential);
        await _unitOfWork.SaveChangesAsync();
        await _unitOfWork.CommitTransactionAsync();

        // Clear context
        _context.ChangeTracker.Clear();

        // Assert
        var savedCredential = await _credentialRepository.GetByIdAsync(credential.Id);
        Assert.NotNull(savedCredential);
    }

    [Fact]
    public async Task UnitOfWork_Transaction_ShouldRollback()
    {
        // Arrange
        var credential = new Credential();
        credential.UpdateStatusData("test_data"u8.ToArray());

        // Act
        await _unitOfWork.BeginTransactionAsync();
        await _credentialRepository.AddAsync(credential);
        await _unitOfWork.SaveChangesAsync();
        await _unitOfWork.RollbackTransactionAsync();

        // Clear context
        _context.ChangeTracker.Clear();

        // Assert
        var savedCredential = await _credentialRepository.GetByIdAsync(credential.Id);
        Assert.Null(savedCredential);
    }

    [Fact]
    public async Task Repository_Query_ShouldReturnFilteredResults()
    {
        // Arrange
        var credential = new Credential();
        credential.UpdateStatusData("test_data"u8.ToArray());
        await _credentialRepository.AddAsync(credential);

        var calendar1 = new Calendar
        {
            Name = "Enabled Calendar",
            ExternalId = "enabled-calendar",
            CredentialId = credential.Id,
            Configuration = new SyncConfiguration
            {
                Interval = SyncInterval.Every30Minutes(),
                StartDate = DateTime.UtcNow,
                IsPrivate = false,
                FieldSelection = CalendarFieldSelection.All()
            }
        };

        var calendar2 = new Calendar
        {
            Name = "Disabled Calendar",
            ExternalId = "disabled-calendar",
            CredentialId = credential.Id,
            Configuration = new SyncConfiguration
            {
                Interval = SyncInterval.Every30Minutes(),
                StartDate = DateTime.UtcNow,
                IsPrivate = false,
                FieldSelection = CalendarFieldSelection.All()
            }
        };
        calendar2.Disable();

        await _calendarRepository.AddAsync(calendar1);
        await _calendarRepository.AddAsync(calendar2);
        await _unitOfWork.SaveChangesAsync();

        // Act
        var enabledCalendars = await _calendarRepository.Query
            .Where(c => c.IsEnabled)
            .ToListAsync();

        // Assert
        Assert.Single(enabledCalendars);
        Assert.Equal("Enabled Calendar", enabledCalendars[0].Name);
    }

    [Fact]
    public async Task RecordSyncSuccess_ShouldUpdateLastSyncAt()
    {
        // Arrange
        var credential = new Credential();
        credential.UpdateStatusData("test_data"u8.ToArray());
        await _credentialRepository.AddAsync(credential);

        var calendar = new Calendar
        {
            Name = "Sync Test Calendar",
            ExternalId = "sync-test-calendar",
            CredentialId = credential.Id,
            Configuration = new SyncConfiguration
            {
                Interval = SyncInterval.Every30Minutes(),
                StartDate = DateTime.UtcNow,
                IsPrivate = false,
                FieldSelection = CalendarFieldSelection.All()
            }
        };

        await _calendarRepository.AddAsync(calendar);
        await _unitOfWork.SaveChangesAsync();

        // Act
        calendar.RecordSuccessfulSync(10);
        await _calendarRepository.UpdateAsync(calendar);
        await _unitOfWork.SaveChangesAsync();

        // Clear context
        _context.ChangeTracker.Clear();

        // Assert
        var updatedCalendar = await _calendarRepository.GetByIdAsync(calendar.Id);
        Assert.NotNull(updatedCalendar);
        Assert.NotNull(updatedCalendar.LastSyncAt);
    }

    public void Dispose()
    {
        _context.Database.CloseConnection();
        _context.Dispose();
    }
}
