using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Avila.Platform.Migrations
{
    /// <inheritdoc />
    public partial class AddOutboxEvents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "outbox_events",
                schema: "platform",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_type = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    payload_json = table.Column<string>(type: "jsonb", nullable: false),
                    correlation_id = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    occurred_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    processed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    attempts = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_outbox_events", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_outbox_events_tenant_id_processed_at_occurred_at",
                schema: "platform",
                table: "outbox_events",
                columns: new[] { "tenant_id", "processed_at", "occurred_at" });

            migrationBuilder.Sql(
                """
                ALTER TABLE platform.outbox_events ENABLE ROW LEVEL SECURITY;
                ALTER TABLE platform.outbox_events FORCE ROW LEVEL SECURITY;
                CREATE POLICY tenant_isolation ON platform.outbox_events
                    USING (tenant_id = nullif(current_setting('app.tenant_id', true), '')::uuid)
                    WITH CHECK (tenant_id = nullif(current_setting('app.tenant_id', true), '')::uuid);

                INSERT INTO platform.rls_policy_registry
                    (table_schema, table_name, tenant_column, policy_name, is_required, created_at)
                VALUES ('platform', 'outbox_events', 'tenant_id', 'tenant_isolation', true, now())
                ON CONFLICT DO NOTHING;

                COMMENT ON TABLE platform.outbox_events IS
                    'Transactional outbox per ADR-005; consumers must be idempotent.';
                """);        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "outbox_events",
                schema: "platform");
        }
    }
}
