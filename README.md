# OutlookSync

OutlookSync is a self-hosted Blazor application that synchronises your Microsoft Exchange / Outlook calendars with external calendar providers. It runs as a lightweight Docker container, stores its data in a local SQLite database, and optionally enforces HTTP Basic Authentication when credentials are configured.

## ✨ Features

- **Exchange Calendar Sync** — Automatically keep calendars in sync with Microsoft Exchange / Outlook
- **Blazor Server UI** — Modern, real-time web interface powered by ASP.NET Core 10 Blazor
- **SQLite Persistence** — Zero-dependency local database; mount a volume and you're done
- **Basic Authentication** — Protect the UI with a username and password configured via environment variables
- **Health Checks** — `/health/live` and `/health/ready` endpoints for container orchestration
- **Docker-first** — Multi-stage, production-ready image published to GitHub Container Registry
- **CI/CD** — GitHub Actions pipeline builds, tests, and pushes the image automatically

## 📋 Prerequisites

- [Docker](https://www.docker.com/get-started) — to run the container
- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) — only for local development

## 🐳 Quick Start with Docker

### Pull from GitHub Container Registry

```bash
docker pull ghcr.io/ricciolo/outlooksync:latest
```

### Run the container

```bash
docker run -d \
  --name outlooksync \
  -p 8080:8080 \
  -e BasicAuth__Username=admin \
  -e BasicAuth__Password=changeme \
  -v outlooksync-db:/app/db \
  ghcr.io/ricciolo/outlooksync:latest
```

Open http://localhost:8080 in your browser and log in with the credentials above.

### Docker Compose

```yaml
services:
  outlooksync:
    image: ghcr.io/ricciolo/outlooksync:latest
    container_name: outlooksync
    restart: unless-stopped
    ports:
      - "8080:8080"
    environment:
      BasicAuth__Username: admin
      BasicAuth__Password: changeme
    volumes:
      - outlooksync-db:/app/db

volumes:
  outlooksync-db:
```

```bash
docker compose up -d
```

## ⚙️ Configuration

All settings are controlled through environment variables. In Docker, use double-underscore (`__`) as the section separator.

| Environment Variable | Default | Description |
|---|---|---|
| `BasicAuth__Username` | *(empty)* | Username for Basic Authentication. Leave empty to disable authentication. |
| `BasicAuth__Password` | *(empty)* | Password for Basic Authentication. |
| `ConnectionStrings__DefaultConnection` | `Data Source=/app/db/outlooksync.db` | SQLite connection string. |
| `ASPNETCORE_URLS` | `http://+:8080` | Listening address and port. Change to use a different port. |
| `ASPNETCORE_ENVIRONMENT` | `Production` | ASP.NET Core environment name. |

### Changing the port

```bash
docker run -d \
  --name outlooksync \
  -p 9000:9000 \
  -e ASPNETCORE_URLS=http://+:9000 \
  -e BasicAuth__Username=admin \
  -e BasicAuth__Password=changeme \
  -v outlooksync-db:/app/db \
  ghcr.io/ricciolo/outlooksync:latest
```

### Database persistence

The SQLite database file is stored at `/app/db/outlooksync.db` inside the container. Mount a named volume or a host directory to persist data across container restarts:

```bash
# Named volume (recommended)
-v outlooksync-db:/app/db

# Host directory
-v /path/on/host:/app/db
```

## 🏗️ Project Structure

```
OutlookSync/
├── src/
│   ├── OutlookSync.Web/            # Blazor Server application
│   ├── OutlookSync.Application/    # Application layer (use cases)
│   ├── OutlookSync.Domain/         # Domain layer (entities, aggregates)
│   └── OutlookSync.Infrastructure/ # Data access, Exchange services
├── test/
│   ├── OutlookSync.Application.Tests/
│   ├── OutlookSync.Domain.Tests/
│   └── OutlookSync.Infrastructure.Tests/
├── .github/workflows/              # CI/CD pipelines
├── Dockerfile                      # Multi-stage Docker build
└── Directory.Build.props           # Common build properties
```

## 🛠️ Local Development

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

### Build the Docker image locally

```bash
docker build -t outlooksync:local .
docker run -p 8080:8080 \
  -e BasicAuth__Username=admin \
  -e BasicAuth__Password=changeme \
  -v outlooksync-db:/app/db \
  outlooksync:local
```

### Health Checks

| Endpoint | Description |
|---|---|
| `GET /health/live` | Liveness — returns 200 if the application is running |
| `GET /health/ready` | Readiness — returns 200 when all dependencies are available |

## 🚀 CI/CD Pipeline

The GitHub Actions workflow (`.github/workflows/ci-cd.yml`) runs automatically on every push and pull request to `main` or `develop`:

1. **Build & Test** — restores packages, builds in Release mode, and runs all unit tests
2. **Publish** — on push to `main`, logs in to GitHub Container Registry and pushes the image with the following tags:
   - `ghcr.io/ricciolo/outlooksync:latest`
   - `ghcr.io/ricciolo/outlooksync:main`
   - `ghcr.io/ricciolo/outlooksync:sha-<commit>`

No secrets need to be configured; the workflow uses the built-in `GITHUB_TOKEN`.

## 🧪 Testing

```bash
# Run all tests
dotnet test

# Run tests with coverage
dotnet test --collect:"XPlat Code Coverage"
```

## 🔧 Architecture

This project follows Clean Architecture and Domain-Driven Design principles:

- **Domain layer** — core business entities and rules, no external dependencies
- **Application layer** — use cases orchestrating domain objects
- **Infrastructure layer** — SQLite/EF Core persistence, Exchange service clients
- **Web layer** — Blazor Server UI, authentication middleware, health checks

See [legend.md](legend.md) for the full list of best practices and references used in this project.

## 🤝 Contributing

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

## 📝 License

This project is licensed under the MIT License.

## 📧 Support

For questions and support, open an issue on GitHub.
