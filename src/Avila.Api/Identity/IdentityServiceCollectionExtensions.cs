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

            var smtpHost = configuration["Email:Smtp:Host"];
            IEmailSender emailSender = string.IsNullOrWhiteSpace(smtpHost)
                ? new SinkEmailSender()
                : new SmtpEmailSender(new SmtpEmailOptions(
                    smtpHost,
                    int.TryParse(configuration["Email:Smtp:Port"], out var port) ? port : 587,
                    configuration["Email:Smtp:From"] ?? "no-reply@avila.local",
                    configuration["Email:Smtp:UserName"],
                    configuration["Email:Smtp:Password"],
                    !string.Equals(
                        configuration["Email:Smtp:EnableSsl"],
                        "false",
                        StringComparison.OrdinalIgnoreCase)));

            var auditWriter = new IdentityAuditWriter(
                new Avila.Platform.Tenancy.TenantUnitOfWorkFactory(),
                configuration.GetConnectionString("Avila") ?? string.Empty);

            return new IdentityService(connectionString, mfaSecretKey, emailSender, auditWriter);
        });

        return services;
    }
}

public static class IdentityApplicationBuilderExtensions
{
    public static IApplicationBuilder UseAvilaSessionAuthentication(this IApplicationBuilder app)
        => app.UseMiddleware<SessionAuthenticationMiddleware>();
}
