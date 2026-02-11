var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// Add health checks for cloud-native deployments
builder.Services.AddHealthChecks();

// Configure Kestrel for cloud-native environments
builder.WebHost.ConfigureKestrel(options =>
{
    // Listen on port from environment variable or default to 8080
    var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
    options.ListenAnyIP(int.Parse(port));
});

var app = builder.Build();

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
