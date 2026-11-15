using Avila.Platform.Authorization;
using Avila.Platform.Lifecycle;
using Avila.Platform.Persistence;
using Avila.Platform.Tenancy;

namespace Avila.Api.Tenancy;

public static class TenancyServiceCollectionExtensions
{
    public static IServiceCollection AddAvilaTenancy(this IServiceCollection services)
    {
        services.AddSingleton<IUnitOfWorkFactory, TenantUnitOfWorkFactory>();
        services.AddSingleton(provider =>
        {
            var configuration = provider.GetRequiredService<IConfiguration>();
            var connectionString = configuration.GetConnectionString("Avila") ?? string.Empty;
            return new SecurityAuditWriter(new TenantUnitOfWorkFactory(), connectionString);
        });
        services.AddScoped<AuthorizationService>();
        services.AddScoped<TenantAccessGuard>();
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
