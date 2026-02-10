# GitHub Copilot Instructions

This file configures GitHub Copilot to use project-specific agents and architectural instructions for better code generation and assistance.

## Agent Files

### C# Expert Agent
- **Path**: `/agents/CSharpExpert.agent.md`
- **Purpose**: Provides C# best practices, modern language features, and .NET ecosystem guidance
- **Use When**: Writing or reviewing C# code, implementing .NET features, or making architectural decisions

## Instruction Files

### .NET Architecture Good Practices
- **Path**: `/instructions/dotnet-architecture-good-practices.instructions.md`
- **Purpose**: Comprehensive guide for DDD, SOLID principles, and .NET architecture patterns
- **Use When**: Designing application architecture, implementing domain models, or structuring projects

## How GitHub Copilot Uses These Files

When you work with code in this repository, GitHub Copilot automatically:
1. References the agent files to understand project-specific coding standards
2. Applies architectural patterns from instruction files
3. Suggests code that aligns with documented best practices
4. Provides context-aware completions based on DDD and SOLID principles

## Best Practices for Using Copilot in This Project

### Code Generation
- Copilot will suggest code following SOLID principles
- Entity and value object implementations will align with DDD patterns
- Async/await patterns will follow .NET best practices
- Dependency injection will be properly structured

### Architecture Decisions
- Copilot understands the layered architecture (Domain, Application, Infrastructure, Presentation)
- Suggests proper placement of classes in appropriate layers
- Recommends repository patterns and aggregate boundaries
- Provides guidance on domain events and domain services

### Code Reviews
- Use Copilot to check adherence to coding conventions
- Validate that SOLID principles are followed
- Ensure domain logic stays in the domain layer
- Verify proper error handling and validation patterns

## Customization

To enhance Copilot's understanding of this project:
1. Add more specific agent files for specialized areas (e.g., testing, security)
2. Update instruction files with project-specific patterns
3. Document domain-specific terminology in the ubiquitous language
4. Include examples of preferred implementations

## Additional Resources

For more information on GitHub Copilot configuration:
- [GitHub Copilot Documentation](https://docs.github.com/en/copilot)
- [Copilot for Business](https://docs.github.com/en/copilot/overview-of-github-copilot/about-github-copilot-business)
- [Copilot Best Practices](https://github.blog/2023-06-20-how-to-write-better-prompts-for-github-copilot/)
