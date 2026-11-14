using Avila.Api.Identity;
using Avila.Api.Observability;
using Avila.Api.Tenancy;
using Avila.Application;
using Avila.Modules.Administration;
using Avila.Modules.Administration.Contracts;
using Avila.Modules.Billing;
using Avila.Modules.Billing.Contracts;
using Avila.Modules.Inventory;
using Avila.Modules.Inventory.Contracts;
using Avila.Modules.Sales;
using Avila.Modules.Sales.Contracts;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAvilaTenancy();
builder.Services.AddAvilaIdentity();
builder.Services.AddScoped<IAdministrationModule, AdministrationModule>();
builder.Services.AddScoped<IInventoryModule, InventoryModule>();
builder.Services.AddScoped<ISalesModule, SalesModule>();
builder.Services.AddScoped<IBillingModule, BillingModule>();
builder.Services.AddScoped<SalesFlowService>();
builder.Services.AddSingleton<MetricsRegistry>();
builder.Services.AddHealthChecks()
    .AddCheck<DatabaseHealthCheck>("database", tags: ["ready"]);

var app = builder.Build();

app.UseMiddleware<RequestObservabilityMiddleware>();
app.UseExceptionHandler(errorApp => errorApp.Run(async context =>
{
    context.Response.StatusCode = StatusCodes.Status500InternalServerError;
    context.Response.ContentType = "application/problem+json";
    await context.Response.WriteAsJsonAsync(new
    {
        type = "about:blank",
        title = "Internal server error",
        status = StatusCodes.Status500InternalServerError,
        traceId = context.TraceIdentifier
    });
}));

// Business endpoints require a session token and a validated tenant.
// Authentication endpoints (to be added) live under /api/v1/auth and are excluded.
app.UseWhen(
    context => context.Request.Path.StartsWithSegments("/api")
               && !context.Request.Path.StartsWithSegments("/api/v1/auth"),
    branch =>
    {
        branch.UseAvilaSessionAuthentication();
        branch.UseTenantUnitOfWork();
    });

app.MapHealthChecks("/health", new HealthCheckOptions
{
    Predicate = _ => false
});
app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = _ => false
});
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = registration => registration.Tags.Contains("ready")
});
app.MapGet("/metrics", (MetricsRegistry metrics) =>
    Results.Text(metrics.Render(), "text/plain; version=0.0.4"));
app.MapIdentityEndpoints();
app.MapGet("/", () => Results.Ok(new
{
    product = "ÁVILA ERP",
    status = "production-skeleton",
    verticalSlice = "tenant -> company -> party -> item -> quote -> order -> invoice draft"
}));

app.Run();

public partial class Program;
