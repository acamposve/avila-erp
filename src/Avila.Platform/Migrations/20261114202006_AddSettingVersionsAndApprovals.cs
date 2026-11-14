using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Avila.Platform.Migrations
{
    /// <inheritdoc />
    public partial class AddSettingVersionsAndApprovals : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "approval_requests",
                schema: "platform",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    entity_id = table.Column<Guid>(type: "uuid", nullable: false),
                    amount = table.Column<decimal>(type: "numeric(19,4)", precision: 19, scale: 4, nullable: false),
                    status = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    requested_by = table.Column<Guid>(type: "uuid", nullable: false),
                    decided_by = table.Column<Guid>(type: "uuid", nullable: true),
                    decided_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_approval_requests", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "approval_rules",
                schema: "platform",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    threshold_amount = table.Column<decimal>(type: "numeric(19,4)", precision: 19, scale: 4, nullable: false),
                    required_role = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_approval_rules", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "setting_versions",
                schema: "platform",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    key = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    value = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    source = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    effective_from = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    effective_to = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_by = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_setting_versions", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_approval_requests_tenant_id_type_entity_id_status",
                schema: "platform",
                table: "approval_requests",
                columns: new[] { "tenant_id", "type", "entity_id", "status" });

            migrationBuilder.CreateIndex(
                name: "IX_approval_rules_tenant_id_type",
                schema: "platform",
                table: "approval_rules",
                columns: new[] { "tenant_id", "type" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_setting_versions_tenant_id_key_effective_from",
                schema: "platform",
                table: "setting_versions",
                columns: new[] { "tenant_id", "key", "effective_from" });

            migrationBuilder.Sql(
                """
                ALTER TABLE platform.setting_versions ENABLE ROW LEVEL SECURITY;
                ALTER TABLE platform.setting_versions FORCE ROW LEVEL SECURITY;
                CREATE POLICY tenant_isolation ON platform.setting_versions
                    USING (tenant_id = nullif(current_setting('app.tenant_id', true), '')::uuid)
                    WITH CHECK (tenant_id = nullif(current_setting('app.tenant_id', true), '')::uuid);

                ALTER TABLE platform.approval_rules ENABLE ROW LEVEL SECURITY;
                ALTER TABLE platform.approval_rules FORCE ROW LEVEL SECURITY;
                CREATE POLICY tenant_isolation ON platform.approval_rules
                    USING (tenant_id = nullif(current_setting('app.tenant_id', true), '')::uuid)
                    WITH CHECK (tenant_id = nullif(current_setting('app.tenant_id', true), '')::uuid);

                ALTER TABLE platform.approval_requests ENABLE ROW LEVEL SECURITY;
                ALTER TABLE platform.approval_requests FORCE ROW LEVEL SECURITY;
                CREATE POLICY tenant_isolation ON platform.approval_requests
                    USING (tenant_id = nullif(current_setting('app.tenant_id', true), '')::uuid)
                    WITH CHECK (tenant_id = nullif(current_setting('app.tenant_id', true), '')::uuid);

                CREATE FUNCTION platform.reject_setting_version_mutation()
                RETURNS trigger
                LANGUAGE plpgsql
                AS $$
                BEGIN
                    RAISE EXCEPTION 'platform.setting_versions is append-only';
                END;
                $$;

                CREATE TRIGGER setting_versions_append_only
                    BEFORE UPDATE OR DELETE ON platform.setting_versions
                    FOR EACH ROW EXECUTE FUNCTION platform.reject_setting_version_mutation();

                COMMENT ON TABLE platform.setting_versions IS
                    'Immutable parameter versions with effective dates and source.';
                COMMENT ON TABLE platform.approval_rules IS
                    'Tenant approval rule by type and amount.';
                COMMENT ON TABLE platform.approval_requests IS
                    'Approval request and decision; requester must differ from approver.';
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "approval_requests",
                schema: "platform");

            migrationBuilder.DropTable(
                name: "approval_rules",
                schema: "platform");

            migrationBuilder.DropTable(
                name: "setting_versions",
                schema: "platform");
        }
    }
}
