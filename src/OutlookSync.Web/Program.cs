using OutlookSync.Infrastructure.Persistence;
using OutlookSync.Web.Components;
using OutlookSync.Web.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

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
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
