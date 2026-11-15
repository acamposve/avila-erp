using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Avila.Modules.Administration.Migrations
{
    /// <inheritdoc />
    public partial class AddAdministrationVersion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "version",
                schema: "admin",
                table: "companies",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "version",
                schema: "admin",
                table: "branches",
                type: "integer",
                nullable: false,
                defaultValue: 1);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "version",
                schema: "admin",
                table: "companies");

            migrationBuilder.DropColumn(
                name: "version",
                schema: "admin",
                table: "branches");
        }
    }
}
