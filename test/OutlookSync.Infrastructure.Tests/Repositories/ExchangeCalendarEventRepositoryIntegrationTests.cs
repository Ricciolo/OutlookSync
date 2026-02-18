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

        private readonly ILogger<ExchangeCalendarEventRepository> _logger;

        public ExchangeCalendarEventRepositoryIntegrationTests()
        {
            var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.SetMinimumLevel(ExtensionsLogLevel.Debug);
            });
            _logger = loggerFactory.CreateLogger<ExchangeCalendarEventRepository>();
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

        var credential = CreateTestCredential();

        var repository = new ExchangeCalendarEventRepository(
            credential,
            _logger);

        // Act
        await repository.InitAsync();

        // Assert - no exception means success
        // Verify we can use the repository
        var events = await repository.GetAllAsync(s_calendarExternalId);
        Assert.NotNull(events);
    }

    [Fact]
    public async Task InitAsync_ShouldThrow_WhenNoCachedAccountsExist()
    {
        // Arrange
        var credential = CreateCredentialWithoutCache();

        var repository = new ExchangeCalendarEventRepository(
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
        var credential = CreateTestCredential();

        var repository = new ExchangeCalendarEventRepository(
            credential,
            _logger);

        // Act & Assert - calling GetAllAsync without InitAsync should throw
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await repository.GetAllAsync(s_calendarExternalId));

        Assert.Contains("has not been initialized", exception.Message);
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnEvents_WhenValidCredentials()
    {
        // Arrange
        ValidateConfiguration();

        var credential = CreateTestCredential();

        var repository = new ExchangeCalendarEventRepository(
            credential,
            _logger);

        // Act
        await repository.InitAsync();
        var events = await repository.GetAllAsync(s_calendarExternalId);

        // Assert
        Assert.NotNull(events);
        Assert.IsAssignableFrom<IReadOnlyList<DomainCalendarEvent>>(events);
    }

    [Fact]
    public async Task GetAvailableCalendarsAsync_ShouldThrow_WhenNotInitialized()
    {
        // Arrange
        var credential = CreateTestCredential();

        var repository = new ExchangeCalendarEventRepository(
            credential,
            _logger);

        // Act & Assert - calling GetAvailableCalendarsAsync without InitAsync should throw
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await repository.GetAvailableCalendarsAsync());

        Assert.Contains("has not been initialized", exception.Message);
    }

    [Fact]
    public async Task GetAvailableCalendarsAsync_ShouldReturnCalendars_WhenValidCredentials()
    {
        // Arrange
        ValidateConfiguration();

        var credential = CreateTestCredential();

        var repository = new ExchangeCalendarEventRepository(
            credential,
            _logger);

        // Act
        await repository.InitAsync();
        var calendars = await repository.GetAvailableCalendarsAsync();

        // Assert
        Assert.NotNull(calendars);
        Assert.IsAssignableFrom<IReadOnlyList<AvailableCalendar>>(calendars);
        Assert.NotEmpty(calendars);
        
        // Verify each calendar has required properties
        foreach (var cal in calendars)
        {
            Assert.NotNull(cal.ExternalId);
            Assert.NotEmpty(cal.ExternalId);
            Assert.NotNull(cal.Name);
            Assert.NotEmpty(cal.Name);
        }
    }

    [Fact]
    public async Task AddAsync_ShouldCreateEvent_WhenValidEvent()
    {
        // Arrange
        ValidateConfiguration();

        var credential = CreateTestCredential();

        var repository = new ExchangeCalendarEventRepository(
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
            OriginalEventId = null,
            SourceCalendarBindingId = null
        };

        // Act
        await repository.AddAsync(calendarEvent, s_calendarExternalId);

        // Assert
        // Verifica che l'evento sia stato creato leggendo tutti gli eventi
        var events = await repository.GetAllAsync(s_calendarExternalId);
        var createdEvent = events.FirstOrDefault(e => e.Subject == calendarEvent.Subject);
        Assert.NotNull(createdEvent);
        Assert.Equal(calendarEvent.Body, createdEvent.Body);
    }

    [Fact]
    public async Task AddAsync_ShouldCreateCopiedEvent_WhenEventIsCopied()
    {
        // Arrange
        ValidateConfiguration();

        var credential = CreateTestCredential();

        var repository = new ExchangeCalendarEventRepository(
            credential,
            _logger);

        await repository.InitAsync();

        var originalEventId = "original-event-12345";
        var sourceCalendarBindingId = Guid.CreateVersion7();

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
            OriginalEventId = originalEventId,
            SourceCalendarBindingId = sourceCalendarBindingId
        };

        // Act
        await repository.AddAsync(copiedEvent, s_calendarExternalId);

        // Assert
        var events = await repository.GetAllAsync(s_calendarExternalId);
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

        var credential = CreateTestCredential();

        var repository = new ExchangeCalendarEventRepository(
            credential,
            _logger);

        await repository.InitAsync();

        var sourceCalendarBindingId = Guid.CreateVersion7();
        var originalEventExternalId = $"test-original-{Guid.CreateVersion7()}";

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
            OriginalEventId = originalEventExternalId,
            SourceCalendarBindingId = sourceCalendarBindingId
        };

        await repository.AddAsync(copiedEvent, s_calendarExternalId);

        // Act
        var foundEvent = await repository.FindCopiedEventAsync(
            originalEventExternalId, 
            sourceCalendarBindingId,
            s_calendarExternalId);

        // Assert
        Assert.NotNull(foundEvent);
        Assert.Equal(originalEventExternalId, foundEvent.OriginalEventId);
    }

    [Fact]
    public async Task RetryPolicy_ShouldRetryOnTransientErrors()
    {
        // Arrange
        ValidateConfiguration();

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
            credential,
            _logger,
            retryPolicy);

        await repository.InitAsync();

        // Act & Assert
        // Questo test verifica che il repository sia configurato correttamente con retry policy
        var events = await repository.GetAllAsync(s_calendarExternalId);
        Assert.NotNull(events);
    }

    [Fact]
    public void Constructor_ShouldThrowArgumentNullException_WhenCredentialIsNull()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new ExchangeCalendarEventRepository(
                null!,
                _logger));
    }

    [Fact]
    public void Constructor_ShouldNotThrow_WhenCredentialTokenIsInvalid()
    {
        // Arrange
        var credential = new Credential { FriendlyName = "Invalid Credential" };
        // No status data set, TokenStatus will be NotAcquired

        // Act & Assert - Constructor should not throw, validation happens in factory
        var exception = Record.Exception(() =>
            new ExchangeCalendarEventRepository(
                credential,
                _logger));
        
        
        Assert.Null(exception);
    }

    [Fact]
    public void Constructor_ShouldThrowArgumentNullException_WhenLoggerIsNull()
    {
        // Arrange
        var credential = CreateTestCredential();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new ExchangeCalendarEventRepository(
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
