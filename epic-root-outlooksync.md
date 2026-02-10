# Epic Root: OutlookSync Project Documentation

Welcome to the OutlookSync project documentation hub. This document serves as the central reference point for all project documentation, guidelines, and best practices.

## Overview

OutlookSync is a .NET application project that follows modern software development practices including Domain-Driven Design (DDD), Clean Architecture, and SOLID principles. This documentation provides comprehensive guidelines for developers, AI assistants, and contributors.

## 📚 Documentation Structure

### Core Documentation

1. **[legend.md](./legend.md)** - ⭐ **START HERE**
   - Comprehensive .NET/C# AI Copilot Best Practices
   - DDD mandatory process (5 steps)
   - Testing conventions (AAA, naming, coverage >85%)
   - Quality checklist
   - Modern C# 14 features
   - SOLID principles overview

2. **[dotnet-architecture-good-practices.md](./dotnet-architecture-good-practices.md)**
   - Clean Architecture patterns
   - Layered architecture guidelines
   - CQRS and Repository patterns
   - Dependency injection best practices
   - Asynchronous programming
   - Performance and security guidelines

3. **[CSharpExpert.agent.md](./CSharpExpert.agent.md)**
   - SOLID principles deep dive with examples
   - Design patterns (Creational, Behavioral, Structural)
   - Code quality standards
   - Working with AI agents
   - Naming conventions and code organization

## 🎯 Quick Start Guide

### For Developers

1. **Read** [legend.md](./legend.md) to understand project best practices
2. **Review** the DDD 5-step process for domain modeling
3. **Follow** testing conventions: `MethodName_Condition_ExpectedResult()`
4. **Apply** SOLID principles in all code
5. **Maintain** >85% code coverage
6. **Complete** the quality checklist before commits

### For AI Copilot Users

1. **Reference** [legend.md](./legend.md) for context when generating code
2. **Request** adherence to SOLID principles from [CSharpExpert.agent.md](./CSharpExpert.agent.md)
3. **Use** architectural patterns from [dotnet-architecture-good-practices.md](./dotnet-architecture-good-practices.md)
4. **Validate** generated code against the quality checklist
5. **Write** tests following AAA pattern with proper naming

### For Code Reviewers

1. **Verify** code follows [legend.md](./legend.md) guidelines
2. **Check** SOLID principles are applied correctly
3. **Ensure** tests use `MethodName_Condition_ExpectedResult()` naming
4. **Validate** AAA pattern in all tests
5. **Confirm** code coverage >85%
6. **Review** quality checklist completion

## 🏗️ Architecture Overview

### Clean Architecture Layers

```
┌─────────────────────────────────────┐
│     Presentation Layer              │  ← Web API, UI, Controllers
│     (OutlookSync.Api)               │
├─────────────────────────────────────┤
│     Application Layer               │  ← Use Cases, Commands, Queries
│     (OutlookSync.Application)       │
├─────────────────────────────────────┤
│     Domain Layer                    │  ← Entities, Value Objects, Domain Logic
│     (OutlookSync.Domain)            │  ← NO dependencies
└─────────────────────────────────────┘
         ↓ depends on ↓
┌─────────────────────────────────────┐
│     Infrastructure Layer            │  ← Data Access, External Services
│     (OutlookSync.Infrastructure)    │
└─────────────────────────────────────┘
```

### Key Principles

1. **Domain Layer** has no external dependencies
2. **Application Layer** orchestrates domain objects and coordinates workflows
3. **Infrastructure Layer** implements interfaces defined in domain/application
4. **Presentation Layer** handles HTTP/UI concerns only

## 🧪 Testing Strategy

### Test Naming Convention

```csharp
// Pattern: MethodName_Condition_ExpectedResult
[Fact]
public void CreateOrder_ValidInput_ReturnsCreatedOrder()
{
    // Arrange
    var orderService = new OrderService();
    var request = new CreateOrderRequest { /* ... */ };
    
    // Act
    var result = orderService.CreateOrder(request);
    
    // Assert
    Assert.NotNull(result);
    Assert.Equal(OrderStatus.Created, result.Status);
}
```

### Coverage Requirements

| Component | Minimum Coverage |
|-----------|------------------|
| Domain Layer | 100% |
| Application Layer | 95% |
| Infrastructure Layer | 80% |
| Presentation Layer | 90% |
| **Overall** | **>85%** |

### Test Types

1. **Unit Tests** - Fast, isolated, mock dependencies
2. **Integration Tests** - Test component interactions
3. **E2E Tests** - Validate complete workflows

## 📋 Quality Standards

### Pre-Commit Checklist

- [ ] Code compiles without errors/warnings
- [ ] All tests pass
- [ ] Code coverage >85%
- [ ] No linting errors
- [ ] Static analysis clean (CodeQL, SonarQube)
- [ ] Code reviewed
- [ ] Documentation updated
- [ ] No breaking changes (or properly versioned)

### Code Review Focus Areas

1. **SOLID Compliance**
   - Single Responsibility Principle
   - Open/Closed Principle
   - Liskov Substitution Principle
   - Interface Segregation Principle
   - Dependency Inversion Principle

2. **Domain-Driven Design**
   - Entities properly modeled
   - Value objects are immutable
   - Aggregates enforce consistency
   - Business rules in domain layer
   - Domain events published

3. **Testing**
   - AAA pattern followed
   - Proper naming convention
   - Edge cases covered
   - Assertions are clear

## 🔧 Development Workflow

### DDD 5-Step Process

1. **Domain Analysis**
   - Understand business domain
   - Create ubiquitous language
   - Identify bounded contexts

2. **Strategic Design**
   - Define context boundaries
   - Establish relationships
   - Plan integration strategies

3. **Tactical Design**
   - Implement entities and value objects
   - Create aggregates
   - Design repositories and services

4. **Implementation & Testing**
   - Build domain model
   - Write comprehensive tests
   - Implement use cases

5. **Post-Implementation Review**
   - Validate against requirements
   - Refactor based on insights
   - Update documentation

### Commit Workflow

```bash
# 1. Format code
dotnet format

# 2. Build
dotnet build

# 3. Run tests
dotnet test

# 4. Check coverage
dotnet test --collect:"XPlat Code Coverage"

# 5. Commit
git add .
git commit -m "feat: implement user registration"
```

## 🚀 Modern C# Features (C# 14)

### Leverage These Features

- **Primary Constructors** - Simplified constructor syntax
- **Collection Expressions** - `[1, 2, 3]` syntax
- **Record Types** - Immutable data structures
- **Pattern Matching** - Advanced switch expressions
- **Nullable Reference Types** - Compile-time null safety
- **Init-only Properties** - Immutable properties
- **File-scoped Namespaces** - Reduced indentation

See [legend.md](./legend.md) for detailed examples.

## 📖 References

### External Resources

- [Microsoft Learn C# 14](https://learn.microsoft.com/en-us/dotnet/csharp/whats-new/csharp-14)
- [.NET Architecture Guides](https://dotnet.microsoft.com/learn/dotnet/architecture-guides)
- [Domain-Driven Design by Eric Evans](https://www.domainlanguage.com/ddd/)
- [Clean Architecture by Robert C. Martin](https://blog.cleancoder.com/uncle-bob/2012/08/13/the-clean-architecture.html)

### Internal Documentation

- [legend.md](./legend.md) - Main best practices guide
- [dotnet-architecture-good-practices.md](./dotnet-architecture-good-practices.md) - Architecture patterns
- [CSharpExpert.agent.md](./CSharpExpert.agent.md) - SOLID & design patterns

## 🤝 Contributing

### Guidelines

1. Follow all practices outlined in [legend.md](./legend.md)
2. Apply SOLID principles from [CSharpExpert.agent.md](./CSharpExpert.agent.md)
3. Use architectural patterns from [dotnet-architecture-good-practices.md](./dotnet-architecture-good-practices.md)
4. Write tests with proper naming and AAA pattern
5. Achieve >85% code coverage
6. Complete quality checklist
7. Get code review approval

### Code Style

- Follow .editorconfig settings
- Use file-scoped namespaces
- Apply XML documentation to public APIs
- Use meaningful variable names
- Keep methods small and focused

## 📞 Support

For questions or clarifications:

1. Review the documentation starting with [legend.md](./legend.md)
2. Check examples in [CSharpExpert.agent.md](./CSharpExpert.agent.md)
3. Consult architectural patterns in [dotnet-architecture-good-practices.md](./dotnet-architecture-good-practices.md)
4. Reach out to the team for domain-specific guidance

## 📝 Document Versions

| Document | Version | Last Updated |
|----------|---------|--------------|
| epic-root-outlooksync.md | 1.0 | 2026-02-10 |
| legend.md | 1.0 | 2026-02-10 |
| dotnet-architecture-good-practices.md | 1.0 | 2026-02-10 |
| CSharpExpert.agent.md | 1.0 | 2026-02-10 |

---

## Summary

This epic root provides a central hub for all OutlookSync project documentation. The key documents are:

1. **legend.md** - Your primary reference for best practices
2. **dotnet-architecture-good-practices.md** - Deep dive into architecture
3. **CSharpExpert.agent.md** - SOLID principles and design patterns

**Start with [legend.md](./legend.md)** and refer to other documents as needed.

---

*Version: 1.0*
*Last Updated: 2026-02-10*
*Project: OutlookSync*
