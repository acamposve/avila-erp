using Avila.Api.Identity;
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

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAvilaTenancy();
builder.Services.AddAvilaIdentity();
builder.Services.AddScoped<IAdministrationModule, AdministrationModule>();
builder.Services.AddScoped<IInventoryModule, InventoryModule>();
builder.Services.AddScoped<ISalesModule, SalesModule>();
builder.Services.AddScoped<IBillingModule, BillingModule>();
builder.Services.AddScoped<SalesFlowService>();
builder.Services.AddHealthChecks();

var app = builder.Build();

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

app.MapHealthChecks("/health");
app.MapIdentityEndpoints();
app.MapGet("/", () => Results.Ok(new
{
    product = "ÁVILA ERP",
    status = "production-skeleton",
    verticalSlice = "tenant -> company -> party -> item -> quote -> order -> invoice draft"
}));

app.Run();

public partial class Program;
