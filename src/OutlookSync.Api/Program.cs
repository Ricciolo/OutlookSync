using OutlookSync.Api.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// Add OutlookSync services (Domain, Application, Infrastructure)
builder.Services.AddOutlookSyncServices(builder.Configuration);

// Add health checks for cloud-native deployments
builder.Services.AddHealthChecks()
    .AddDbContextCheck<OutlookSync.Infrastructure.Persistence.OutlookSyncDbContext>();

var app = builder.Build();

// Apply database migrations
await app.ApplyMigrationsAsync();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// Health check endpoints for Kubernetes/Docker
app.MapHealthChecks("/health/live", new()
{
    Predicate = _ => false // Liveness check - always healthy if app is running
});

app.MapHealthChecks("/health/ready", new()
{
    Predicate = _ => true // Readiness check - checks all registered health checks
});

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
