using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Avila.Platform.Migrations
{
    /// <inheritdoc />
    public partial class AddTenantLifecycleLimitsFlags : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "feature_flags",
                schema: "platform",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    key = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    enabled = table.Column<bool>(type: "boolean", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_feature_flags", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "tenant_lifecycle",
                schema: "platform",
                columns: table => new
                {
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    plan_code = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    suspended_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    suspension_reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tenant_lifecycle", x => x.tenant_id);
                });

            migrationBuilder.CreateTable(
                name: "usage_counters",
                schema: "platform",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    resource = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    used = table.Column<long>(type: "bigint", nullable: false),
                    limit_value = table.Column<long>(type: "bigint", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_usage_counters", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_feature_flags_tenant_id_key",
                schema: "platform",
                table: "feature_flags",
                columns: new[] { "tenant_id", "key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_usage_counters_tenant_id_resource",
                schema: "platform",
                table: "usage_counters",
                columns: new[] { "tenant_id", "resource" },
                unique: true);

            migrationBuilder.Sql(
                """
                ALTER TABLE platform.tenant_lifecycle ENABLE ROW LEVEL SECURITY;
                ALTER TABLE platform.tenant_lifecycle FORCE ROW LEVEL SECURITY;
                CREATE POLICY tenant_isolation ON platform.tenant_lifecycle
                    USING (tenant_id = nullif(current_setting('app.tenant_id', true), '')::uuid)
                    WITH CHECK (tenant_id = nullif(current_setting('app.tenant_id', true), '')::uuid);

                ALTER TABLE platform.feature_flags ENABLE ROW LEVEL SECURITY;
                ALTER TABLE platform.feature_flags FORCE ROW LEVEL SECURITY;
                CREATE POLICY tenant_isolation ON platform.feature_flags
                    USING (tenant_id = nullif(current_setting('app.tenant_id', true), '')::uuid)
                    WITH CHECK (tenant_id = nullif(current_setting('app.tenant_id', true), '')::uuid);

                ALTER TABLE platform.usage_counters ENABLE ROW LEVEL SECURITY;
                ALTER TABLE platform.usage_counters FORCE ROW LEVEL SECURITY;
                CREATE POLICY tenant_isolation ON platform.usage_counters
                    USING (tenant_id = nullif(current_setting('app.tenant_id', true), '')::uuid)
                    WITH CHECK (tenant_id = nullif(current_setting('app.tenant_id', true), '')::uuid);

                COMMENT ON TABLE platform.tenant_lifecycle IS
                    'Tenant status, plan code and suspension metadata.';
                COMMENT ON TABLE platform.feature_flags IS
                    'Tenant-scoped feature flags.';
                COMMENT ON TABLE platform.usage_counters IS
                    'Tenant usage and limit counters.';
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "feature_flags",
                schema: "platform");

            migrationBuilder.DropTable(
                name: "tenant_lifecycle",
                schema: "platform");

            migrationBuilder.DropTable(
                name: "usage_counters",
                schema: "platform");
        }
    }
}
