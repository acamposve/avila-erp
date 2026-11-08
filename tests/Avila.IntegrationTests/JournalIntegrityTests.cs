using Avila.Spike.Tenancy;
using Npgsql;

namespace Avila.IntegrationTests;

[Collection("postgres")]
public sealed class JournalIntegrityTests
{
    private readonly PostgresFixture _fixture;

    public JournalIntegrityTests(PostgresFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task T14_direct_sql_unbalanced_entry_is_rejected()
    {
        var tenant = Guid.NewGuid();
        var entryId = Guid.NewGuid();

        await using var connection = new NpgsqlConnection(_fixture.AppConnectionString);
        await connection.OpenAsync();
        await using var transaction = await connection.BeginTransactionAsync();
        await TenantSession.SetTenantAsync(connection, transaction, tenant);

        await using (var entry = new NpgsqlCommand(
            """
            INSERT INTO accounting.journal_entries
                (id, tenant_id, posting_key, source_type, source_id, entry_type, status)
            VALUES
                (@id, @tenant, @posting_key, 'manual-test', @source, 'original', 'posted')
            """,
            connection,
            transaction))
        {
            entry.Parameters.AddWithValue("id", entryId);
            entry.Parameters.AddWithValue("tenant", tenant);
            entry.Parameters.AddWithValue("posting_key", $"{tenant}:manual:{entryId}:1:original");
            entry.Parameters.AddWithValue("source", Guid.NewGuid());
            await entry.ExecuteNonQueryAsync();
        }

        await using (var line = new NpgsqlCommand(
            """
            INSERT INTO accounting.journal_lines
                (id, tenant_id, journal_entry_id, account_id, debit, credit)
            VALUES
                (@id, @tenant, @entry, @account, 100, 0)
            """,
            connection,
            transaction))
        {
            line.Parameters.AddWithValue("id", Guid.NewGuid());
            line.Parameters.AddWithValue("tenant", tenant);
            line.Parameters.AddWithValue("entry", entryId);
            line.Parameters.AddWithValue("account", Guid.NewGuid());
            await line.ExecuteNonQueryAsync();
        }

        var exception = await Assert.ThrowsAsync<PostgresException>(() => transaction.CommitAsync());

        Assert.Equal("P0001", exception.SqlState);
        Assert.Contains("unbalanced", exception.MessageText.ToLowerInvariant());
        Assert.Equal(0, await TestDb.CountJournalEntriesAsync(_fixture.AdminConnectionString, tenant));
    }
}
