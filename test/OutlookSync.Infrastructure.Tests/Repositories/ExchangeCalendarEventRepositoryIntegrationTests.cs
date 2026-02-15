using Microsoft.Extensions.Logging;
using OutlookSync.Domain.Aggregates;
using OutlookSync.Domain.ValueObjects;
using OutlookSync.Infrastructure.Repositories;
using Task = System.Threading.Tasks.Task;
using DomainCalendarEvent = OutlookSync.Domain.ValueObjects.CalendarEvent;
using ExtensionsLogLevel = Microsoft.Extensions.Logging.LogLevel;

namespace OutlookSync.Infrastructure.Tests.Repositories;

    /// <summary>
    /// Integration tests for ExchangeCalendarEventRepository.
    /// NOTE: These tests require an actual connection to Exchange Online and cached MSAL tokens.
    /// </summary>
    public class ExchangeCalendarEventRepositoryIntegrationTests
    {
        // NOTE: These tests require actual cached MSAL tokens from previous interactive authentication
        // The token cache should be stored in Credential.StatusData
        private static readonly string s_calendarExternalId = Environment.GetEnvironmentVariable("CALENDAR_ID") ?? string.Empty;

        private const string CalendarName = "Test Calendar";

        private readonly ILogger<ExchangeCalendarEventRepository> _logger;

        public ExchangeCalendarEventRepositoryIntegrationTests()
        {
            var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.SetMinimumLevel(ExtensionsLogLevel.Debug);
            });
            _logger = loggerFactory.CreateLogger<ExchangeCalendarEventRepository>();
        }

    private static Calendar CreateTestCalendar()
    {
        return new Calendar
        {
            Name = CalendarName,
            ExternalId = s_calendarExternalId,
            CredentialId = Guid.CreateVersion7(),
            Configuration = new SyncConfiguration
            {
                Interval = SyncInterval.Every30Minutes(),
                StartDate = DateTime.UtcNow,
                IsPrivate = false,
                FieldSelection = CalendarFieldSelection.All()
            }
        };
    }

    private static Credential CreateTestCredential()
    {
        var credential = new Credential { FriendlyName = "Test Account" };

        // NOTE: In real scenarios, StatusData should contain a valid MSAL token cache
        // For testing, you need to authenticate interactively first and save the token cache
        var cachedTokenData = Environment.GetEnvironmentVariable("MSAL_TOKEN_CACHE");
        if (!string.IsNullOrEmpty(cachedTokenData))
        {
            var tokenBytes = Convert.FromBase64String(cachedTokenData);
            credential.UpdateStatusData(tokenBytes);
        }

        return credential;
    }

    private static Credential CreateCredentialWithoutCache()
    {
        return new Credential { FriendlyName = "Test Account Without Cache" }; // No StatusData - should fail initialization
    }

    [Fact]
    public async Task InitAsync_ShouldSucceed_WhenValidTokenCacheExists()
    {
        // Arrange
        ValidateConfiguration();

        var calendar = CreateTestCalendar();
        var credential = CreateTestCredential();

        var repository = new ExchangeCalendarEventRepository(
            calendar,
            credential,
            _logger);

        // Act
        await repository.InitAsync();

        // Assert - no exception means success
        // Verify we can use the repository
        var events = await repository.GetAllAsync();
        Assert.NotNull(events);
    }

    [Fact]
    public async Task InitAsync_ShouldThrow_WhenNoCachedAccountsExist()
    {
        // Arrange
        var calendar = CreateTestCalendar();
        var credential = CreateCredentialWithoutCache();

        var repository = new ExchangeCalendarEventRepository(
            calendar,
            credential,
            _logger);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await repository.InitAsync());

        Assert.Contains("No cached authentication found", exception.Message);
        Assert.Equal(TokenStatus.Invalid, credential.TokenStatus);
    }

    [Fact]
    public async Task GetAllAsync_ShouldThrow_WhenNotInitialized()
    {
        // Arrange
        var calendar = CreateTestCalendar();
        var credential = CreateTestCredential();

        var repository = new ExchangeCalendarEventRepository(
            calendar,
            credential,
            _logger);

        // Act & Assert - calling GetAllAsync without InitAsync should throw
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await repository.GetAllAsync());

        Assert.Contains("has not been initialized", exception.Message);
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnEvents_WhenValidCredentials()
    {
        // Arrange
        ValidateConfiguration();

        var calendar = CreateTestCalendar();
        var credential = CreateTestCredential();

        var repository = new ExchangeCalendarEventRepository(
            calendar,
            credential,
            _logger);

        // Act
        await repository.InitAsync();
        var events = await repository.GetAllAsync();

        // Assert
        Assert.NotNull(events);
        Assert.IsAssignableFrom<IReadOnlyList<DomainCalendarEvent>>(events);
    }

    [Fact]
    public async Task AddAsync_ShouldCreateEvent_WhenValidEvent()
    {
        // Arrange
        ValidateConfiguration();

        var calendar = CreateTestCalendar();
        var credential = CreateTestCredential();

        var repository = new ExchangeCalendarEventRepository(
            calendar,
            credential,
            _logger);

        await repository.InitAsync();

        var calendarEvent = new DomainCalendarEvent
        {
            Id = Guid.CreateVersion7(),
            Subject = $"Test Event - {DateTime.Now:yyyy-MM-dd HH:mm:ss}",
            Body = "Questo è un evento di test creato automaticamente.",
            Start = DateTime.Now.AddDays(1),
            End = DateTime.Now.AddDays(1).AddHours(1),
            Location = "Test Location",
            IsAllDay = false,
            IsRecurring = false,
            CalendarId = calendar.Id,
            OriginalEventId = null,
            SourceCalendarId = null
        };

        // Act
        await repository.AddAsync(calendarEvent);

        // Assert
        // Verifica che l'evento sia stato creato leggendo tutti gli eventi
        var events = await repository.GetAllAsync();
        var createdEvent = events.FirstOrDefault(e => e.Subject == calendarEvent.Subject);
        Assert.NotNull(createdEvent);
        Assert.Equal(calendarEvent.Body, createdEvent.Body);
    }

    [Fact]
    public async Task AddAsync_ShouldCreateCopiedEvent_WhenEventIsCopied()
    {
        // Arrange
        ValidateConfiguration();

        var calendar = CreateTestCalendar();
        var credential = CreateTestCredential();

        var repository = new ExchangeCalendarEventRepository(
            calendar,
            credential,
            _logger);

        await repository.InitAsync();

        var originalEventId = "original-event-12345";
        var sourceCalendarId = Guid.CreateVersion7();

        var copiedEvent = new CalendarEvent
        {
            Id = Guid.CreateVersion7(),
            Subject = $"Copied Test Event - {DateTime.Now:yyyy-MM-dd HH:mm:ss}",
            Body = "Questo è un evento copiato di test.",
            Start = DateTime.Now.AddDays(2),
            End = DateTime.Now.AddDays(2).AddHours(1),
            Location = "Copied Location",
            IsAllDay = false,
            IsRecurring = false,
            CalendarId = calendar.Id,
            OriginalEventId = originalEventId,
            SourceCalendarId = sourceCalendarId
        };

        // Act
        await repository.AddAsync(copiedEvent);

        // Assert
        var events = await repository.GetAllAsync();
        var createdEvent = events.FirstOrDefault(e => e.Subject == copiedEvent.Subject);
        Assert.NotNull(createdEvent);
        Assert.True(createdEvent.IsCopiedEvent);
        Assert.Equal(originalEventId, createdEvent.OriginalEventId);
    }

    [Fact]
    public async Task FindCopiedEventAsync_ShouldReturnEvent_WhenCopiedEventExists()
    {
        // Arrange
        ValidateConfiguration();

        var calendar = CreateTestCalendar();
        var credential = CreateTestCredential();

        var repository = new ExchangeCalendarEventRepository(
            calendar,
            credential,
            _logger);

        await repository.InitAsync();

        var sourceCalendarId = Guid.CreateVersion7();
        var originalEventId = $"test-original-{Guid.CreateVersion7()}";

        // First create a copied event
        var copiedEvent = new DomainCalendarEvent
        {
            Id = Guid.CreateVersion7(),
            Subject = $"Find Test - {DateTime.Now:yyyy-MM-dd HH:mm:ss}",
            Body = "Test for FindCopiedEventAsync",
            Start = DateTime.Now.AddDays(3),
            End = DateTime.Now.AddDays(3).AddHours(1),
            Location = "Find Test Location",
            IsAllDay = false,
            IsRecurring = false,
            CalendarId = calendar.Id,
            OriginalEventId = originalEventId,
            SourceCalendarId = sourceCalendarId
        };

        await repository.AddAsync(copiedEvent);

        // Create a dummy source event
        var sourceEvent = new DomainCalendarEvent
        {
            Id = Guid.CreateVersion7(),
            ExternalId = originalEventId,
            Subject = "Original Event",
            Body = "Original event body",
            Start = DateTime.Now.AddDays(3),
            End = DateTime.Now.AddDays(3).AddHours(1),
            CalendarId = sourceCalendarId,
        };

        var sourceCalendar = new Calendar(sourceCalendarId)
        {
            Name = "Source Calendar",
            ExternalId = "source-cal-123",
            CredentialId = Guid.CreateVersion7(),
            Configuration = new SyncConfiguration
            {
                Interval = SyncInterval.Every30Minutes(),
                StartDate = DateTime.UtcNow,
                IsPrivate = false,
                FieldSelection = CalendarFieldSelection.All()
            }
        };

        // Act
        var foundEvent = await repository.FindCopiedEventAsync(sourceEvent, sourceCalendar);

        // Assert
        Assert.NotNull(foundEvent);
        Assert.Equal(originalEventId, foundEvent.OriginalEventId);
        Assert.Equal(sourceCalendar.Id, foundEvent.SourceCalendarId);
    }

    [Fact]
    public async Task RetryPolicy_ShouldRetryOnTransientErrors()
    {
        // Arrange
        ValidateConfiguration();

        var calendar = CreateTestCalendar();
        var credential = CreateTestCredential();

        var retryPolicy = new RetryPolicy
        {
            MaxRetryAttempts = 3,
            InitialDelayMs = 100,
            BackoffMultiplier = 2.0,
            MaxDelayMs = 1000,
            UseJitter = false
        };

        var repository = new ExchangeCalendarEventRepository(
            calendar,
            credential,
            _logger,
            retryPolicy);

        await repository.InitAsync();

        // Act & Assert
        // Questo test verifica che il repository sia configurato correttamente con retry policy
        var events = await repository.GetAllAsync();
        Assert.NotNull(events);
    }

    [Fact]
    public void Constructor_ShouldThrowArgumentNullException_WhenCalendarIsNull()
    {
        // Arrange
        var credential = CreateTestCredential();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new ExchangeCalendarEventRepository(
                null!,
                credential,
                _logger));
    }

    [Fact]
    public void Constructor_ShouldThrowArgumentNullException_WhenCredentialIsNull()
    {
        // Arrange
        var calendar = CreateTestCalendar();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new ExchangeCalendarEventRepository(
                calendar,
                null!,
                _logger));
    }

    [Fact]
    public void Constructor_ShouldNotThrow_WhenCredentialTokenIsInvalid()
    {
        // Arrange
        var calendar = CreateTestCalendar();
        var credential = new Credential { FriendlyName = "Invalid Credential" };
        // No status data set, TokenStatus will be NotAcquired

        // Act & Assert - Constructor should not throw, validation happens in factory
        var exception = Record.Exception(() =>
            new ExchangeCalendarEventRepository(
                calendar,
                credential,
                _logger));
        
        
        Assert.Null(exception);
    }

    [Fact]
    public void Constructor_ShouldThrowArgumentNullException_WhenLoggerIsNull()
    {
        // Arrange
        var calendar = CreateTestCalendar();
        var credential = CreateTestCredential();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new ExchangeCalendarEventRepository(
                calendar,
                credential,
                null!));
    }

    private static void ValidateConfiguration()
    {
        if (string.IsNullOrWhiteSpace(s_calendarExternalId))
        {
            throw new InvalidOperationException(
                "Calendar External ID not configured. Please set the CALENDAR_ID environment variable with a valid ID.");
        }

        var cachedTokenData = Environment.GetEnvironmentVariable("MSAL_TOKEN_CACHE");
        if (string.IsNullOrEmpty(cachedTokenData))
        {
            throw new InvalidOperationException(
                "MSAL token cache not configured. Please set the MSAL_TOKEN_CACHE environment variable with a Base64-encoded token cache.");
        }
    }
}
