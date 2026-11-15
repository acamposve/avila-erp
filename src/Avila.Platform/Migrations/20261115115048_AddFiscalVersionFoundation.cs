using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Avila.Platform.Migrations
{
    /// <inheritdoc />
    public partial class AddFiscalVersionFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "fiscal_rule_versions",
                schema: "platform",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    rule_key = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    version_number = table.Column<int>(type: "integer", nullable: false),
                    value_json = table.Column<string>(type: "character varying(8000)", maxLength: 8000, nullable: false),
                    source = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    effective_from = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    effective_to = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_by = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_fiscal_rule_versions", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "fiscal_document_snapshots",
                schema: "platform",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    document_type = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    document_id = table.Column<Guid>(type: "uuid", nullable: false),
                    rule_key = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    fiscal_rule_version_id = table.Column<Guid>(type: "uuid", nullable: false),
                    rule_version = table.Column<int>(type: "integer", nullable: false),
                    captured_by = table.Column<Guid>(type: "uuid", nullable: false),
                    captured_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_fiscal_document_snapshots", x => x.id);
                    table.ForeignKey(
                        name: "FK_fiscal_document_snapshots_fiscal_rule_versions_fiscal_rule_~",
                        column: x => x.fiscal_rule_version_id,
                        principalSchema: "platform",
                        principalTable: "fiscal_rule_versions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_fiscal_document_snapshots_fiscal_rule_version_id",
                schema: "platform",
                table: "fiscal_document_snapshots",
                column: "fiscal_rule_version_id");

            migrationBuilder.CreateIndex(
                name: "IX_fiscal_document_snapshots_tenant_id_document_type_document_~",
                schema: "platform",
                table: "fiscal_document_snapshots",
                columns: new[] { "tenant_id", "document_type", "document_id", "rule_key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_fiscal_rule_versions_tenant_id_rule_key_effective_from",
                schema: "platform",
                table: "fiscal_rule_versions",
                columns: new[] { "tenant_id", "rule_key", "effective_from" });

            migrationBuilder.CreateIndex(
                name: "IX_fiscal_rule_versions_tenant_id_rule_key_version_number",
                schema: "platform",
                table: "fiscal_rule_versions",
                columns: new[] { "tenant_id", "rule_key", "version_number" },
                unique: true);

            migrationBuilder.Sql(
                """
                ALTER TABLE platform.fiscal_rule_versions ENABLE ROW LEVEL SECURITY;
                ALTER TABLE platform.fiscal_rule_versions FORCE ROW LEVEL SECURITY;
                CREATE POLICY tenant_isolation ON platform.fiscal_rule_versions
                    USING (tenant_id = nullif(current_setting('app.tenant_id', true), '')::uuid)
                    WITH CHECK (tenant_id = nullif(current_setting('app.tenant_id', true), '')::uuid);

                ALTER TABLE platform.fiscal_document_snapshots ENABLE ROW LEVEL SECURITY;
                ALTER TABLE platform.fiscal_document_snapshots FORCE ROW LEVEL SECURITY;
                CREATE POLICY tenant_isolation ON platform.fiscal_document_snapshots
                    USING (tenant_id = nullif(current_setting('app.tenant_id', true), '')::uuid)
                    WITH CHECK (tenant_id = nullif(current_setting('app.tenant_id', true), '')::uuid);

                CREATE FUNCTION platform.reject_fiscal_version_mutation()
                RETURNS trigger AS $$
                BEGIN
                    RAISE EXCEPTION 'platform.fiscal_rule_versions is append-only';
                END;
                $$ LANGUAGE plpgsql;

                CREATE TRIGGER fiscal_rule_versions_append_only
                    BEFORE UPDATE OR DELETE ON platform.fiscal_rule_versions
                    FOR EACH ROW EXECUTE FUNCTION platform.reject_fiscal_version_mutation();

                CREATE FUNCTION platform.reject_fiscal_snapshot_mutation()
                RETURNS trigger AS $$
                BEGIN
                    RAISE EXCEPTION 'platform.fiscal_document_snapshots is append-only';
                END;
                $$ LANGUAGE plpgsql;

                CREATE TRIGGER fiscal_document_snapshots_append_only
                    BEFORE UPDATE OR DELETE ON platform.fiscal_document_snapshots
                    FOR EACH ROW EXECUTE FUNCTION platform.reject_fiscal_snapshot_mutation();

                INSERT INTO platform.rls_policy_registry
                    (table_schema, table_name, tenant_column, policy_name, is_required, created_at)
                VALUES
                    ('platform', 'fiscal_rule_versions', 'tenant_id', 'tenant_isolation', true, now()),
                    ('platform', 'fiscal_document_snapshots', 'tenant_id', 'tenant_isolation', true, now())
                ON CONFLICT DO NOTHING;

                COMMENT ON TABLE platform.fiscal_rule_versions IS
                    'Immutable, tenant-scoped fiscal rule versions; values are data-driven JSON.';
                COMMENT ON TABLE platform.fiscal_document_snapshots IS
                    'Historical fiscal rule version captured on a document.';
                """);        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "fiscal_document_snapshots",
                schema: "platform");

            migrationBuilder.DropTable(
                name: "fiscal_rule_versions",
                schema: "platform");
        }
    }
}
