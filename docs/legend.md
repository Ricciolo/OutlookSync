# Legend – OutlookSync AI & Agent Framework

> Linked to **epic-root-outlooksync**.

## Overview

This document describes the AI & Agent Framework module of OutlookSync, covering architecture decisions, best practices, and guidance for contributors.

## Architecture Principles

| Principle | Application |
|---|---|
| **Single Responsibility** | Each service owns exactly one concern (AI completion, agent execution, privacy). |
| **Open/Closed** | Interfaces (`IAiService`, `IAgentExecutor`, `IPrivacyService`) allow extension without modifying existing code. |
| **Liskov Substitution** | All implementations are interchangeable through their interfaces. |
| **Interface Segregation** | Small, focused interfaces — no client is forced to depend on methods it does not use. |
| **Dependency Inversion** | Services depend on abstractions registered via `IServiceCollection`. |

## Module Map

```
src/OutlookSync.AI/
├── Configuration/      # Options classes (AiOptions, AgentOptions)
├── Extensions/         # DI registration (AddOutlookSyncAI)
├── Interfaces/         # Service contracts (IAiService, IAgentExecutor, IPrivacyService)
├── Models/             # DTOs (AiRequest, AiResponse, AgentTask, AgentResult, PrivacySettings)
└── Services/           # Implementations (AiService, AgentExecutor, PrivacyService)
```

## AI Microservice Modules

### IAiService

Sends prompts to an AI backend and returns completions. The service checks privacy settings before every call.

### IAgentExecutor

Executes `AgentTask` objects by delegating to `IAiService` and enforcing configurable timeouts.

### IPrivacyService

Manages user privacy preferences (personal data sharing, calendar data sharing, data retention). AI calls are blocked when data sharing is disabled.

## Agent Framework Workflows

1. A caller creates an `AgentTask` with a name, description, and optional payload.
2. `AgentExecutor` builds a prompt from the task and invokes `IAiService`.
3. The result is wrapped in an `AgentResult` with success/failure and optional error info.
4. Timeout is controlled by `AgentOptions.TaskTimeoutSeconds`.

## Privacy & Configuration

- **Privacy by default**: All sharing flags default to `false`.
- Configuration is provided through the standard `IOptions<T>` pattern.
- Register services with `services.AddOutlookSyncAI()`.

## Testing & Mocking

- All services are mockable via their interfaces.
- Tests use **xUnit** and **Moq**.
- Test project: `tests/OutlookSync.AI.Tests/`.

## Copilot AI Best Practices

1. Always program against interfaces, not concrete types.
2. Register services through `AddOutlookSyncAI` to ensure correct wiring.
3. Use `CancellationToken` on all async methods.
4. Keep models as immutable records or sealed classes with `required`/`init` properties.
5. Validate arguments with `ArgumentNullException.ThrowIfNull`.
6. Respect privacy settings — never bypass `IPrivacyService`.

## References

- `dotnet-architecture-good-practices.instructions.md`
- `CSharpExpert.agent.md`
- [epic-root-outlooksync](.)
