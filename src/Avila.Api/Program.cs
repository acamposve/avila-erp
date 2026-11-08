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
builder.Services.AddScoped<IAdministrationModule, AdministrationModule>();
builder.Services.AddScoped<IInventoryModule, InventoryModule>();
builder.Services.AddScoped<ISalesModule, SalesModule>();
builder.Services.AddScoped<IBillingModule, BillingModule>();
builder.Services.AddScoped<SalesFlowService>();
builder.Services.AddHealthChecks();

var app = builder.Build();

// Business endpoints must carry a tenant resolved by identity (T-006).
// The hook is ready; no business endpoint is exposed in this slice.
app.UseWhen(
    context => context.Request.Path.StartsWithSegments("/api"),
    branch => branch.UseTenantUnitOfWork());

app.MapHealthChecks("/health");
app.MapGet("/", () => Results.Ok(new
{
    product = "ÁVILA ERP",
    status = "production-skeleton",
    verticalSlice = "tenant -> company -> party -> item -> quote -> order -> invoice draft"
}));

app.Run();

public partial class Program;
