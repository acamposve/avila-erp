using Avila.Platform.Tenancy;

namespace Avila.Api.Tenancy;

public sealed class TenantUnitOfWorkMiddleware
{
    private readonly RequestDelegate _next;

    public TenantUnitOfWorkMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(
        HttpContext context,
        ITenantContext tenantContext,
        IUnitOfWorkFactory unitOfWorkFactory,
        IConfiguration configuration)
    {
        if (!tenantContext.IsResolved)
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsync("Tenant context has not been resolved.");
            return;
        }

        var connectionString = configuration.GetConnectionString("Avila");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("ConnectionStrings:Avila is not configured.");
        }

        await using var unitOfWork = await unitOfWorkFactory.BeginAsync(
            connectionString,
            tenantContext.TenantId,
            context.RequestAborted);

        context.Items[typeof(IUnitOfWork)] = unitOfWork;

        try
        {
            await _next(context);

            if (context.Response.StatusCode is >= 200 and < 400)
            {
                await unitOfWork.CommitAsync(context.RequestAborted);
            }
            else
            {
                await unitOfWork.RollbackAsync(context.RequestAborted);
            }
        }
        catch
        {
            await unitOfWork.RollbackAsync(CancellationToken.None);
            throw;
        }
    }
}
