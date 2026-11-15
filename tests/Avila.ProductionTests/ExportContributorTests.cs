using System.Text.Json;
using Avila.Modules.Administration;
using Avila.Modules.Administration.Contracts;
using Avila.Platform.Authorization;
using Avila.Platform.Exporting;
using Avila.Platform.Identity;
using Avila.Platform.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace Avila.ProductionTests;

[Collection("production-postgres")]
public sealed class ExportContributorTests
{
    private readonly ProductionFixture _fixture;

    public ExportContributorTests(ProductionFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task R1_contributors_export_only_the_target_tenant_and_never_credentials()
    {
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();
        var emailA = $"a-{Guid.NewGuid():N}@example.test";
        var emailB = $"b-{Guid.NewGuid():N}@example.test";
        await SeedAdministrationAsync(tenantA, "A Company", "A Party");
        await SeedAdministrationAsync(tenantB, "B Company", "B Party");
        await SeedIdentityAsync(tenantA, emailA, "PASSWORD_HASH_A", "MFA_SECRET_A", "TOKEN_HASH_A");
        await SeedIdentityAsync(tenantB, emailB, "PASSWORD_HASH_B", "MFA_SECRET_B", "TOKEN_HASH_B");
        await SeedAuditAsync(tenantA, "tenant-a-export");
        await SeedAuditAsync(tenantB, "tenant-b-export");

        var registry = new TenantExportRegistry(
        [
            new AdministrationExportContributor(new AdministrationModule()),
            new AuditExportContributor(),
            new IdentityExportContributor(_fixture.IdentityConnectionString)
        ]);

        TenantExportEnvelope envelopeA;
        await using (var unitOfWork = await BeginAsync(tenantA))
        {
            envelopeA = await registry.ExportAsync(unitOfWork);
        }

        var sectionNames = envelopeA.Sections.Select(x => x.ModuleKey).ToArray();
        Assert.Equal(["administration", "identity", "audit"], sectionNames);

        var jsonA = JsonSerializer.Serialize(envelopeA, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        Assert.Contains("A Company", jsonA);
        Assert.Contains("A Party", jsonA);
        Assert.Contains(emailA, jsonA);
        Assert.Contains("tenant-a-export", jsonA);
        Assert.DoesNotContain("B Company", jsonA);
        Assert.DoesNotContain("B Party", jsonA);
        Assert.DoesNotContain(emailB, jsonA);
        Assert.DoesNotContain("tenant-b-export", jsonA);

        Assert.DoesNotContain("PASSWORD_HASH_A", jsonA);
        Assert.DoesNotContain("PASSWORD_HASH_B", jsonA);
        Assert.DoesNotContain("MFA_SECRET_A", jsonA);
        Assert.DoesNotContain("MFA_SECRET_B", jsonA);
        Assert.DoesNotContain("TOKEN_HASH_A", jsonA);
        Assert.DoesNotContain("TOKEN_HASH_B", jsonA);
        Assert.DoesNotContain("RECOVERY_CODE_SENTINEL", jsonA);
        Assert.DoesNotContain("SESSION_TOKEN_SENTINEL", jsonA);
        Assert.DoesNotContain("password_hash", jsonA, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("mfa_secret", jsonA, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("token_hash", jsonA, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("recovery_code", jsonA, StringComparison.OrdinalIgnoreCase);

        TenantExportEnvelope envelopeB;
        await using (var unitOfWork = await BeginAsync(tenantB))
        {
            envelopeB = await registry.ExportAsync(unitOfWork);
        }

        var jsonB = JsonSerializer.Serialize(envelopeB, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        Assert.Contains("B Company", jsonB);
        Assert.Contains(emailB, jsonB);
        Assert.DoesNotContain("A Company", jsonB);
        Assert.DoesNotContain(emailA, jsonB);
        Assert.DoesNotContain("tenant-a-export", jsonB);
    }

    [Fact]
    public async Task Identity_contributor_rejects_non_identity_role()
    {
        var tenant = Guid.NewGuid();
        await SeedIdentityAsync(tenant, $"x-{Guid.NewGuid():N}@example.test", "PASSWORD", "MFA", "TOKEN");
        var contributor = new IdentityExportContributor(_fixture.AppConnectionString);
        var registry = new TenantExportRegistry([contributor]);

        await using var unitOfWork = await BeginAsync(tenant);
        await Assert.ThrowsAsync<InvalidOperationException>(() => registry.ExportAsync(unitOfWork));
    }

    [Fact]
    public async Task Identical_export_state_has_same_manifest_hash()
    {
        var tenant = Guid.NewGuid();
        await SeedAdministrationAsync(tenant, "Same Company", "Same Party");
        var registry = new TenantExportRegistry(
        [
            new AdministrationExportContributor(new AdministrationModule())
        ]);

        TenantExportEnvelope first;
        await using (var unitOfWork = await BeginAsync(tenant))
        {
            first = await registry.ExportAsync(unitOfWork);
        }

        TenantExportEnvelope second;
        await using (var unitOfWork = await BeginAsync(tenant))
        {
            second = await registry.ExportAsync(unitOfWork);
        }

        Assert.Equal(first.Manifest.Sha256, second.Manifest.Sha256);
        Assert.Equal(
            first.Manifest.Sections.Select(x => x.Sha256),
            second.Manifest.Sections.Select(x => x.Sha256));
    }

    private async Task SeedAdministrationAsync(Guid tenant, string companyName, string partyName)
    {
        await using var unitOfWork = await BeginAsync(tenant);
        var module = new AdministrationModule();
        var company = await module.CreateCompanyAsync(
            unitOfWork,
            new CreateCompanyCommand(
                companyName,
                $"J-{Guid.NewGuid():N}"[..10],
                "Av. Principal, Caracas",
                FiscalProfileComplete: true));
        await module.CreateBranchAsync(
            unitOfWork,
            new CreateBranchCommand(company.Id, $"B-{Guid.NewGuid():N}"[..10], "Main branch"));
        await module.CreatePartyAsync(
            unitOfWork,
            new CreatePartyCommand(partyName, $"V-{Guid.NewGuid():N}"[..10], true, true));
        await unitOfWork.CommitAsync();
    }

    private async Task SeedIdentityAsync(
        Guid tenant,
        string email,
        string passwordHash,
        string mfaSecret,
        string tokenHash)
    {
        var options = new DbContextOptionsBuilder<IdentityDbContext>()
            .UseNpgsql(_fixture.IdentityConnectionString)
            .Options;
        await using var db = new IdentityDbContext(options);
        var user = new IdentityUser
        {
            Id = Guid.NewGuid(),
            Email = email,
            NormalizedEmail = email.ToUpperInvariant(),
            DisplayName = email.Split('@')[0],
            PasswordHash = passwordHash,
            MfaSecret = mfaSecret,
            MfaEnabled = true
        };
        db.Users.Add(user);
        db.Memberships.Add(new Membership
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TenantId = tenant,
            Role = "member",
            Status = "active"
        });
        db.Invitations.Add(new Invitation
        {
            Id = Guid.NewGuid(),
            TenantId = tenant,
            Email = email,
            NormalizedEmail = email.ToUpperInvariant(),
            Role = "member",
            TokenHash = tokenHash,
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(1)
        });
        db.Sessions.Add(new Session
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            ActiveTenantId = tenant,
            TokenHash = $"SESSION_TOKEN_SENTINEL_{tenant:N}",
            ExpiresAt = DateTimeOffset.UtcNow.AddHours(1)
        });
        await db.SaveChangesAsync();
    }

    private async Task SeedAuditAsync(Guid tenant, string action)
    {
        await using var unitOfWork = await BeginAsync(tenant);
        var actor = new SessionPrincipal(
            Guid.NewGuid(),
            tenant,
            "owner@example.test",
            "owner");
        await new AuditService().RecordAsync(
            unitOfWork,
            actor,
            action,
            "export_test",
            Guid.NewGuid(),
            before: null,
            after: new { ok = true },
            correlationId: Guid.NewGuid().ToString("N"));
        await unitOfWork.CommitAsync();
    }

    private async Task<IUnitOfWork> BeginAsync(Guid tenant)
        => await new TenantUnitOfWorkFactory().BeginAsync(_fixture.AppConnectionString, tenant);
}
