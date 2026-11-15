using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Avila.Platform.Identity;
using Avila.Platform.Lifecycle;
using Avila.Platform.Tenancy;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Npgsql;

namespace Avila.ProductionTests;

[Collection("production-postgres")]
public sealed class AdministrationHttpE2eTests
{
    private readonly ProductionFixture _fixture;

    public AdministrationHttpE2eTests(ProductionFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Owner_create_update_conflict_member_denial_and_suspension_guard()
    {
        var tenant = Guid.NewGuid();
        var email = $"adm-{Guid.NewGuid():N}@example.test";
        const string password = "CorrectHorseBattery1!";
        var identity = new IdentityService(_fixture.IdentityConnectionString, _fixture.IdentityMfaKey);
        var ownerInvite = await identity.InviteAsync(tenant, email, "owner");
        await identity.AcceptInvitationAsync(ownerInvite.Token, "Admin Owner", password);

        using var factory = Factory();
        using var client = factory.CreateClient();
        var ownerToken = await LoginOwnerAsync(client, email, password, tenant);

        using var create = new HttpRequestMessage(HttpMethod.Post, "/api/v1/administration/companies")
        {
            Content = JsonContent.Create(new
            {
                name = "Closure Co",
                taxId = $"J-{Guid.NewGuid():N}"[..10],
                fiscalAddress = "Av. Principal, Caracas",
                fiscalProfileComplete = true
            })
        };
        create.Headers.Authorization = Bearer(ownerToken);
        using var createResponse = await client.SendAsync(create);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        using var createJson = JsonDocument.Parse(await createResponse.Content.ReadAsStringAsync());
        var companyId = createJson.RootElement.GetProperty("id").GetGuid();
        Assert.Equal(1, createJson.RootElement.GetProperty("version").GetInt32());

        using var update = new HttpRequestMessage(
            HttpMethod.Patch,
            $"/api/v1/administration/companies/{companyId}")
        {
            Content = JsonContent.Create(new
            {
                name = "Closure Co Updated",
                taxId = createJson.RootElement.GetProperty("taxId").GetString(),
                fiscalAddress = "Av. Principal, Caracas",
                fiscalProfileComplete = true,
                version = 1
            })
        };
        update.Headers.Authorization = Bearer(ownerToken);
        using var updateResponse = await client.SendAsync(update);
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
        using var updateJson = JsonDocument.Parse(await updateResponse.Content.ReadAsStringAsync());
        Assert.Equal(2, updateJson.RootElement.GetProperty("version").GetInt32());
        Assert.Equal("Closure Co Updated", updateJson.RootElement.GetProperty("name").GetString());

        using var branchCreate = new HttpRequestMessage(
            HttpMethod.Post,
            "/api/v1/administration/branches")
        {
            Content = JsonContent.Create(new
            {
                companyId,
                code = "MAIN",
                name = "Main Branch",
                address = "Av. Principal, Caracas"
            })
        };
        branchCreate.Headers.Authorization = Bearer(ownerToken);
        using var branchResponse = await client.SendAsync(branchCreate);
        Assert.Equal(HttpStatusCode.Created, branchResponse.StatusCode);
        using var branchJson = JsonDocument.Parse(await branchResponse.Content.ReadAsStringAsync());
        var branchId = branchJson.RootElement.GetProperty("id").GetGuid();
        Assert.Equal(1, branchJson.RootElement.GetProperty("version").GetInt32());

        using var branchUpdate = new HttpRequestMessage(
            HttpMethod.Patch,
            $"/api/v1/administration/branches/{branchId}")
        {
            Content = JsonContent.Create(new
            {
                code = "MAIN",
                name = "Main Branch Updated",
                address = "Av. Principal, Caracas",
                isActive = true,
                version = 1
            })
        };
        branchUpdate.Headers.Authorization = Bearer(ownerToken);
        using var branchUpdateResponse = await client.SendAsync(branchUpdate);
        Assert.Equal(HttpStatusCode.OK, branchUpdateResponse.StatusCode);
        using var branchUpdateJson = JsonDocument.Parse(
            await branchUpdateResponse.Content.ReadAsStringAsync());
        Assert.Equal(2, branchUpdateJson.RootElement.GetProperty("version").GetInt32());

        using var stale = new HttpRequestMessage(
            HttpMethod.Patch,
            $"/api/v1/administration/companies/{companyId}")
        {
            Content = JsonContent.Create(new
            {
                name = "Stale",
                taxId = createJson.RootElement.GetProperty("taxId").GetString(),
                fiscalAddress = "Av. Principal, Caracas",
                fiscalProfileComplete = true,
                version = 1
            })
        };
        stale.Headers.Authorization = Bearer(ownerToken);
        using var staleResponse = await client.SendAsync(stale);
        Assert.Equal(HttpStatusCode.Conflict, staleResponse.StatusCode);

        var memberEmail = $"member-{Guid.NewGuid():N}@example.test";
        var memberInvite = await identity.InviteAsync(tenant, memberEmail, "member");
        await identity.AcceptInvitationAsync(memberInvite.Token, "Member", password);
        var memberLogin = await identity.LoginAsync(memberEmail, password, tenant);
        Assert.Equal(LoginStatus.Authenticated, memberLogin.Status);

        using var denied = new HttpRequestMessage(HttpMethod.Post, "/api/v1/administration/companies")
        {
            Content = JsonContent.Create(new
            {
                name = "Denied Co",
                taxId = $"J-{Guid.NewGuid():N}"[..10],
                fiscalAddress = (string?)null,
                fiscalProfileComplete = false
            })
        };
        denied.Headers.Authorization = Bearer(memberLogin.Token!);
        using var deniedResponse = await client.SendAsync(denied);
        Assert.Equal(HttpStatusCode.Forbidden, deniedResponse.StatusCode);

        await new TenantLifecycleStore(_fixture.AdminConnectionString)
            .SetStatusAsync(tenant, "suspended", "closure test");
        using var suspended = new HttpRequestMessage(HttpMethod.Post, "/api/v1/administration/companies")
        {
            Content = JsonContent.Create(new
            {
                name = "Suspended Co",
                taxId = $"J-{Guid.NewGuid():N}"[..10],
                fiscalAddress = (string?)null,
                fiscalProfileComplete = false
            })
        };
        suspended.Headers.Authorization = Bearer(ownerToken);
        using var suspendedResponse = await client.SendAsync(suspended);
        Assert.Equal((HttpStatusCode)423, suspendedResponse.StatusCode);

        var actions = await ActionsAsync(tenant);
        Assert.Contains("administration.company.created", actions);
        Assert.Contains("administration.company.updated", actions);
        Assert.Contains("authorization.denied", actions);
        Assert.Contains("tenant.write.blocked", actions);
        Assert.DoesNotContain("Suspended Co", await ReadCompanyNamesAsync(tenant));
    }

    private WebApplicationFactory<Program> Factory()
        => new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseSetting("ConnectionStrings:Avila", _fixture.AppConnectionString);
                builder.UseSetting("ConnectionStrings:Identity", _fixture.IdentityConnectionString);
                builder.UseSetting("Identity:MfaSecretKey", _fixture.IdentityMfaKey);
            });

    private static async Task<string> LoginOwnerAsync(
        HttpClient client,
        string email,
        string password,
        Guid tenant)
    {
        var login = await client.PostAsJsonAsync(
            "/api/v1/auth/login",
            new { email, password, tenantId = tenant });
        login.EnsureSuccessStatusCode();
        using var loginJson = JsonDocument.Parse(await login.Content.ReadAsStringAsync());
        var challengeId = loginJson.RootElement.GetProperty("mfaChallengeId").GetGuid();

        var begin = await client.PostAsJsonAsync(
            "/api/v1/auth/mfa/enroll/begin",
            new { challengeId, code = string.Empty });
        begin.EnsureSuccessStatusCode();
        using var beginJson = JsonDocument.Parse(await begin.Content.ReadAsStringAsync());
        var secret = beginJson.RootElement.GetProperty("secret").GetString()!;

        var complete = await client.PostAsJsonAsync(
            "/api/v1/auth/mfa/enroll/complete",
            new { challengeId, code = Totp.Code(secret) });
        complete.EnsureSuccessStatusCode();
        using var completeJson = JsonDocument.Parse(await complete.Content.ReadAsStringAsync());
        return completeJson.RootElement.GetProperty("token").GetString()!;
    }

    private static AuthenticationHeaderValue Bearer(string token) => new("Bearer", token);

    private async Task<List<string>> ActionsAsync(Guid tenant)
    {
        await using var connection = new NpgsqlConnection(_fixture.AdminConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(
            "SELECT action FROM platform.audit_events WHERE tenant_id = @tenant ORDER BY occurred_at",
            connection);
        command.Parameters.AddWithValue("tenant", tenant);
        await using var reader = await command.ExecuteReaderAsync();
        var actions = new List<string>();
        while (await reader.ReadAsync())
        {
            actions.Add(reader.GetString(0));
        }

        return actions;
    }

    private async Task<string> ReadCompanyNamesAsync(Guid tenant)
    {
        await using var connection = new NpgsqlConnection(_fixture.AdminConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(
            "SELECT COALESCE(string_agg(name, ','), '') FROM admin.companies WHERE tenant_id = @tenant",
            connection);
        command.Parameters.AddWithValue("tenant", tenant);
        return (string)(await command.ExecuteScalarAsync() ?? string.Empty);
    }
}
