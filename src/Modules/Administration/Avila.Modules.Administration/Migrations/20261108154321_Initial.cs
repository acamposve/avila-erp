using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Avila.Modules.Administration.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "admin");

            migrationBuilder.CreateTable(
                name: "companies",
                schema: "admin",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    tax_id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_companies", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "parties",
                schema: "admin",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    tax_id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    is_customer = table.Column<bool>(type: "boolean", nullable: false),
                    is_supplier = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_parties", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_companies_tenant_id_tax_id",
                schema: "admin",
                table: "companies",
                columns: new[] { "tenant_id", "tax_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_parties_tenant_id_tax_id",
                schema: "admin",
                table: "parties",
                columns: new[] { "tenant_id", "tax_id" },
                unique: true);

            migrationBuilder.Sql(
                """
                ALTER TABLE admin.companies ENABLE ROW LEVEL SECURITY;
                ALTER TABLE admin.companies FORCE ROW LEVEL SECURITY;
                CREATE POLICY tenant_isolation ON admin.companies
                    USING (tenant_id = nullif(current_setting('app.tenant_id', true), '')::uuid)
                    WITH CHECK (tenant_id = nullif(current_setting('app.tenant_id', true), '')::uuid);

                ALTER TABLE admin.parties ENABLE ROW LEVEL SECURITY;
                ALTER TABLE admin.parties FORCE ROW LEVEL SECURITY;
                CREATE POLICY tenant_isolation ON admin.parties
                    USING (tenant_id = nullif(current_setting('app.tenant_id', true), '')::uuid)
                    WITH CHECK (tenant_id = nullif(current_setting('app.tenant_id', true), '')::uuid);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "companies",
                schema: "admin");

            migrationBuilder.DropTable(
                name: "parties",
                schema: "admin");
        }
    }
}
