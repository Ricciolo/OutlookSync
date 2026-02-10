# GitHub Copilot Instructions

This file configures GitHub Copilot to use project-specific agents and architectural instructions for better code generation and assistance.

## Repository Overview

OutlookSync is a .NET application focused on synchronizing Outlook data with external systems. The project follows Domain-Driven Design (DDD) principles and SOLID design patterns to ensure maintainability and scalability.

## Agent Instructions

This repository uses AI agent instructions defined in the `AGENTS.md` file located in the root directory. The agent file provides:
- C# and .NET best practices
- Modern language features guidance (C# 7.0+)
- Asynchronous programming patterns
- Dependency injection guidelines
- Architecture patterns (DDD, Clean Architecture, CQRS)
- Testing and security best practices

For detailed agent instructions, see [/AGENTS.md](/AGENTS.md).

## Path-Specific Instructions

### .NET Architecture Good Practices
- **Path**: `/instructions/dotnet-architecture-good-practices.instructions.md`
- **Purpose**: Comprehensive guide for DDD, SOLID principles, and .NET architecture patterns
- **Applies to**: All .NET code in the repository

## How to Work with This Repository

### Code Generation
When generating code, Copilot will:
- Follow SOLID principles as defined in our architecture instructions
- Implement DDD patterns (entities, value objects, aggregates, repositories)
- Use modern async/await patterns according to .NET best practices
- Structure dependency injection properly

### Architecture Guidelines
- Follow layered architecture: Domain, Application, Infrastructure, Presentation
- Keep domain logic in the domain layer, free from infrastructure concerns
- Use repository patterns for data access abstraction
- Implement proper aggregate boundaries

### Code Quality Standards
- Use meaningful names following Microsoft's C# conventions
- Keep methods small and focused (Single Responsibility Principle)
- Leverage nullable reference types to prevent null reference exceptions
- Write unit tests following the AAA pattern (Arrange, Act, Assert)

## Additional Resources

For more information on GitHub Copilot configuration:
- [GitHub Copilot Documentation](https://docs.github.com/en/copilot)
- [Repository Custom Instructions](https://docs.github.com/en/copilot/how-tos/configure-custom-instructions/add-repository-instructions)
- [Copilot Best Practices](https://github.blog/2023-06-20-how-to-write-better-prompts-for-github-copilot/)
