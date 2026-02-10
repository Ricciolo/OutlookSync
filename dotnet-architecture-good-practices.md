# .NET Architecture Good Practices

> **Return to**: [legend.md](./legend.md) | [epic-root-outlooksync.md](./epic-root-outlooksync.md)

This document provides comprehensive architectural guidelines for building robust, maintainable .NET applications following industry best practices.

## Table of Contents

1. [Architectural Patterns](#architectural-patterns)
2. [Layered Architecture](#layered-architecture)
3. [Dependency Management](#dependency-management)
4. [Asynchronous Programming](#asynchronous-programming)
5. [Error Handling](#error-handling)
6. [Configuration and Secrets](#configuration-and-secrets)
7. [Performance Best Practices](#performance-best-practices)
8. [Security Guidelines](#security-guidelines)

---

## Architectural Patterns

### Clean Architecture

Clean Architecture (also known as Onion Architecture or Hexagonal Architecture) ensures that business logic remains independent of external concerns.

```
┌─────────────────────────────────────┐
│        Presentation Layer           │  ← UI, Web API, Console
├─────────────────────────────────────┤
│        Application Layer            │  ← Use Cases, Commands, Queries
├─────────────────────────────────────┤
│          Domain Layer               │  ← Entities, Value Objects, Domain Services
└─────────────────────────────────────┘
         ↓ depends on ↓
┌─────────────────────────────────────┐
│      Infrastructure Layer           │  ← Data Access, External Services
└─────────────────────────────────────┘
```

**Key Principles**:
- Domain layer has no dependencies
- All dependencies point inward
- Use interfaces to invert dependencies
- Keep business logic separate from infrastructure

### CQRS (Command Query Responsibility Segregation)

Separate read and write operations for better scalability and maintainability.

```csharp
// Command - Modifies state
public record CreateOrderCommand(int CustomerId, List<OrderItem> Items);

public class CreateOrderCommandHandler
{
    private readonly IOrderRepository _repository;
    
    public async Task<OrderId> HandleAsync(CreateOrderCommand command)
    {
        var order = new Order(command.CustomerId, command.Items);
        await _repository.AddAsync(order);
        return order.Id;
    }
}

// Query - Retrieves data
public record GetOrderByIdQuery(int OrderId);

public class GetOrderByIdQueryHandler
{
    private readonly IReadOnlyOrderRepository _repository;
    
    public async Task<OrderDto> HandleAsync(GetOrderByIdQuery query)
    {
        return await _repository.GetByIdAsync(query.OrderId);
    }
}
```

### Repository Pattern

Abstract data access to provide a collection-like interface.

```csharp
public interface IRepository<T> where T : Entity
{
    Task<T?> GetByIdAsync(int id);
    Task<IEnumerable<T>> GetAllAsync();
    Task AddAsync(T entity);
    Task UpdateAsync(T entity);
    Task DeleteAsync(int id);
}

// Specific repository
public interface IOrderRepository : IRepository<Order>
{
    Task<IEnumerable<Order>> GetOrdersByCustomerIdAsync(int customerId);
    Task<Order?> GetOrderWithItemsAsync(int orderId);
}
```

---

## Layered Architecture

### Domain Layer

**Responsibilities**:
- Define business entities and value objects
- Enforce business rules and invariants
- Contain domain services
- Publish domain events

**Dependencies**: None (pure business logic)

```csharp
namespace OutlookSync.Domain.Entities;

public class Order : Entity<OrderId>
{
    private readonly List<OrderLine> _orderLines = new();
    
    public CustomerId CustomerId { get; private set; }
    public OrderStatus Status { get; private set; }
    public Money Total { get; private set; }
    public IReadOnlyCollection<OrderLine> OrderLines => _orderLines.AsReadOnly();
    
    public Order(CustomerId customerId)
    {
        CustomerId = customerId;
        Status = OrderStatus.Draft;
        Total = Money.Zero;
    }
    
    public void AddOrderLine(Product product, int quantity)
    {
        if (quantity <= 0)
            throw new DomainException("Quantity must be positive");
            
        if (Status != OrderStatus.Draft)
            throw new DomainException("Cannot modify submitted order");
            
        var orderLine = new OrderLine(product, quantity);
        _orderLines.Add(orderLine);
        Total = Total.Add(orderLine.Total);
        
        AddDomainEvent(new OrderLineAddedEvent(Id, product.Id, quantity));
    }
}
```

### Application Layer

**Responsibilities**:
- Implement use cases
- Orchestrate domain objects
- Handle transactions
- Coordinate infrastructure services

**Dependencies**: Domain Layer

```csharp
namespace OutlookSync.Application.Orders;

public class CreateOrderUseCase
{
    private readonly IOrderRepository _orderRepository;
    private readonly IProductRepository _productRepository;
    private readonly IUnitOfWork _unitOfWork;
    
    public CreateOrderUseCase(
        IOrderRepository orderRepository,
        IProductRepository productRepository,
        IUnitOfWork unitOfWork)
    {
        _orderRepository = orderRepository;
        _productRepository = productRepository;
        _unitOfWork = unitOfWork;
    }
    
    public async Task<Result<OrderId>> ExecuteAsync(CreateOrderRequest request)
    {
        // Validate request
        var validationResult = await ValidateRequestAsync(request);
        if (!validationResult.IsSuccess)
            return Result<OrderId>.Failure(validationResult.Error);
        
        // Create order
        var order = new Order(new CustomerId(request.CustomerId));
        
        foreach (var item in request.Items)
        {
            var product = await _productRepository.GetByIdAsync(item.ProductId);
            if (product == null)
                return Result<OrderId>.Failure($"Product {item.ProductId} not found");
                
            order.AddOrderLine(product, item.Quantity);
        }
        
        await _orderRepository.AddAsync(order);
        await _unitOfWork.SaveChangesAsync();
        
        return Result<OrderId>.Success(order.Id);
    }
}
```

### Infrastructure Layer

**Responsibilities**:
- Implement repository interfaces
- Handle database access
- Integrate with external services
- Implement cross-cutting concerns

**Dependencies**: Domain Layer, Application Layer

```csharp
namespace OutlookSync.Infrastructure.Persistence;

public class OrderRepository : IOrderRepository
{
    private readonly ApplicationDbContext _context;
    
    public OrderRepository(ApplicationDbContext context)
    {
        _context = context;
    }
    
    public async Task<Order?> GetByIdAsync(int id)
    {
        return await _context.Orders
            .Include(o => o.OrderLines)
            .FirstOrDefaultAsync(o => o.Id == id);
    }
    
    public async Task AddAsync(Order order)
    {
        await _context.Orders.AddAsync(order);
    }
}
```

### Presentation Layer

**Responsibilities**:
- Handle HTTP requests/responses
- Validate input
- Map DTOs to/from domain models
- Return appropriate status codes

**Dependencies**: Application Layer

```csharp
namespace OutlookSync.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly CreateOrderUseCase _createOrderUseCase;
    private readonly ILogger<OrdersController> _logger;
    
    public OrdersController(
        CreateOrderUseCase createOrderUseCase,
        ILogger<OrdersController> logger)
    {
        _createOrderUseCase = createOrderUseCase;
        _logger = logger;
    }
    
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<OrderResponse>> CreateOrder(CreateOrderRequest request)
    {
        _logger.LogInformation("Creating order for customer {CustomerId}", request.CustomerId);
        
        var result = await _createOrderUseCase.ExecuteAsync(request);
        
        if (!result.IsSuccess)
        {
            _logger.LogWarning("Failed to create order: {Error}", result.Error);
            return BadRequest(new { error = result.Error });
        }
        
        return CreatedAtAction(
            nameof(GetOrder),
            new { id = result.Value.Value },
            new OrderResponse { OrderId = result.Value.Value });
    }
}
```

---

## Dependency Management

### Dependency Injection Lifetimes

```csharp
// Startup.cs or Program.cs
services.AddTransient<IEmailService, EmailService>();        // New instance per request
services.AddScoped<IOrderRepository, OrderRepository>();     // One instance per HTTP request
services.AddSingleton<ICacheService, MemoryCacheService>(); // One instance for application lifetime
```

**Guidelines**:
- **Transient**: Stateless services, lightweight objects
- **Scoped**: DbContext, Unit of Work, per-request state
- **Singleton**: Configuration, caches, thread-safe services

### Avoiding Service Locator Anti-Pattern

```csharp
// Bad: Service Locator
public class OrderService
{
    public void ProcessOrder(int orderId)
    {
        var repository = ServiceLocator.GetService<IOrderRepository>();
        // ...
    }
}

// Good: Constructor Injection
public class OrderService
{
    private readonly IOrderRepository _repository;
    
    public OrderService(IOrderRepository repository)
    {
        _repository = repository;
    }
    
    public void ProcessOrder(int orderId)
    {
        // Use _repository
    }
}
```

---

## Asynchronous Programming

### Async Best Practices

1. **Use async/await consistently**
```csharp
// Good
public async Task<User> GetUserAsync(int id)
{
    return await _repository.GetByIdAsync(id);
}

// Bad: Blocking
public User GetUser(int id)
{
    return _repository.GetByIdAsync(id).Result;  // Can cause deadlocks
}
```

2. **ConfigureAwait in libraries**
```csharp
// In library code
public async Task<string> FetchDataAsync()
{
    var data = await _httpClient.GetStringAsync(url).ConfigureAwait(false);
    return data;
}
```

3. **Avoid async void (except event handlers)**
```csharp
// Good
public async Task ProcessDataAsync()
{
    await DoWorkAsync();
}

// Bad: Can't catch exceptions
public async void ProcessData()
{
    await DoWorkAsync();
}

// Exception: Event handlers
private async void Button_Click(object sender, EventArgs e)
{
    try
    {
        await ProcessDataAsync();
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error processing data");
    }
}
```

---

## Error Handling

### Result Pattern

```csharp
public record Result<T>
{
    public bool IsSuccess { get; init; }
    public T? Value { get; init; }
    public string? Error { get; init; }
    
    public static Result<T> Success(T value) => new() { IsSuccess = true, Value = value };
    public static Result<T> Failure(string error) => new() { IsSuccess = false, Error = error };
}

// Usage
public async Task<Result<Order>> CreateOrderAsync(CreateOrderRequest request)
{
    if (request.Items.Count == 0)
        return Result<Order>.Failure("Order must contain at least one item");
        
    var order = new Order(request.CustomerId);
    // ...
    return Result<Order>.Success(order);
}
```

### Exception Handling Strategy

```csharp
// Domain exceptions
public class DomainException : Exception
{
    public DomainException(string message) : base(message) { }
}

// Global exception handler
public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;
    
    public async Task TryHandleAsync(HttpContext context, Exception exception)
    {
        _logger.LogError(exception, "Unhandled exception occurred");
        
        var response = exception switch
        {
            DomainException => (StatusCodes.Status400BadRequest, exception.Message),
            NotFoundException => (StatusCodes.Status404NotFound, exception.Message),
            UnauthorizedException => (StatusCodes.Status401Unauthorized, "Unauthorized"),
            _ => (StatusCodes.Status500InternalServerError, "An error occurred")
        };
        
        context.Response.StatusCode = response.Item1;
        await context.Response.WriteAsJsonAsync(new { error = response.Item2 });
    }
}
```

---

## Configuration and Secrets

### Strongly-Typed Configuration

```csharp
// Configuration class
public class EmailSettings
{
    public string SmtpServer { get; set; } = string.Empty;
    public int Port { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

// Registration
services.Configure<EmailSettings>(configuration.GetSection("Email"));

// Usage
public class EmailService
{
    private readonly EmailSettings _settings;
    
    public EmailService(IOptions<EmailSettings> settings)
    {
        _settings = settings.Value;
    }
}
```

### Secrets Management

```json
// appsettings.json (no secrets)
{
  "Email": {
    "SmtpServer": "smtp.example.com",
    "Port": 587
  }
}

// User secrets (development)
dotnet user-secrets set "Email:Username" "user@example.com"
dotnet user-secrets set "Email:Password" "secretpassword"

// Azure Key Vault (production)
builder.Configuration.AddAzureKeyVault(
    new Uri($"https://{keyVaultName}.vault.azure.net/"),
    new DefaultAzureCredential());
```

---

## Performance Best Practices

### Database Performance

1. **Use AsNoTracking for read-only queries**
```csharp
var orders = await _context.Orders
    .AsNoTracking()
    .Where(o => o.CustomerId == customerId)
    .ToListAsync();
```

2. **Avoid N+1 queries**
```csharp
// Bad: N+1 query
var orders = await _context.Orders.ToListAsync();
foreach (var order in orders)
{
    var customer = await _context.Customers.FindAsync(order.CustomerId); // N queries
}

// Good: Eager loading
var orders = await _context.Orders
    .Include(o => o.Customer)
    .ToListAsync();
```

3. **Use pagination**
```csharp
public async Task<PagedResult<Order>> GetOrdersAsync(int page, int pageSize)
{
    var query = _context.Orders.AsNoTracking();
    
    var totalCount = await query.CountAsync();
    var items = await query
        .Skip((page - 1) * pageSize)
        .Take(pageSize)
        .ToListAsync();
        
    return new PagedResult<Order>(items, totalCount, page, pageSize);
}
```

### Caching

```csharp
public class CachedOrderRepository : IOrderRepository
{
    private readonly IOrderRepository _inner;
    private readonly IMemoryCache _cache;
    
    public async Task<Order?> GetByIdAsync(int id)
    {
        var cacheKey = $"order:{id}";
        
        if (_cache.TryGetValue(cacheKey, out Order? cachedOrder))
            return cachedOrder;
            
        var order = await _inner.GetByIdAsync(id);
        
        if (order != null)
        {
            _cache.Set(cacheKey, order, TimeSpan.FromMinutes(5));
        }
        
        return order;
    }
}
```

---

## Security Guidelines

### Input Validation

```csharp
public record CreateUserRequest
{
    [Required]
    [StringLength(100, MinimumLength = 2)]
    public string Name { get; init; } = string.Empty;
    
    [Required]
    [EmailAddress]
    public string Email { get; init; } = string.Empty;
    
    [Required]
    [StringLength(100, MinimumLength = 8)]
    [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]",
        ErrorMessage = "Password must contain uppercase, lowercase, digit, and special character")]
    public string Password { get; init; } = string.Empty;
}
```

### SQL Injection Prevention

```csharp
// Good: Parameterized query
var users = await _context.Users
    .Where(u => u.Email == email)
    .ToListAsync();

// Bad: String concatenation (vulnerable)
var query = $"SELECT * FROM Users WHERE Email = '{email}'";
```

### Authentication & Authorization

```csharp
[Authorize]
[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    [HttpGet("{id}")]
    [Authorize(Policy = "CanViewOrders")]
    public async Task<ActionResult<OrderDto>> GetOrder(int id)
    {
        // Only authorized users can access
    }
    
    [HttpPost]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<ActionResult> CreateOrder(CreateOrderRequest request)
    {
        // Only admins and managers can create orders
    }
}
```

---

## Summary

Key architectural principles:

1. **Separation of Concerns**: Keep layers independent
2. **Dependency Inversion**: Depend on abstractions
3. **Async Programming**: Use async/await consistently
4. **Error Handling**: Use Result pattern or proper exception handling
5. **Security**: Validate input, use parameterized queries, implement authentication
6. **Performance**: Use caching, pagination, and efficient queries
7. **Configuration**: Use strongly-typed configuration and secure secrets management

### Related Documentation

- [legend.md](./legend.md) - Main best practices guide
- [CSharpExpert.agent.md](./CSharpExpert.agent.md) - AI agent guidelines
- [epic-root-outlooksync.md](./epic-root-outlooksync.md) - Project root documentation

---

*Last Updated: 2026-02-10*
*Version: 1.0*
