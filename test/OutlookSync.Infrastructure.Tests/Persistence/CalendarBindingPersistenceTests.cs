using Microsoft.EntityFrameworkCore;
using OutlookSync.Domain.Aggregates;
using OutlookSync.Domain.ValueObjects;
using OutlookSync.Infrastructure.Persistence;
using OutlookSync.Infrastructure.Repositories;

namespace OutlookSync.Infrastructure.Tests.Persistence;

/// <summary>
/// Integration tests for CalendarBinding persistence with EF Core
/// </summary>
public class CalendarBindingPersistenceTests : IDisposable
{
    private readonly OutlookSyncDbContext _context;
    private readonly CalendarBindingRepository _repository;

    public CalendarBindingPersistenceTests()
    {
        var options = new DbContextOptionsBuilder<OutlookSyncDbContext>()
            .UseSqlite("DataSource=:memory:")
            .Options;

        _context = new OutlookSyncDbContext(options);
        _context.Database.OpenConnection();
        _context.Database.EnsureCreated();

        _repository = new CalendarBindingRepository(_context);
    }

    [Fact]
    public async Task AddCalendarBinding_WithDefaultConfiguration_ShouldPersistSyncInterval()
    {
        // Arrange
        var binding = new CalendarBinding
        {
            Name = "Test Binding",
            SourceCredentialId = Guid.CreateVersion7(),
            SourceCalendarExternalId = "source-calendar-id",
            TargetCredentialId = Guid.CreateVersion7(),
            TargetCalendarExternalId = "target-calendar-id",
            Configuration = CalendarBindingConfiguration.Default()
        };

        // Act
        await _repository.AddAsync(binding);
        await _context.SaveChangesAsync();

        // Clear context to force reload from DB
        _context.ChangeTracker.Clear();

        // Assert
        var savedBinding = await _repository.GetByIdAsync(binding.Id);
        Assert.NotNull(savedBinding);
        Assert.NotNull(savedBinding.Configuration.Interval);
        Assert.Equal(30, savedBinding.Configuration.Interval.Minutes);
        Assert.Equal("*/30 * * * *", savedBinding.Configuration.Interval.CronExpression);
        Assert.Equal(30, savedBinding.Configuration.SyncDaysForward);
    }

    [Fact]
    public async Task AddCalendarBinding_WithEvery15MinutesInterval_ShouldPersist()
    {
        // Arrange
        var config = CalendarBindingConfiguration.Default() with 
        { 
            Interval = SyncInterval.Every15Minutes(),
            SyncDaysForward = 60
        };

        var binding = new CalendarBinding
        {
            Name = "Test Binding",
            SourceCredentialId = Guid.CreateVersion7(),
            SourceCalendarExternalId = "source-calendar-id",
            TargetCredentialId = Guid.CreateVersion7(),
            TargetCalendarExternalId = "target-calendar-id",
            Configuration = config
        };

        // Act
        await _repository.AddAsync(binding);
        await _context.SaveChangesAsync();

        // Clear context
        _context.ChangeTracker.Clear();

        // Assert
        var savedBinding = await _repository.GetByIdAsync(binding.Id);
        Assert.NotNull(savedBinding);
        Assert.Equal(15, savedBinding.Configuration.Interval.Minutes);
        Assert.Equal("*/15 * * * *", savedBinding.Configuration.Interval.CronExpression);
        Assert.Equal(60, savedBinding.Configuration.SyncDaysForward);
    }

    [Fact]
    public async Task AddCalendarBinding_WithHourlyInterval_ShouldPersist()
    {
        // Arrange
        var config = CalendarBindingConfiguration.Default() with 
        { 
            Interval = SyncInterval.Hourly()
        };

        var binding = new CalendarBinding
        {
            Name = "Test Binding",
            SourceCredentialId = Guid.CreateVersion7(),
            SourceCalendarExternalId = "source-calendar-id",
            TargetCredentialId = Guid.CreateVersion7(),
            TargetCalendarExternalId = "target-calendar-id",
            Configuration = config
        };

        // Act
        await _repository.AddAsync(binding);
        await _context.SaveChangesAsync();

        // Clear context
        _context.ChangeTracker.Clear();

        // Assert
        var savedBinding = await _repository.GetByIdAsync(binding.Id);
        Assert.NotNull(savedBinding);
        Assert.Equal(60, savedBinding.Configuration.Interval.Minutes);
        Assert.Equal("0 * * * *", savedBinding.Configuration.Interval.CronExpression);
    }

    [Fact]
    public async Task AddCalendarBinding_WithCustomInterval_ShouldPersist()
    {
        // Arrange
        var config = CalendarBindingConfiguration.Default() with 
        { 
            Interval = SyncInterval.Custom(45, "*/45 * * * *")
        };

        var binding = new CalendarBinding
        {
            Name = "Test Binding",
            SourceCredentialId = Guid.CreateVersion7(),
            SourceCalendarExternalId = "source-calendar-id",
            TargetCredentialId = Guid.CreateVersion7(),
            TargetCalendarExternalId = "target-calendar-id",
            Configuration = config
        };

        // Act
        await _repository.AddAsync(binding);
        await _context.SaveChangesAsync();

        // Clear context
        _context.ChangeTracker.Clear();

        // Assert
        var savedBinding = await _repository.GetByIdAsync(binding.Id);
        Assert.NotNull(savedBinding);
        Assert.Equal(45, savedBinding.Configuration.Interval.Minutes);
        Assert.Equal("*/45 * * * *", savedBinding.Configuration.Interval.CronExpression);
    }

    [Fact]
    public async Task UpdateCalendarBinding_ChangingSyncInterval_ShouldPersist()
    {
        // Arrange
        var binding = new CalendarBinding
        {
            Name = "Test Binding",
            SourceCredentialId = Guid.CreateVersion7(),
            SourceCalendarExternalId = "source-calendar-id",
            TargetCredentialId = Guid.CreateVersion7(),
            TargetCalendarExternalId = "target-calendar-id",
            Configuration = CalendarBindingConfiguration.Default()
        };

        await _repository.AddAsync(binding);
        await _context.SaveChangesAsync();

        // Act
        var updatedConfig = binding.Configuration with 
        { 
            Interval = SyncInterval.Every15Minutes(),
            SyncDaysForward = 90
        };
        binding.UpdateConfiguration(updatedConfig);
        await _repository.UpdateAsync(binding);
        await _context.SaveChangesAsync();

        // Clear context
        _context.ChangeTracker.Clear();

        // Assert
        var updatedBinding = await _repository.GetByIdAsync(binding.Id);
        Assert.NotNull(updatedBinding);
        Assert.Equal(15, updatedBinding.Configuration.Interval.Minutes);
        Assert.Equal(90, updatedBinding.Configuration.SyncDaysForward);
    }

    [Fact]
    public async Task AddCalendarBinding_WithPrivacyFocusedConfig_ShouldPersistAllFields()
    {
        // Arrange
        var binding = new CalendarBinding
        {
            Name = "Privacy Binding",
            SourceCredentialId = Guid.CreateVersion7(),
            SourceCalendarExternalId = "source-calendar-id",
            TargetCredentialId = Guid.CreateVersion7(),
            TargetCalendarExternalId = "target-calendar-id",
            Configuration = CalendarBindingConfiguration.PrivacyFocused()
        };

        // Act
        await _repository.AddAsync(binding);
        await _context.SaveChangesAsync();

        // Clear context
        _context.ChangeTracker.Clear();

        // Assert
        var savedBinding = await _repository.GetByIdAsync(binding.Id);
        Assert.NotNull(savedBinding);
        Assert.Equal(30, savedBinding.Configuration.Interval.Minutes);
        Assert.Equal(30, savedBinding.Configuration.SyncDaysForward);
        Assert.Equal(TitleHandling.Hide, savedBinding.Configuration.TitleHandling);
        Assert.True(savedBinding.Configuration.MarkAsPrivate);
    }

    [Fact]
    public async Task AddCalendarBinding_WithMaxSyncDaysForward_ShouldPersist()
    {
        // Arrange
        var config = CalendarBindingConfiguration.Default() with 
        { 
            SyncDaysForward = 365
        };

        var binding = new CalendarBinding
        {
            Name = "Test Binding",
            SourceCredentialId = Guid.CreateVersion7(),
            SourceCalendarExternalId = "source-calendar-id",
            TargetCredentialId = Guid.CreateVersion7(),
            TargetCalendarExternalId = "target-calendar-id",
            Configuration = config
        };

        // Act
        await _repository.AddAsync(binding);
        await _context.SaveChangesAsync();

        // Clear context
        _context.ChangeTracker.Clear();

        // Assert
        var savedBinding = await _repository.GetByIdAsync(binding.Id);
        Assert.NotNull(savedBinding);
        Assert.Equal(365, savedBinding.Configuration.SyncDaysForward);
    }

    public void Dispose()
    {
        _context.Database.CloseConnection();
        _context.Dispose();
    }
}
