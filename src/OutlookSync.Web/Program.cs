using OutlookSync.Infrastructure.Persistence;
using OutlookSync.Web.Components;
using OutlookSync.Web.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Add OutlookSync services (Domain, Application, Infrastructure)
builder.Services.AddOutlookSyncServices(builder.Configuration);

// Add HTTP basic authentication (credentials configurable via BasicAuth:Username / BasicAuth:Password)
builder.Services.AddBasicAuthentication(builder.Configuration);

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
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);

app.UseAuthentication();
app.UseAuthorization();

// Health check endpoints for Kubernetes/Docker (no authentication required)
app.MapHealthChecks("/health/live", new()
{
    Predicate = _ => false // Liveness check - always healthy if app is running
}).AllowAnonymous();

app.MapHealthChecks("/health/ready", new()
{
    Predicate = _ => true // Readiness check - checks all registered health checks
}).AllowAnonymous();

app.UseAntiforgery();

app.UseStaticFiles(); 
app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .RequireAuthorization();

app.Run();

