using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Avila.Platform.Migrations
{
    /// <inheritdoc />
    public partial class AddAuditHashChain : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "event_hash",
                schema: "platform",
                table: "audit_events",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "hash_version",
                schema: "platform",
                table: "audit_events",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<string>(
                name: "previous_hash",
                schema: "platform",
                table: "audit_events",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE platform.audit_events
                SET hash_version = 0
                WHERE event_hash IS NULL;

                COMMENT ON COLUMN platform.audit_events.hash_version IS
                    '0 = legacy pre-chain row; 1 = SHA-256 chained row.';
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "event_hash",
                schema: "platform",
                table: "audit_events");

            migrationBuilder.DropColumn(
                name: "hash_version",
                schema: "platform",
                table: "audit_events");

            migrationBuilder.DropColumn(
                name: "previous_hash",
                schema: "platform",
                table: "audit_events");
        }
    }
}
