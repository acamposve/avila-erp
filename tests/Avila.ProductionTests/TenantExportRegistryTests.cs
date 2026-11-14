using System.Text.Json;
using Avila.Platform.Exporting;
using Avila.Platform.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace Avila.ProductionTests;

public sealed class TenantExportRegistryTests
{
    [Fact]
    public async Task Sections_and_records_are_deterministically_ordered()
    {
        var tenant = Guid.NewGuid();
        var registry = new TenantExportRegistry(
        [
            new FakeContributor("beta", 20, 1,
            [
                Record("company", "b", new { z = 1, a = 2 }),
                Record("company", "a", new { z = 1, a = 2 })
            ]),
            new FakeContributor("alpha", 10, 1,
            [
                Record("party", "2", new { name = "two" }),
                Record("party", "1", new { name = "one" })
            ])
        ]);

        var envelope = await registry.ExportAsync(new FakeUnitOfWork(tenant));

        Assert.Equal(["alpha", "beta"], envelope.Sections.Select(x => x.ModuleKey).ToArray());
        Assert.Equal(["1", "2"], envelope.Sections[0].Records.Select(x => x.Id).ToArray());
        Assert.Equal(["a", "b"], envelope.Sections[1].Records.Select(x => x.Id).ToArray());
        Assert.Equal(["alpha", "beta"], envelope.Manifest.Sections.Select(x => x.ModuleKey).ToArray());
        Assert.Equal(2, envelope.Manifest.Sections[0].RecordCount);
        Assert.Equal(64, envelope.Manifest.Sha256.Length);
    }

    [Fact]
    public async Task Identical_tenant_state_produces_identical_manifest_hash_regardless_of_registration_order()
    {
        var tenant = Guid.NewGuid();
        var first = new TenantExportRegistry(
        [
            new FakeContributor("beta", 20, 1,
            [
                Record("company", "b", new { z = 1, a = 2 }),
                Record("company", "a", new { z = 1, a = 2 })
            ]),
            new FakeContributor("alpha", 10, 1,
            [
                Record("party", "2", new { name = "two" }),
                Record("party", "1", new { name = "one" })
            ])
        ]);

        var second = new TenantExportRegistry(
        [
            new FakeContributor("alpha", 10, 1,
            [
                Record("party", "1", new { name = "one" }),
                Record("party", "2", new { name = "two" })
            ]),
            new FakeContributor("beta", 20, 1,
            [
                Record("company", "a", new { a = 2, z = 1 }),
                Record("company", "b", new { a = 2, z = 1 })
            ])
        ]);

        var a = await first.ExportAsync(new FakeUnitOfWork(tenant));
        var b = await second.ExportAsync(new FakeUnitOfWork(tenant));

        Assert.Equal(a.Manifest.Sha256, b.Manifest.Sha256);
        Assert.Equal(
            a.Manifest.Sections.Select(x => x.Sha256),
            b.Manifest.Sections.Select(x => x.Sha256));
    }

    [Fact]
    public async Task Different_tenant_state_changes_hash()
    {
        var tenant = Guid.NewGuid();
        var registry = new TenantExportRegistry(
        [
            new FakeContributor("alpha", 10, 1,
            [
                Record("party", "1", new { name = "one" })
            ])
        ]);
        var changed = new TenantExportRegistry(
        [
            new FakeContributor("alpha", 10, 1,
            [
                Record("party", "1", new { name = "changed" })
            ])
        ]);

        var a = await registry.ExportAsync(new FakeUnitOfWork(tenant));
        var b = await changed.ExportAsync(new FakeUnitOfWork(tenant));

        Assert.NotEqual(a.Manifest.Sha256, b.Manifest.Sha256);
    }

    [Fact]
    public void Duplicate_module_keys_are_rejected()
    {
        Assert.Throws<ArgumentException>(() => new TenantExportRegistry(
        [
            new FakeContributor("alpha", 10, 1, []),
            new FakeContributor("alpha", 20, 1, [])
        ]));
    }

    [Fact]
    public async Task Duplicate_entity_ids_are_rejected()
    {
        var registry = new TenantExportRegistry(
        [
            new FakeContributor("alpha", 10, 1,
            [
                Record("party", "1", new { name = "one" }),
                Record("party", "1", new { name = "duplicate" })
            ])
        ]);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => registry.ExportAsync(new FakeUnitOfWork(Guid.NewGuid())));
    }

    [Fact]
    public void Export_orchestrator_does_not_reference_domain_modules()
    {
        var references = typeof(TenantExportRegistry).Assembly.GetReferencedAssemblies();
        Assert.DoesNotContain(
            references,
            reference => reference.Name?.StartsWith("Avila.Modules.", StringComparison.Ordinal) == true);
    }

    private static TenantExportRecord Record(string entity, string id, object data)
        => new(entity, id, JsonSerializer.SerializeToElement(data));

    private sealed class FakeContributor : ITenantExportContributor
    {
        private readonly IReadOnlyList<TenantExportRecord> _records;

        public FakeContributor(
            string moduleKey,
            int order,
            int schemaVersion,
            IReadOnlyList<TenantExportRecord> records)
        {
            ModuleKey = moduleKey;
            Order = order;
            SchemaVersion = schemaVersion;
            _records = records;
        }

        public string ModuleKey { get; }
        public int Order { get; }
        public int SchemaVersion { get; }

        public Task<TenantExportSection> ExportAsync(
            IUnitOfWork unitOfWork,
            TenantExportContext context,
            CancellationToken cancellationToken = default)
            => Task.FromResult(new TenantExportSection(ModuleKey, SchemaVersion, _records));
    }

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        public FakeUnitOfWork(Guid tenantId)
        {
            TenantId = tenantId;
        }

        public Guid TenantId { get; }
        public Npgsql.NpgsqlConnection Connection => throw new NotSupportedException();
        public Npgsql.NpgsqlTransaction Transaction => throw new NotSupportedException();

        public DbContextOptions<T> OptionsFor<T>(string migrationSchema) where T : DbContext
            => throw new NotSupportedException();

        public void Enlist(DbContext context) => throw new NotSupportedException();
        public Task CommitAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task RollbackAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
