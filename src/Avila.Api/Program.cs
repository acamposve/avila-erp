using Avila.Application;
using Avila.Modules.Administration;
using Avila.Modules.Administration.Contracts;
using Avila.Modules.Billing;
using Avila.Modules.Billing.Contracts;
using Avila.Modules.Inventory;
using Avila.Modules.Inventory.Contracts;
using Avila.Modules.Sales;
using Avila.Modules.Sales.Contracts;
using Avila.Platform.Tenancy;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<IUnitOfWorkFactory, TenantUnitOfWorkFactory>();
builder.Services.AddScoped<IAdministrationModule, AdministrationModule>();
builder.Services.AddScoped<IInventoryModule, InventoryModule>();
builder.Services.AddScoped<ISalesModule, SalesModule>();
builder.Services.AddScoped<IBillingModule, BillingModule>();
builder.Services.AddScoped<SalesFlowService>();
builder.Services.AddHealthChecks();

var app = builder.Build();

app.MapHealthChecks("/health");
app.MapGet("/", () => Results.Ok(new
{
    product = "ÁVILA ERP",
    status = "production-skeleton",
    verticalSlice = "tenant -> company -> party -> item -> quote -> order -> invoice draft"
}));

app.Run();

public partial class Program;
