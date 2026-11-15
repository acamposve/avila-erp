using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Avila.Modules.Inventory.Migrations
{
    /// <inheritdoc />
    public partial class WidenItemPricePrecision : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<decimal>(
                name: "unit_price",
                schema: "inventory",
                table: "items",
                type: "numeric(19,6)",
                precision: 19,
                scale: 6,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(19,4)",
                oldPrecision: 19,
                oldScale: 4);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<decimal>(
                name: "unit_price",
                schema: "inventory",
                table: "items",
                type: "numeric(19,4)",
                precision: 19,
                scale: 4,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(19,6)",
                oldPrecision: 19,
                oldScale: 6);
        }
    }
}
