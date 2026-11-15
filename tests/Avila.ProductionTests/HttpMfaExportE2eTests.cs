using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Avila.Modules.Administration;
using Avila.Modules.Administration.Contracts;
using Avila.Platform.Identity;
using Avila.Platform.Tenancy;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Avila.ProductionTests;

[Collection("production-postgres")]
public sealed class HttpMfaExportE2eTests
{
    private readonly ProductionFixture _fixture;

    public HttpMfaExportE2eTests(ProductionFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Login_enroll_mfa_then_bearer_export_returns_ndjson()
    {
        var tenant = Guid.NewGuid();
        var email = $"http-{Guid.NewGuid():N}@example.test";
        const string password = "CorrectHorseBattery1!";
        var identity = new IdentityService(_fixture.IdentityConnectionString, _fixture.IdentityMfaKey);
        var invitation = await identity.InviteAsync(tenant, email, "owner");
        await identity.AcceptInvitationAsync(invitation.Token, "HTTP Owner", password);
        await CreateCompanyAsync(tenant, "HTTP Export Co");

        using var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseSetting("ConnectionStrings:Avila", _fixture.AppConnectionString);
                builder.UseSetting("ConnectionStrings:Identity", _fixture.IdentityConnectionString);
                builder.UseSetting("Identity:MfaSecretKey", _fixture.IdentityMfaKey);
            });
        using var client = factory.CreateClient();

        var loginResponse = await client.PostAsJsonAsync(
            "/api/v1/auth/login",
            new { email, password, tenantId = tenant });
        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);
        using var loginJson = JsonDocument.Parse(await loginResponse.Content.ReadAsStringAsync());
        Assert.Equal(
            (int)LoginStatus.MfaEnrollmentRequired,
            loginJson.RootElement.GetProperty("status").GetInt32());
        var challengeId = loginJson.RootElement.GetProperty("mfaChallengeId").GetGuid();

        var beginResponse = await client.PostAsJsonAsync(
            "/api/v1/auth/mfa/enroll/begin",
            new { challengeId, code = string.Empty });
        beginResponse.EnsureSuccessStatusCode();
        using var beginJson = JsonDocument.Parse(await beginResponse.Content.ReadAsStringAsync());
        var secret = beginJson.RootElement.GetProperty("secret").GetString();
        Assert.False(string.IsNullOrWhiteSpace(secret));

        var completeResponse = await client.PostAsJsonAsync(
            "/api/v1/auth/mfa/enroll/complete",
            new { challengeId, code = Totp.Code(secret!) });
        completeResponse.EnsureSuccessStatusCode();
        using var completeJson = JsonDocument.Parse(await completeResponse.Content.ReadAsStringAsync());
        var token = completeJson.RootElement.GetProperty("token").GetString();
        Assert.False(string.IsNullOrWhiteSpace(token));

        using var exportRequest = new HttpRequestMessage(HttpMethod.Get, "/api/v1/tenant/export");
        exportRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        using var exportResponse = await client.SendAsync(
            exportRequest,
            HttpCompletionOption.ResponseHeadersRead);
        Assert.Equal(HttpStatusCode.OK, exportResponse.StatusCode);
        Assert.StartsWith("application/x-ndjson", exportResponse.Content.Headers.ContentType?.ToString());
        var body = await exportResponse.Content.ReadAsStringAsync();
        Assert.Contains("\"type\":\"manifest\"", body);
        Assert.Contains("HTTP Export Co", body);
        Assert.DoesNotContain("password_hash", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("mfa_secret", body, StringComparison.OrdinalIgnoreCase);
    }

    private async Task CreateCompanyAsync(Guid tenant, string name)
    {
        await using var unitOfWork = await new TenantUnitOfWorkFactory()
            .BeginAsync(_fixture.AppConnectionString, tenant);
        await new AdministrationModule().CreateCompanyAsync(
            unitOfWork,
            new CreateCompanyCommand(
                name,
                $"J-{Guid.NewGuid():N}"[..10],
                "Av. Principal, Caracas",
                FiscalProfileComplete: true));
        await unitOfWork.CommitAsync();
    }
}
