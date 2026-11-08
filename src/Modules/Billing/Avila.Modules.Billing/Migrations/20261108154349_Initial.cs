using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Avila.Modules.Billing.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "billing");

            migrationBuilder.CreateTable(
                name: "invoice_drafts",
                schema: "billing",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    order_id = table.Column<Guid>(type: "uuid", nullable: false),
                    party_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    subtotal = table.Column<decimal>(type: "numeric(19,4)", precision: 19, scale: 4, nullable: false),
                    tax_total = table.Column<decimal>(type: "numeric(19,4)", precision: 19, scale: 4, nullable: false),
                    total = table.Column<decimal>(type: "numeric(19,4)", precision: 19, scale: 4, nullable: false),
                    currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_invoice_drafts", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "invoice_draft_lines",
                schema: "billing",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    invoice_draft_id = table.Column<Guid>(type: "uuid", nullable: false),
                    item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    description = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: false),
                    quantity = table.Column<decimal>(type: "numeric(19,6)", precision: 19, scale: 6, nullable: false),
                    unit_price = table.Column<decimal>(type: "numeric(19,4)", precision: 19, scale: 4, nullable: false),
                    tax_rate = table.Column<decimal>(type: "numeric(9,6)", precision: 9, scale: 6, nullable: false),
                    line_total = table.Column<decimal>(type: "numeric(19,4)", precision: 19, scale: 4, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_invoice_draft_lines", x => x.id);
                    table.ForeignKey(
                        name: "FK_invoice_draft_lines_invoice_drafts_invoice_draft_id",
                        column: x => x.invoice_draft_id,
                        principalSchema: "billing",
                        principalTable: "invoice_drafts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_invoice_draft_lines_invoice_draft_id",
                schema: "billing",
                table: "invoice_draft_lines",
                column: "invoice_draft_id");

            migrationBuilder.CreateIndex(
                name: "IX_invoice_drafts_tenant_id_order_id",
                schema: "billing",
                table: "invoice_drafts",
                columns: new[] { "tenant_id", "order_id" },
                unique: true);

            migrationBuilder.Sql(
                """
                ALTER TABLE billing.invoice_drafts ENABLE ROW LEVEL SECURITY;
                ALTER TABLE billing.invoice_drafts FORCE ROW LEVEL SECURITY;
                CREATE POLICY tenant_isolation ON billing.invoice_drafts
                    USING (tenant_id = nullif(current_setting('app.tenant_id', true), '')::uuid)
                    WITH CHECK (tenant_id = nullif(current_setting('app.tenant_id', true), '')::uuid);

                ALTER TABLE billing.invoice_draft_lines ENABLE ROW LEVEL SECURITY;
                ALTER TABLE billing.invoice_draft_lines FORCE ROW LEVEL SECURITY;
                CREATE POLICY tenant_isolation ON billing.invoice_draft_lines
                    USING (tenant_id = nullif(current_setting('app.tenant_id', true), '')::uuid)
                    WITH CHECK (tenant_id = nullif(current_setting('app.tenant_id', true), '')::uuid);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "invoice_draft_lines",
                schema: "billing");

            migrationBuilder.DropTable(
                name: "invoice_drafts",
                schema: "billing");
        }
    }
}
