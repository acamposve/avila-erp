using Avila.Platform.Identity;

namespace Avila.Api.Identity;

public static class IdentityServiceCollectionExtensions
{
    public static IServiceCollection AddAvilaIdentity(this IServiceCollection services)
    {
        services.AddSingleton(provider =>
        {
            var configuration = provider.GetRequiredService<IConfiguration>();
            var connectionString = configuration.GetConnectionString("Identity");
            var mfaSecretKey = configuration["Identity:MfaSecretKey"];

            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException("ConnectionStrings:Identity is not configured.");
            }

            if (string.IsNullOrWhiteSpace(mfaSecretKey))
            {
                throw new InvalidOperationException("Identity:MfaSecretKey is not configured.");
            }

            return new IdentityService(connectionString, mfaSecretKey);
        });

        return services;
    }
}

public static class IdentityApplicationBuilderExtensions
{
    public static IApplicationBuilder UseAvilaSessionAuthentication(this IApplicationBuilder app)
        => app.UseMiddleware<SessionAuthenticationMiddleware>();
}
