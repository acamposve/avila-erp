using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Avila.Modules.Administration.Migrations
{
    /// <inheritdoc />
    public partial class AddBranchesAndFiscalProfile : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "fiscal_address",
                schema: "admin",
                table: "companies",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "fiscal_profile_complete",
                schema: "admin",
                table: "companies",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "branches",
                schema: "admin",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    address = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_branches", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_branches_tenant_id_company_id_code",
                schema: "admin",
                table: "branches",
                columns: new[] { "tenant_id", "company_id", "code" },
                unique: true);

            migrationBuilder.Sql(
                """
                ALTER TABLE admin.branches ENABLE ROW LEVEL SECURITY;
                ALTER TABLE admin.branches FORCE ROW LEVEL SECURITY;
                CREATE POLICY tenant_isolation ON admin.branches
                    USING (tenant_id = nullif(current_setting('app.tenant_id', true), '')::uuid)
                    WITH CHECK (tenant_id = nullif(current_setting('app.tenant_id', true), '')::uuid);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "branches",
                schema: "admin");

            migrationBuilder.DropColumn(
                name: "fiscal_address",
                schema: "admin",
                table: "companies");

            migrationBuilder.DropColumn(
                name: "fiscal_profile_complete",
                schema: "admin",
                table: "companies");
        }
    }
}
