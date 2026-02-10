# OutlookSync - Legend and Architecture Guide

## Overview
OutlookSync is a cloud-native ASP.NET Core 10 application designed following modern .NET architecture best practices.

## Project Structure

```
OutlookSync/
├── src/                    # Source code
│   └── OutlookSync.Api/   # Main API application
├── test/                   # Test projects
│   └── OutlookSync.Api.Tests/
├── docs/                   # Documentation
├── .github/               # CI/CD workflows
├── Dockerfile            # Multi-stage Docker build
├── Directory.Build.props # Common build properties
└── global.json          # .NET SDK version pinning
```

## Architecture Principles

### 12-Factor App Principles
This application follows the [12-Factor App](https://12factor.net/) methodology:

1. **Codebase** - One codebase tracked in version control
2. **Dependencies** - Explicitly declare and isolate dependencies
3. **Config** - Store config in the environment
4. **Backing services** - Treat backing services as attached resources
5. **Build, release, run** - Strictly separate build and run stages
6. **Processes** - Execute as stateless processes
7. **Port binding** - Export services via port binding
8. **Concurrency** - Scale out via the process model
9. **Disposability** - Maximize robustness with fast startup and graceful shutdown
10. **Dev/prod parity** - Keep development and production as similar as possible
11. **Logs** - Treat logs as event streams
12. **Admin processes** - Run admin tasks as one-off processes

### Cloud-Native Configuration
- Environment-based configuration (no hardcoded values)
- Health checks for Kubernetes/Docker orchestration
- Structured logging with correlation IDs
- Graceful shutdown support
- Stateless design for horizontal scaling

### Code Quality Standards
- **Nullable Reference Types** - Enabled project-wide to reduce null reference exceptions
- **Implicit Usings** - Enabled for cleaner code
- **Treat Warnings as Errors** - Ensures code quality
- **Latest C# Language Version** - Leverage modern language features
- **.NET Analyzers** - Static code analysis enabled

## Docker Architecture

### Multi-Stage Build
The Dockerfile uses a multi-stage build process:

1. **Base Stage** - ASP.NET Core runtime
2. **Build Stage** - SDK with dependencies and compilation
3. **Test Stage** - Run unit and integration tests
4. **Publish Stage** - Create optimized release build
5. **Final Stage** - Minimal runtime image with published output

Benefits:
- Smaller final image size
- Build-time testing
- Cached layers for faster rebuilds
- Separation of build and runtime dependencies

## CI/CD Pipeline

GitHub Actions workflow provides:
- Automated builds on push/PR
- Unit and integration testing
- Docker image building
- Code quality checks
- Docker registry publishing

## Development Guidelines

### Getting Started
```bash
# Restore dependencies
dotnet restore

# Build the solution
dotnet build

# Run tests
dotnet test

# Run the application
dotnet run --project src/OutlookSync.Api

# Build Docker image
docker build -t outlooksync:latest .
```

### Adding New Projects
All projects inherit from `Directory.Build.props`, ensuring:
- Nullable reference types are enabled
- Warnings are treated as errors
- Latest C# language features are available
- Code analyzers are active

## References

- [Epic Root: OutlookSync](epic-root-outlooksync.md)
- [.NET Architecture Best Practices](https://learn.microsoft.com/en-us/dotnet/architecture/)
- [12-Factor App](https://12factor.net/)
- [ASP.NET Core Best Practices](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/best-practices)
- [Docker Best Practices](https://docs.docker.com/develop/dev-best-practices/)

## Key Technologies

- **.NET 10** - Latest .NET framework
- **ASP.NET Core** - Web framework
- **Docker** - Containerization
- **GitHub Actions** - CI/CD automation
- **xUnit** - Testing framework

## Support

For issues and questions, please refer to the main repository documentation or create an issue on GitHub.
