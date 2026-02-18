# OutlookSync

A modern, cloud-native ASP.NET Core 10 application built with Docker support and CI/CD automation.

## 🚀 Features

- **ASP.NET Core 10** - Latest .NET framework
- **Docker Multi-Stage Build** - Optimized containerization
- **Cloud-Native** - 12-factor app principles, health checks, environment-based configuration
- **CI/CD Ready** - GitHub Actions workflow for automated builds and tests
- **Code Quality** - Nullable reference types, code analyzers, consistent styling

## 📋 Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) (10.0.102 or later)
- [Docker](https://www.docker.com/get-started) (optional, for containerization)

## 🏗️ Project Structure

```
OutlookSync/
├── src/                    # Source code
│   ├── OutlookSync.Application/  # Application layer (use cases)
│   ├── OutlookSync.Domain/       # Domain layer (entities, aggregates)
│   ├── OutlookSync.Infrastructure/ # Infrastructure (data access, services)
│   └── OutlookSync.Web/          # Blazor web application
├── test/                   # Test projects
│   ├── OutlookSync.Application.Tests/
│   ├── OutlookSync.Domain.Tests/
│   └── OutlookSync.Infrastructure.Tests/
├── docs/                   # Documentation
├── .github/workflows/     # CI/CD pipelines
├── Dockerfile            # Multi-stage Docker build
└── Directory.Build.props # Common build properties
```

## 🛠️ Getting Started

### Local Development

```bash
# Clone the repository
git clone https://github.com/Ricciolo/OutlookSync.git
cd OutlookSync

# Restore dependencies
dotnet restore

# Build the solution
dotnet build

# Run tests
dotnet test

# Run the application
dotnet run --project src/OutlookSync.Web
```

The web application will be available at:
- HTTP: http://localhost:5000
- HTTPS: https://localhost:5001

### Health Checks

- Liveness: http://localhost:5000/health/live
- Readiness: http://localhost:5000/health/ready

### Docker

```bash
# Build the Docker image
docker build -t outlooksync:latest .

# Run the container
docker run -p 8080:8080 outlooksync:latest

# Access the application
curl http://localhost:8080/health/live
```

## 🧪 Testing

```bash
# Run all tests
dotnet test

# Run tests with coverage
dotnet test --collect:"XPlat Code Coverage"
```

## 📖 Documentation

- [Architecture Overview](docs/architecture.md)
- [Legend and Project Guide](legend.md)
- [Epic Root Documentation](epic-root-outlooksync.md)

## 🔧 Configuration

The application uses environment-based configuration following 12-factor app principles:

```bash
# Set environment
export ASPNETCORE_ENVIRONMENT=Production

# Set custom port
export PORT=8080

# Run with environment variables
dotnet run --project src/OutlookSync.Web
```

## 🏗️ Architecture

This project follows:
- **12-Factor App** methodology
- **Cloud-Native** best practices
- **Microservices-ready** architecture
- **Clean Architecture** principles

See [Architecture Documentation](docs/architecture.md) for details.

## 🤝 Contributing

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

## 📝 License

This project is licensed under the MIT License - see the LICENSE file for details.

## 📧 Support

For questions and support:
- Open an issue on GitHub
- Check the [documentation](docs/)
- Review [epic-root-outlooksync.md](epic-root-outlooksync.md)

## 🙏 Acknowledgments

- Built with [ASP.NET Core](https://asp.net/)
- Follows [Microsoft .NET Architecture Guides](https://learn.microsoft.com/en-us/dotnet/architecture/)
- Implements [12-Factor App](https://12factor.net/) methodology
