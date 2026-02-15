using OutlookSync.Infrastructure.Persistence;
using OutlookSync.Web.Components;
using OutlookSync.Web.Extensions;
using Serilog;

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("logs/outlooksync-.log", rollingInterval: RollingInterval.Day)
    .Enrich.FromLogContext()
    .CreateLogger();

try
{
    Log.Information("Starting OutlookSync Web application");

    var builder = WebApplication.CreateBuilder(args);

    // Add Serilog
    builder.Host.UseSerilog();

    // Add services to the container.
    builder.Services.AddRazorComponents();

    // Add SignalR for real-time updates
    builder.Services.AddSignalR();

    // Add OutlookSync services (Domain, Application, Infrastructure)
    builder.Services.AddOutlookSyncServices(builder.Configuration);

    // Add health checks for cloud-native deployments
    builder.Services.AddHealthChecks()
        .AddDbContextCheck<OutlookSyncDbContext>();

    var app = builder.Build();

    // Apply database migrations
    await app.ApplyMigrationsAsync();

    // Configure the HTTP request pipeline.
    if (!app.Environment.IsDevelopment())
    {
        app.UseExceptionHandler("/Error", createScopeForErrors: true);
        // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
        app.UseHsts();
    }

    app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
    app.UseHttpsRedirection();

    // Health check endpoints for Kubernetes/Docker
    app.MapHealthChecks("/health/live", new()
    {
        Predicate = _ => false // Liveness check - always healthy if app is running
    });

    app.MapHealthChecks("/health/ready", new()
    {
        Predicate = _ => true // Readiness check - checks all registered health checks
    });

    app.UseAntiforgery();

    app.MapStaticAssets();
    app.MapRazorComponents<App>();

    Log.Information("OutlookSync Web application started successfully");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
