# .NET Architecture Good Practices

## Overview
This document outlines architectural best practices for building maintainable, scalable, and robust .NET applications using Domain-Driven Design (DDD), SOLID principles, and modern .NET patterns.

## SOLID Principles

### Single Responsibility Principle (SRP)
**Definition**: A class should have only one reason to change.

**Best Practices**:
- Each class should have a single, well-defined purpose
- Separate concerns into different classes (e.g., validation, persistence, business logic)
- Keep classes focused and cohesive

**Example**:
```csharp
// Bad: Multiple responsibilities
public class UserService
{
    public void CreateUser(User user) { }
    public void SendEmail(string email, string message) { }
    public void LogActivity(string message) { }
}

// Good: Single responsibility
public class UserService
{
    private readonly IEmailService _emailService;
    private readonly ILogger _logger;
    
    public void CreateUser(User user) { /* ... */ }
}

public class EmailService : IEmailService
{
    public void SendEmail(string email, string message) { /* ... */ }
}
```

### Open/Closed Principle (OCP)
**Definition**: Software entities should be open for extension but closed for modification.

**Best Practices**:
- Use interfaces and abstract classes for extensibility
- Leverage inheritance and polymorphism
- Use dependency injection to swap implementations

**Example**:
```csharp
// Extensible design
public interface IPaymentProcessor
{
    void ProcessPayment(decimal amount);
}

public class CreditCardProcessor : IPaymentProcessor
{
    public void ProcessPayment(decimal amount) { /* ... */ }
}

public class PayPalProcessor : IPaymentProcessor
{
    public void ProcessPayment(decimal amount) { /* ... */ }
}
```

### Liskov Substitution Principle (LSP)
**Definition**: Derived classes must be substitutable for their base classes.

**Best Practices**:
- Ensure derived classes don't break base class contracts
- Maintain behavioral consistency
- Avoid strengthening preconditions or weakening postconditions

### Interface Segregation Principle (ISP)
**Definition**: Clients should not be forced to depend on interfaces they don't use.

**Best Practices**:
- Create small, focused interfaces
- Split large interfaces into smaller, specific ones
- Follow "role interfaces" pattern

**Example**:
```csharp
// Bad: Fat interface
public interface IWorker
{
    void Work();
    void Eat();
    void Sleep();
}

// Good: Segregated interfaces
public interface IWorkable
{
    void Work();
}

public interface IFeedable
{
    void Eat();
}
```

### Dependency Inversion Principle (DIP)
**Definition**: High-level modules should not depend on low-level modules. Both should depend on abstractions.

**Best Practices**:
- Depend on interfaces, not concrete implementations
- Use dependency injection containers
- Invert control flow through abstractions

**Example**:
```csharp
// Good: Depend on abstraction
public class OrderService
{
    private readonly IRepository<Order> _orderRepository;
    
    public OrderService(IRepository<Order> orderRepository)
    {
        _orderRepository = orderRepository;
    }
}
```

## Domain-Driven Design (DDD)

### Strategic Design

#### Bounded Contexts
- Define clear boundaries between different parts of your domain
- Each bounded context has its own ubiquitous language
- Use context mapping to manage relationships between contexts

#### Ubiquitous Language
- Use domain terminology consistently in code and communication
- Collaborate with domain experts to refine language
- Reflect domain concepts directly in code

### Tactical Design

#### Entities
- Objects with distinct identity that persists over time
- Identity is more important than attributes

```csharp
public class Order : Entity
{
    public OrderId Id { get; private set; }
    public CustomerId CustomerId { get; private set; }
    public OrderStatus Status { get; private set; }
    private readonly List<OrderItem> _items = new();
    
    public IReadOnlyCollection<OrderItem> Items => _items.AsReadOnly();
}
```

#### Value Objects
- Objects defined by their attributes, not identity
- Immutable by design
- Compared by value equality

```csharp
public record Address(string Street, string City, string PostalCode, string Country);

public record Money(decimal Amount, string Currency)
{
    public Money Add(Money other)
    {
        if (Currency != other.Currency)
            throw new InvalidOperationException("Cannot add money with different currencies");
        return this with { Amount = Amount + other.Amount };
    }
}
```

#### Aggregates
- Cluster of entities and value objects treated as a single unit
- One entity is the aggregate root
- Enforce invariants and business rules

```csharp
public class Order // Aggregate Root
{
    private readonly List<OrderItem> _items = new();
    
    public void AddItem(Product product, int quantity)
    {
        // Enforce business rules
        if (quantity <= 0)
            throw new DomainException("Quantity must be positive");
            
        var item = new OrderItem(product, quantity);
        _items.Add(item);
    }
}
```

#### Domain Services
- Operations that don't naturally fit in an entity or value object
- Stateless operations involving multiple aggregates

```csharp
public class PricingService
{
    public Money CalculateOrderTotal(Order order, Customer customer)
    {
        // Complex pricing logic involving order and customer
    }
}
```

#### Repositories
- Abstraction for data access
- Work with aggregates, not individual entities
- Repository per aggregate root

```csharp
public interface IOrderRepository
{
    Task<Order> GetByIdAsync(OrderId id);
    Task AddAsync(Order order);
    Task UpdateAsync(Order order);
    Task DeleteAsync(OrderId id);
}
```

#### Domain Events
- Capture important domain occurrences
- Enable loose coupling between aggregates
- Support eventual consistency

```csharp
public record OrderPlacedEvent(OrderId OrderId, CustomerId CustomerId, DateTime PlacedAt);
```

## Layered Architecture

### Domain Layer
- **Purpose**: Core business logic and domain models
- **Dependencies**: None (pure domain logic)
- **Contains**: Entities, value objects, aggregates, domain services, domain events

### Application Layer
- **Purpose**: Use cases and application workflows
- **Dependencies**: Domain layer
- **Contains**: Application services, DTOs, commands, queries, interfaces for infrastructure

### Infrastructure Layer
- **Purpose**: Technical implementations
- **Dependencies**: Application and Domain layers
- **Contains**: Database access, external services, file system, logging

### Presentation Layer
- **Purpose**: User interface and API endpoints
- **Dependencies**: Application layer
- **Contains**: Controllers, views, API models, validation

## Best Practices

### Project Structure
```
Solution/
├── src/
│   ├── Domain/
│   │   ├── Entities/
│   │   ├── ValueObjects/
│   │   ├── Aggregates/
│   │   ├── Events/
│   │   └── Interfaces/
│   ├── Application/
│   │   ├── Services/
│   │   ├── DTOs/
│   │   ├── Commands/
│   │   ├── Queries/
│   │   └── Interfaces/
│   ├── Infrastructure/
│   │   ├── Persistence/
│   │   ├── Services/
│   │   └── Configuration/
│   └── Presentation/
│       ├── API/
│       └── Web/
└── tests/
    ├── Domain.Tests/
    ├── Application.Tests/
    └── Integration.Tests/
```

### Dependency Management
- Use dependency injection for loose coupling
- Register services in the appropriate layer
- Use interface-based programming
- Avoid circular dependencies

### Data Access Patterns
- **Repository Pattern**: Abstract data access behind repositories
- **Unit of Work**: Manage transactions across multiple repositories
- **Specification Pattern**: Encapsulate query logic
- **CQRS**: Separate read and write models when appropriate

### Error Handling
- Use domain-specific exceptions for business rule violations
- Use result objects for operation outcomes
- Log errors with context
- Don't leak infrastructure exceptions to the domain layer

```csharp
public class Result<T>
{
    public bool IsSuccess { get; }
    public T Value { get; }
    public string Error { get; }
    
    public static Result<T> Success(T value) => new(true, value, null);
    public static Result<T> Failure(string error) => new(false, default, error);
}
```

### Validation
- Validate at the boundary (controllers, commands)
- Use FluentValidation for complex validation rules
- Enforce invariants in domain entities
- Validate business rules in domain services

### Testing Strategy
- **Unit Tests**: Test domain logic in isolation
- **Integration Tests**: Test infrastructure components
- **Application Tests**: Test use cases end-to-end
- Use test doubles (mocks, stubs) appropriately

### Configuration
- Use strongly-typed configuration classes
- Store settings in appsettings.json
- Use environment variables for secrets
- Validate configuration on startup

### Logging and Monitoring
- Use structured logging (Serilog, NLog)
- Log at appropriate levels (Trace, Debug, Info, Warning, Error, Critical)
- Include correlation IDs for request tracking
- Monitor application health and performance

## Anti-Patterns to Avoid

### Anemic Domain Model
- Domain objects with only getters/setters and no behavior
- Business logic in services instead of domain objects
- **Solution**: Enrich domain models with behavior

### God Object
- One class that knows or does too much
- Violates SRP
- **Solution**: Break into smaller, focused classes

### Service Locator
- Directly requesting dependencies from a container
- Hides dependencies
- **Solution**: Use constructor injection

### N+1 Query Problem
- Loading collections with multiple database queries
- Performance issues
- **Solution**: Use eager loading or projections

### Leaky Abstractions
- Implementation details leak through interfaces
- Tight coupling
- **Solution**: Design proper abstractions

## Performance Considerations

- Use async/await for I/O operations
- Implement caching strategically
- Use pagination for large data sets
- Optimize database queries (projections, indexes)
- Consider read replicas for read-heavy workloads
- Use connection pooling
- Profile before optimizing

## Security Best Practices

- Implement authentication and authorization
- Validate and sanitize all inputs
- Use parameterized queries
- Store secrets securely (Azure Key Vault, AWS Secrets Manager)
- Implement rate limiting
- Use HTTPS everywhere
- Keep dependencies updated
- Follow OWASP guidelines

## References

- [Domain-Driven Design by Eric Evans](https://www.domainlanguage.com/ddd/)
- [Clean Architecture by Robert C. Martin](https://blog.cleancoder.com/uncle-bob/2012/08/13/the-clean-architecture.html)
- [Microsoft .NET Architecture Guides](https://dotnet.microsoft.com/learn/dotnet/architecture-guides)
- [SOLID Principles](https://en.wikipedia.org/wiki/SOLID)
- [Martin Fowler's Patterns of Enterprise Application Architecture](https://martinfowler.com/books/eaa.html)
