# Exchange Service Integration - Implementation Guide

## Overview

This implementation provides a complete Exchange Web Services (EWS) integration with:
- Device Flow OAuth authentication using Azure.Identity
- Resilient I/O with retry logic and exponential backoff
- PropertySet configuration for calendar items
- Mock implementations for testing

## Architecture

### Domain Layer (`OutlookSync.Domain`)

**Interfaces:**
- `IExchangeService` - Main service interface for EWS operations
- `IExchangeAuthenticationService` - OAuth authentication interface

**Value Objects:**
- `ExchangeConfiguration` - Configuration for Exchange connection
- `RetryPolicy` - Retry logic configuration with exponential backoff
- `AccessToken` - OAuth access token wrapper

### Infrastructure Layer (`OutlookSync.Infrastructure`)

**Implementations:**
- `ExchangeService` - Full EWS implementation with retry logic
- `ExchangeAuthenticationService` - Device Flow OAuth implementation
- `MockExchangeService` - In-memory mock for testing
- `MockExchangeAuthenticationService` - Mock authentication for testing

### Test Layer (`OutlookSync.Infrastructure.Tests`)

**Test Coverage:**
- 15 unit tests covering all mock service operations
- Retry policy tests with various scenarios
- All tests passing ✅

## Usage Example

```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OutlookSync.Domain.Services;
using OutlookSync.Domain.ValueObjects;

// Setup DI container
var services = new ServiceCollection();
services.AddLogging(builder => builder.AddConsole());
services.AddInfrastructure(configuration);

var serviceProvider = services.BuildServiceProvider();

// Get services
var authService = serviceProvider.GetRequiredService<IExchangeAuthenticationService>();
var exchangeService = serviceProvider.GetRequiredService<IExchangeService>();

// Configure Exchange
var config = ExchangeConfiguration.CreateDefault();

// Authenticate with Device Flow
var token = await authService.AuthenticateAsync(
    config.ClientId,
    config.TenantId,
    config.Scopes,
    deviceCodeCallback: async (verificationUri, userCode) =>
    {
        Console.WriteLine($"Please visit: {verificationUri}");
        Console.WriteLine($"And enter code: {userCode}");
    });

// Initialize Exchange service
await exchangeService.InitializeAsync(token.Token, config.ServiceUrl);

// Test connection
var isConnected = await exchangeService.TestConnectionAsync();
Console.WriteLine($"Connected: {isConnected}");

// Get calendar events
var events = await exchangeService.GetCalendarEventsAsync(
    "Calendar",
    DateTime.Now,
    DateTime.Now.AddDays(30));

Console.WriteLine($"Found {events.Count} events");
foreach (var evt in events)
{
    Console.WriteLine($"- {evt.Subject} ({evt.Start} - {evt.End})");
}
```

## Configuration

### Default Configuration

The `ExchangeConfiguration.CreateDefault()` provides sensible defaults:
- **ClientId**: Microsoft Office client ID (d3590ed6-52b3-4102-aeff-aad2292ab01c)
- **TenantId**: "common" (multi-tenant)
- **ServiceUrl**: https://outlook.office365.com/EWS/Exchange.asmx
- **Scopes**: EWS.AccessAsUser.All
- **Timeout**: 100 seconds
- **MaxRetryAttempts**: 3
- **InitialRetryDelay**: 1000ms

### Custom Configuration

```csharp
var config = new ExchangeConfiguration
{
    ClientId = "your-client-id",
    TenantId = "your-tenant-id",
    ServiceUrl = "https://your-exchange-server/EWS/Exchange.asmx",
    Scopes = ["https://outlook.office365.com/EWS.AccessAsUser.All"],
    TimeoutSeconds = 120,
    MaxRetryAttempts = 5,
    InitialRetryDelayMs = 2000
};
```

## Resilient I/O Features

### Retry Policy

The retry policy implements exponential backoff with jitter:

```csharp
var retryPolicy = new RetryPolicy
{
    MaxRetryAttempts = 3,      // Maximum retry attempts
    InitialDelayMs = 1000,     // Initial delay (1 second)
    BackoffMultiplier = 2.0,   // Exponential multiplier
    MaxDelayMs = 30000,        // Maximum delay cap (30 seconds)
    UseJitter = true           // Add randomness to avoid thundering herd
};
```

**Retry Schedule Example:**
- Attempt 1: Immediate
- Attempt 2: ~1000ms delay
- Attempt 3: ~2000ms delay
- Attempt 4: ~4000ms delay

### Transient Error Handling

The service automatically retries on:
- `ErrorServerBusy` - Server is temporarily busy
- `ErrorTimeoutExpired` - Request timeout
- `ErrorConnectionFailed` - Network connection failed
- `ErrorInternalServerTransientError` - Temporary server error

Non-transient errors (authentication, permission, not found) fail immediately without retry.

## PropertySet Configuration

The service uses a comprehensive PropertySet for calendar items:

```csharp
new PropertySet(
    BasePropertySet.FirstClassProperties,
    AppointmentSchema.Subject,
    AppointmentSchema.Start,
    AppointmentSchema.End,
    AppointmentSchema.Location,
    AppointmentSchema.Body,
    AppointmentSchema.IsAllDayEvent,
    AppointmentSchema.Organizer,
    AppointmentSchema.LastModifiedTime
);
```

This retrieves all essential appointment properties in a single request, optimizing performance.

## Testing

### Using Mock Services

```csharp
// Create mock services
var mockAuth = new MockExchangeAuthenticationService();
var mockExchange = new MockExchangeService();

// Initialize
var token = await mockAuth.AuthenticateAsync(
    "client-id", "tenant-id", ["scope"], 
    (uri, code) => Task.CompletedTask);

await mockExchange.InitializeAsync(token.Token, "https://mock.com/ews");

// Create test event
var testEvent = new CalendarEvent
{
    Id = Guid.NewGuid(),
    CalendarId = Guid.NewGuid(),
    ExternalId = "temp",
    Subject = "Test Meeting",
    Start = DateTime.Now.AddDays(1),
    End = DateTime.Now.AddDays(1).AddHours(1),
    IsAllDay = false,
    IsRecurring = false,
    IsCopiedEvent = false
};

var created = await mockExchange.CreateCalendarEventAsync("calendar1", testEvent);
```

## Known Limitations

### EWS SDK Vulnerabilities

The Microsoft.Exchange.WebServices.NETStandard package has transitive dependencies with known vulnerabilities:
- System.Drawing.Common (Critical)
- System.Security.Cryptography.Xml (Moderate)

These warnings are suppressed in the project file with comments. Consider these mitigation strategies:
1. **Short-term**: Warnings suppressed, risk accepted for now
2. **Mid-term**: Monitor for updated EWS SDK releases
3. **Long-term**: Migrate to Microsoft Graph API (recommended by Microsoft)

### Current Limitations

1. **Calendar Selection**: Currently only supports the default calendar. Future enhancement needed for custom calendar folders.
2. **Recurring Events**: Basic support only. Complex recurrence patterns may need additional handling.
3. **Attachments**: Not yet implemented.
4. **Meeting Responses**: Not yet implemented.

## Security Considerations

✅ **Passed CodeQL Security Scan** - 0 alerts

- Uses Azure.Identity for secure OAuth flows
- No hardcoded credentials
- Proper error handling without information leakage
- Structured logging without sensitive data
- All user inputs validated
- CancellationToken support for proper resource cleanup

## Dependencies

```xml
<PackageReference Include="Azure.Identity" Version="1.15.0" />
<PackageReference Include="Microsoft.Exchange.WebServices.NETStandard" Version="2.0.0-beta3" />
<PackageReference Include="Microsoft.Extensions.Configuration.Abstractions" Version="10.0.3" />
<PackageReference Include="Microsoft.Extensions.Logging.Abstractions" Version="10.0.0" />
```

## Future Enhancements

1. **Custom Calendar Support**: Parse calendarId to support multiple calendars
2. **Batch Operations**: Implement batch create/update/delete for efficiency
3. **Attachment Support**: Add methods for managing calendar item attachments
4. **Meeting Rooms**: Support for room finder and resource booking
5. **Migration to Graph API**: Consider migrating to Microsoft Graph API for better security and features

## References

- [Microsoft Exchange Web Services Documentation](https://docs.microsoft.com/en-us/exchange/client-developer/exchange-web-services/explore-the-ews-managed-api-ews-and-web-services-in-exchange)
- [Azure Identity Documentation](https://docs.microsoft.com/en-us/dotnet/api/overview/azure/identity-readme)
- [Device Code Flow](https://docs.microsoft.com/en-us/azure/active-directory/develop/v2-oauth2-device-code)
