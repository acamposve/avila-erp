using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Avila.Platform.Migrations
{
    /// <inheritdoc />
    public partial class AddRlsPolicyRegistry : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "rls_policy_registry",
                schema: "platform",
                columns: table => new
                {
                    table_schema = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    table_name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    tenant_column = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    policy_name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    is_required = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_rls_policy_registry", x => new { x.table_schema, x.table_name });
                });

            migrationBuilder.Sql(
                """
                INSERT INTO platform.rls_policy_registry
                    (table_schema, table_name, tenant_column, policy_name, is_required, created_at)
                SELECT c.table_schema, c.table_name, 'tenant_id', 'tenant_isolation', true, now()
                FROM information_schema.columns AS c
                JOIN information_schema.tables AS t
                  ON t.table_schema = c.table_schema
                 AND t.table_name = c.table_name
                 AND t.table_type = 'BASE TABLE'
                WHERE c.column_name = 'tenant_id'
                  AND c.table_schema IN ('admin', 'inventory', 'sales', 'billing', 'platform')
                ON CONFLICT (table_schema, table_name) DO NOTHING;

                COMMENT ON TABLE platform.rls_policy_registry IS
                    'Inventory of tenant-scoped tables; CI cross-checks catalog and policies.';
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "rls_policy_registry",
                schema: "platform");
        }
    }
}
