using Avila.Platform.Persistence;
using Avila.Platform.Tenancy;

namespace Avila.Api.Tenancy;

public static class TenancyServiceCollectionExtensions
{
    public static IServiceCollection AddAvilaTenancy(this IServiceCollection services)
    {
        services.AddSingleton<IUnitOfWorkFactory, TenantUnitOfWorkFactory>();
        services.AddScoped<ITenantContext, TenantContext>();
        services.AddScoped<TenantSettingsBackfill>();
        return services;
    }
}

public static class TenancyApplicationBuilderExtensions
{
    public static IApplicationBuilder UseTenantUnitOfWork(this IApplicationBuilder app)
        => app.UseMiddleware<TenantUnitOfWorkMiddleware>();
}
