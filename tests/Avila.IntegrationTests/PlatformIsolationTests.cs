using Npgsql;

namespace Avila.IntegrationTests;

[Collection("postgres")]
public sealed class PlatformIsolationTests
{
    private readonly PostgresFixture _fixture;

    public PlatformIsolationTests(PostgresFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task T09_platform_tables_respect_rls_and_reject_cross_tenant_writes()
    {
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();

        await TestDb.SeedTenantNoteAsync(_fixture.AdminConnectionString, tenantA, "A");
        await TestDb.SeedTenantNoteAsync(_fixture.AdminConnectionString, tenantB, "B");

        var (notesA, pidA) = await TestDb.QueryTenantNotesAsync(_fixture.AppConnectionString, tenantA);
        var (notesB, pidB) = await TestDb.QueryTenantNotesAsync(_fixture.AppConnectionString, tenantB);
        var (notesWithoutContext, pidWithoutContext) = await TestDb.QueryTenantNotesAsync(
            _fixture.AppConnectionString,
            tenantId: null);

        Assert.Equal(new[] { tenantA }, notesA);
        Assert.Equal(new[] { tenantB }, notesB);
        Assert.Empty(notesWithoutContext);
        Assert.True(pidA > 0);
        Assert.True(pidB > 0);
        Assert.Equal(-1, pidWithoutContext);

        var writeException = await TestDb.TryInsertCrossTenantNoteAsync(
            _fixture.AppConnectionString,
            contextTenant: tenantA,
            noteTenant: tenantB);

        var postgresException = Assert.IsType<PostgresException>(writeException);
        Assert.Equal(PostgresErrorCodes.InsufficientPrivilege, postgresException.SqlState);
    }
}
