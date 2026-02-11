# Multi-stage Dockerfile for ASP.NET Core 10 (OutlookSync)
# This Dockerfile follows best practices for cloud-native applications

# Stage 1: Base runtime image
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

# Create a non-root user for security
RUN groupadd -r appuser && useradd -r -g appuser appuser

# Stage 2: Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copy solution and project files
COPY ["OutlookSync.sln", "./"]
COPY ["Directory.Build.props", "./"]
COPY ["global.json", "./"]
COPY ["src/OutlookSync.Api/OutlookSync.Api.csproj", "src/OutlookSync.Api/"]
COPY ["test/OutlookSync.Api.Tests/OutlookSync.Api.Tests.csproj", "test/OutlookSync.Api.Tests/"]

# Restore dependencies (cached layer if project files haven't changed)
RUN dotnet restore "OutlookSync.sln"

# Copy the rest of the source code
COPY . .

# Build the application
WORKDIR "/src/src/OutlookSync.Api"
RUN dotnet build "OutlookSync.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build --no-restore

# Stage 3: Test stage
FROM build AS test
WORKDIR /src

# Run all tests (rebuild to ensure everything is compiled correctly in this stage)
RUN dotnet test "OutlookSync.sln" -c $BUILD_CONFIGURATION --verbosity normal

# Stage 4: Publish stage
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
WORKDIR "/src/src/OutlookSync.Api"

# Publish the application
RUN dotnet publish "OutlookSync.Api.csproj" \
    -c $BUILD_CONFIGURATION \
    -o /app/publish \
    --no-restore \
    /p:UseAppHost=false

# Stage 5: Final release image
FROM base AS final
WORKDIR /app

# Copy published output from publish stage
COPY --from=publish /app/publish .

# Switch to non-root user for security
USER appuser

# Set environment variables for cloud-native deployment
ENV ASPNETCORE_ENVIRONMENT=Production \
    ASPNETCORE_URLS=http://+:8080 \
    DOTNET_RUNNING_IN_CONTAINER=true \
    DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false

# Health check for container orchestration (Kubernetes, Docker Compose)
HEALTHCHECK --interval=30s --timeout=3s --start-period=10s --retries=3 \
    CMD curl -f http://localhost:8080/health/live || exit 1

# Entry point
ENTRYPOINT ["dotnet", "OutlookSync.Api.dll"]
