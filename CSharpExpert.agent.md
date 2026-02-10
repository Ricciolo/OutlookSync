# CSharpExpert Agent Guidelines

> **Return to**: [legend.md](./legend.md) | [epic-root-outlooksync.md](./epic-root-outlooksync.md)

This document provides guidelines for working with the CSharpExpert AI agent and outlines best practices for C# development that the agent follows.

## Table of Contents

1. [Agent Overview](#agent-overview)
2. [SOLID Principles Deep Dive](#solid-principles-deep-dive)
3. [Design Patterns](#design-patterns)
4. [Code Quality Standards](#code-quality-standards)
5. [Working with the Agent](#working-with-the-agent)

---

## Agent Overview

### CSharpExpert Capabilities

The CSharpExpert agent is designed to:

- Generate high-quality, idiomatic C# code
- Apply SOLID principles and design patterns
- Follow .NET best practices and conventions
- Create maintainable, testable code
- Provide architectural guidance
- Suggest refactoring opportunities

### Agent Expertise Areas

- **Language Features**: C# 12+, LINQ, async/await, pattern matching
- **Frameworks**: .NET 8+, ASP.NET Core, Entity Framework Core
- **Patterns**: DDD, CQRS, Repository, Factory, Strategy, etc.
- **Testing**: xUnit, NUnit, Moq, FluentAssertions
- **Architecture**: Clean Architecture, Microservices, Event-Driven

---

## SOLID Principles Deep Dive

### Single Responsibility Principle (SRP)

**Definition**: A class should have only one reason to change.

#### Examples

**Violation**:
```csharp
public class UserManager
{
    public void CreateUser(User user)
    {
        // Validate user
        if (string.IsNullOrEmpty(user.Email))
            throw new ArgumentException("Email is required");
            
        // Save to database
        using var connection = new SqlConnection(_connectionString);
        connection.Open();
        // ... SQL code
        
        // Send welcome email
        var smtp = new SmtpClient("smtp.example.com");
        smtp.Send(new MailMessage("noreply@example.com", user.Email, "Welcome", "Welcome!"));
        
        // Log activity
        File.AppendAllText("log.txt", $"User {user.Email} created at {DateTime.Now}");
    }
}
```

**Fixed**:
```csharp
public class UserService
{
    private readonly IUserRepository _userRepository;
    private readonly IEmailService _emailService;
    private readonly ILogger<UserService> _logger;
    private readonly IUserValidator _validator;
    
    public UserService(
        IUserRepository userRepository,
        IEmailService emailService,
        ILogger<UserService> logger,
        IUserValidator validator)
    {
        _userRepository = userRepository;
        _emailService = emailService;
        _logger = logger;
        _validator = validator;
    }
    
    public async Task CreateUserAsync(User user)
    {
        _validator.Validate(user);
        await _userRepository.AddAsync(user);
        await _emailService.SendWelcomeEmailAsync(user.Email);
        _logger.LogInformation("User {Email} created", user.Email);
    }
}
```

### Open/Closed Principle (OCP)

**Definition**: Software entities should be open for extension but closed for modification.

#### Examples

**Violation**:
```csharp
public class OrderProcessor
{
    public decimal CalculateDiscount(Order order, string customerType)
    {
        return customerType switch
        {
            "Regular" => order.Total * 0.05m,
            "Premium" => order.Total * 0.10m,
            "VIP" => order.Total * 0.20m,
            _ => 0m
        };
        // Adding new customer type requires modifying this method
    }
}
```

**Fixed**:
```csharp
public interface IDiscountStrategy
{
    decimal Calculate(Order order);
}

public class RegularCustomerDiscount : IDiscountStrategy
{
    public decimal Calculate(Order order) => order.Total * 0.05m;
}

public class PremiumCustomerDiscount : IDiscountStrategy
{
    public decimal Calculate(Order order) => order.Total * 0.10m;
}

public class VipCustomerDiscount : IDiscountStrategy
{
    public decimal Calculate(Order order) => order.Total * 0.20m;
}

public class OrderProcessor
{
    public decimal CalculateDiscount(Order order, IDiscountStrategy strategy)
    {
        return strategy.Calculate(order);
    }
}

// Adding new discount types doesn't require modifying OrderProcessor
```

### Liskov Substitution Principle (LSP)

**Definition**: Objects of a superclass should be replaceable with objects of a subclass without breaking functionality.

#### Examples

**Violation**:
```csharp
public class Rectangle
{
    public virtual int Width { get; set; }
    public virtual int Height { get; set; }
    
    public int CalculateArea() => Width * Height;
}

public class Square : Rectangle
{
    public override int Width
    {
        get => base.Width;
        set
        {
            base.Width = value;
            base.Height = value;  // Violates LSP - unexpected behavior
        }
    }
    
    public override int Height
    {
        get => base.Height;
        set
        {
            base.Width = value;
            base.Height = value;  // Violates LSP - unexpected behavior
        }
    }
}

// This breaks:
Rectangle rect = new Square();
rect.Width = 5;
rect.Height = 10;
Console.WriteLine(rect.CalculateArea());  // Expected: 50, Actual: 100
```

**Fixed**:
```csharp
public abstract class Shape
{
    public abstract int CalculateArea();
}

public class Rectangle : Shape
{
    public int Width { get; set; }
    public int Height { get; set; }
    
    public override int CalculateArea() => Width * Height;
}

public class Square : Shape
{
    public int Side { get; set; }
    
    public override int CalculateArea() => Side * Side;
}

// Now substitution works correctly:
Shape shape1 = new Rectangle { Width = 5, Height = 10 };
Shape shape2 = new Square { Side = 5 };
Console.WriteLine(shape1.CalculateArea());  // 50
Console.WriteLine(shape2.CalculateArea());  // 25
```

### Interface Segregation Principle (ISP)

**Definition**: Clients should not be forced to depend on interfaces they don't use.

#### Examples

**Violation**:
```csharp
public interface IWorker
{
    void Work();
    void Eat();
    void Sleep();
}

public class Human : IWorker
{
    public void Work() { /* ... */ }
    public void Eat() { /* ... */ }
    public void Sleep() { /* ... */ }
}

public class Robot : IWorker
{
    public void Work() { /* ... */ }
    public void Eat() { throw new NotImplementedException(); }  // Doesn't make sense
    public void Sleep() { throw new NotImplementedException(); }  // Doesn't make sense
}
```

**Fixed**:
```csharp
public interface IWorkable
{
    void Work();
}

public interface IEatable
{
    void Eat();
}

public interface ISleepable
{
    void Sleep();
}

public class Human : IWorkable, IEatable, ISleepable
{
    public void Work() { /* ... */ }
    public void Eat() { /* ... */ }
    public void Sleep() { /* ... */ }
}

public class Robot : IWorkable
{
    public void Work() { /* ... */ }
}
```

### Dependency Inversion Principle (DIP)

**Definition**: High-level modules should not depend on low-level modules. Both should depend on abstractions.

#### Examples

**Violation**:
```csharp
public class EmailNotificationService
{
    public void SendEmail(string to, string message)
    {
        // Email sending logic
    }
}

public class UserService
{
    private readonly EmailNotificationService _emailService = new();  // Tight coupling
    
    public void RegisterUser(User user)
    {
        // Registration logic
        _emailService.SendEmail(user.Email, "Welcome!");
    }
}
```

**Fixed**:
```csharp
public interface INotificationService
{
    Task SendAsync(string to, string message);
}

public class EmailNotificationService : INotificationService
{
    public async Task SendAsync(string to, string message)
    {
        // Email sending logic
    }
}

public class SmsNotificationService : INotificationService
{
    public async Task SendAsync(string to, string message)
    {
        // SMS sending logic
    }
}

public class UserService
{
    private readonly INotificationService _notificationService;
    
    public UserService(INotificationService notificationService)
    {
        _notificationService = notificationService;
    }
    
    public async Task RegisterUserAsync(User user)
    {
        // Registration logic
        await _notificationService.SendAsync(user.Email, "Welcome!");
    }
}
```

---

## Design Patterns

### Creational Patterns

#### Factory Pattern

```csharp
public interface IPaymentProcessor
{
    Task<PaymentResult> ProcessAsync(Payment payment);
}

public class PaymentProcessorFactory
{
    private readonly IServiceProvider _serviceProvider;
    
    public PaymentProcessorFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }
    
    public IPaymentProcessor Create(PaymentMethod method)
    {
        return method switch
        {
            PaymentMethod.CreditCard => _serviceProvider.GetRequiredService<CreditCardProcessor>(),
            PaymentMethod.PayPal => _serviceProvider.GetRequiredService<PayPalProcessor>(),
            PaymentMethod.BankTransfer => _serviceProvider.GetRequiredService<BankTransferProcessor>(),
            _ => throw new ArgumentException($"Unknown payment method: {method}")
        };
    }
}
```

#### Builder Pattern

```csharp
public class EmailMessageBuilder
{
    private string _to = string.Empty;
    private string _subject = string.Empty;
    private string _body = string.Empty;
    private readonly List<string> _attachments = new();
    
    public EmailMessageBuilder To(string to)
    {
        _to = to;
        return this;
    }
    
    public EmailMessageBuilder WithSubject(string subject)
    {
        _subject = subject;
        return this;
    }
    
    public EmailMessageBuilder WithBody(string body)
    {
        _body = body;
        return this;
    }
    
    public EmailMessageBuilder AddAttachment(string path)
    {
        _attachments.Add(path);
        return this;
    }
    
    public EmailMessage Build()
    {
        if (string.IsNullOrEmpty(_to))
            throw new InvalidOperationException("Recipient is required");
            
        return new EmailMessage
        {
            To = _to,
            Subject = _subject,
            Body = _body,
            Attachments = _attachments.ToList()
        };
    }
}

// Usage
var email = new EmailMessageBuilder()
    .To("user@example.com")
    .WithSubject("Welcome")
    .WithBody("Welcome to our service!")
    .AddAttachment("welcome.pdf")
    .Build();
```

### Behavioral Patterns

#### Strategy Pattern

```csharp
public interface ISortStrategy<T>
{
    IEnumerable<T> Sort(IEnumerable<T> items);
}

public class QuickSortStrategy<T> : ISortStrategy<T> where T : IComparable<T>
{
    public IEnumerable<T> Sort(IEnumerable<T> items)
    {
        // QuickSort implementation
        return items.OrderBy(x => x);
    }
}

public class BubbleSortStrategy<T> : ISortStrategy<T> where T : IComparable<T>
{
    public IEnumerable<T> Sort(IEnumerable<T> items)
    {
        // BubbleSort implementation
        var list = items.ToList();
        // ... bubble sort logic
        return list;
    }
}

public class DataProcessor<T> where T : IComparable<T>
{
    private readonly ISortStrategy<T> _sortStrategy;
    
    public DataProcessor(ISortStrategy<T> sortStrategy)
    {
        _sortStrategy = sortStrategy;
    }
    
    public IEnumerable<T> ProcessData(IEnumerable<T> data)
    {
        return _sortStrategy.Sort(data);
    }
}
```

#### Observer Pattern (using Events)

```csharp
public class Order
{
    public event EventHandler<OrderStatusChangedEventArgs>? StatusChanged;
    
    private OrderStatus _status;
    
    public OrderStatus Status
    {
        get => _status;
        private set
        {
            if (_status != value)
            {
                var oldStatus = _status;
                _status = value;
                OnStatusChanged(new OrderStatusChangedEventArgs(oldStatus, value));
            }
        }
    }
    
    protected virtual void OnStatusChanged(OrderStatusChangedEventArgs e)
    {
        StatusChanged?.Invoke(this, e);
    }
    
    public void Submit() => Status = OrderStatus.Submitted;
}

public class OrderStatusChangedEventArgs : EventArgs
{
    public OrderStatus OldStatus { get; }
    public OrderStatus NewStatus { get; }
    
    public OrderStatusChangedEventArgs(OrderStatus oldStatus, OrderStatus newStatus)
    {
        OldStatus = oldStatus;
        NewStatus = newStatus;
    }
}

// Usage
var order = new Order();
order.StatusChanged += (sender, e) =>
{
    Console.WriteLine($"Order status changed from {e.OldStatus} to {e.NewStatus}");
};
```

### Structural Patterns

#### Decorator Pattern

```csharp
public interface IDataRepository
{
    Task<Data> GetAsync(int id);
}

public class DataRepository : IDataRepository
{
    public async Task<Data> GetAsync(int id)
    {
        // Actual data retrieval
        await Task.Delay(100);
        return new Data { Id = id };
    }
}

public class CachedDataRepository : IDataRepository
{
    private readonly IDataRepository _inner;
    private readonly IMemoryCache _cache;
    
    public CachedDataRepository(IDataRepository inner, IMemoryCache cache)
    {
        _inner = inner;
        _cache = cache;
    }
    
    public async Task<Data> GetAsync(int id)
    {
        var cacheKey = $"data:{id}";
        
        if (_cache.TryGetValue(cacheKey, out Data? cachedData))
            return cachedData!;
            
        var data = await _inner.GetAsync(id);
        _cache.Set(cacheKey, data, TimeSpan.FromMinutes(5));
        
        return data;
    }
}

public class LoggingDataRepository : IDataRepository
{
    private readonly IDataRepository _inner;
    private readonly ILogger<LoggingDataRepository> _logger;
    
    public LoggingDataRepository(IDataRepository inner, ILogger<LoggingDataRepository> logger)
    {
        _inner = inner;
        _logger = logger;
    }
    
    public async Task<Data> GetAsync(int id)
    {
        _logger.LogInformation("Getting data with id {Id}", id);
        var data = await _inner.GetAsync(id);
        _logger.LogInformation("Retrieved data with id {Id}", data.Id);
        return data;
    }
}
```

---

## Code Quality Standards

### Naming Conventions

```csharp
// Interfaces: IPascalCase
public interface IUserRepository { }

// Classes: PascalCase
public class UserService { }

// Methods: PascalCase
public void ProcessOrder() { }

// Private fields: _camelCase
private readonly ILogger<UserService> _logger;

// Static fields: s_camelCase
private static readonly HttpClient s_httpClient = new();

// Constants: PascalCase
public const int MaxRetryCount = 3;

// Parameters and local variables: camelCase
public void CreateUser(string userName, int userId)
{
    var newUser = new User();
}

// Properties: PascalCase
public string UserName { get; set; }

// Async methods: End with Async
public async Task<User> GetUserAsync(int id) { }
```

### Code Organization

```csharp
namespace OutlookSync.Domain.Entities;

// 1. Usings (sorted, System first)
using System;
using System.Collections.Generic;
using System.Linq;
using OutlookSync.Domain.Common;

// 2. Class/Interface declaration
public class Order : Entity<OrderId>
{
    // 3. Constants
    private const int MaxOrderLines = 100;
    
    // 4. Static fields
    private static readonly Money s_minimumOrderValue = new(10.00m, "USD");
    
    // 5. Private fields
    private readonly List<OrderLine> _orderLines = new();
    
    // 6. Constructors
    public Order(CustomerId customerId)
    {
        CustomerId = customerId;
        Status = OrderStatus.Draft;
    }
    
    // 7. Public properties
    public CustomerId CustomerId { get; private set; }
    public OrderStatus Status { get; private set; }
    public IReadOnlyCollection<OrderLine> OrderLines => _orderLines.AsReadOnly();
    
    // 8. Public methods
    public void AddOrderLine(Product product, int quantity)
    {
        ValidateOrderLine(product, quantity);
        var orderLine = new OrderLine(product, quantity);
        _orderLines.Add(orderLine);
    }
    
    // 9. Private methods
    private void ValidateOrderLine(Product product, int quantity)
    {
        if (quantity <= 0)
            throw new DomainException("Quantity must be positive");
    }
}
```

### XML Documentation

```csharp
/// <summary>
/// Represents a service for managing user accounts.
/// </summary>
public class UserService
{
    /// <summary>
    /// Creates a new user account.
    /// </summary>
    /// <param name="request">The user creation request containing user details.</param>
    /// <returns>The created user's unique identifier.</returns>
    /// <exception cref="ArgumentNullException">Thrown when request is null.</exception>
    /// <exception cref="ValidationException">Thrown when user data is invalid.</exception>
    public async Task<UserId> CreateUserAsync(CreateUserRequest request)
    {
        // Implementation
    }
}
```

---

## Working with the Agent

### How to Request Code from CSharpExpert

#### 1. Be Specific About Requirements

**Poor Request**:
> "Create a user service"

**Good Request**:
> "Create a UserService class that implements the Repository pattern with dependency injection. It should handle user CRUD operations, use async/await for database calls, and follow SOLID principles. Use Entity Framework Core as the ORM."

#### 2. Specify Architecture and Patterns

**Example**:
> "Implement a CreateOrderUseCase following Clean Architecture principles. The use case should coordinate between OrderRepository and ProductRepository, validate business rules, handle transactions with Unit of Work pattern, and return a Result<OrderId>."

#### 3. Request Specific Design Patterns

**Example**:
> "Create a payment processing system using the Strategy pattern. Support three payment methods: CreditCard, PayPal, and BankTransfer. Use Factory pattern to create the appropriate processor based on payment method."

#### 4. Ask for Refactoring

**Example**:
> "Refactor this UserManager class to follow the Single Responsibility Principle. Extract validation, email sending, and logging into separate services."

#### 5. Request Tests

**Example**:
> "Generate unit tests for the OrderService.CreateOrder method following AAA pattern and using the naming convention MethodName_Condition_ExpectedResult. Include tests for successful creation, null input, and invalid order items."

### Agent Response Format

When you request code, the agent will typically provide:

1. **Context and Explanation**
   - Brief description of the solution
   - Design decisions and pattern choices
   - Trade-offs and considerations

2. **Code Implementation**
   - Complete, compilable code
   - Proper error handling
   - XML documentation for public APIs

3. **Usage Examples**
   - How to instantiate and use the code
   - Dependency injection registration
   - Common scenarios

4. **Testing Recommendations**
   - Suggested test cases
   - Testing approach
   - Mock requirements

### Iterative Refinement

If the initial response doesn't meet your needs:

**Refine the Request**:
> "The solution is good, but please use the Mediator pattern instead of direct repository calls, and add domain events when the order status changes."

**Ask for Alternatives**:
> "Can you show an alternative implementation using the Specification pattern for filtering?"

**Request Optimization**:
> "This works, but it's making N+1 queries. Can you optimize it with eager loading?"

---

## Summary

Working effectively with the CSharpExpert agent:

1. **Be Specific**: Clearly state requirements, patterns, and architecture
2. **Follow SOLID**: Request adherence to SOLID principles
3. **Use Patterns**: Leverage design patterns for better solutions
4. **Quality Standards**: Expect proper naming, organization, and documentation
5. **Iterate**: Refine requests based on initial responses

### Quick Reference

- **SOLID**: SRP, OCP, LSP, ISP, DIP
- **Patterns**: Factory, Builder, Strategy, Observer, Decorator, Repository
- **Quality**: Naming conventions, code organization, XML docs
- **Testing**: AAA pattern, MethodName_Condition_ExpectedResult

### Related Documentation

- [legend.md](./legend.md) - Main best practices guide
- [dotnet-architecture-good-practices.md](./dotnet-architecture-good-practices.md) - Architecture guidelines
- [epic-root-outlooksync.md](./epic-root-outlooksync.md) - Project root documentation

---

*Last Updated: 2026-02-10*
*Version: 1.0*
