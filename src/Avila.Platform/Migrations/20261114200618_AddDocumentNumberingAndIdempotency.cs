using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Avila.Platform.Migrations
{
    /// <inheritdoc />
    public partial class AddDocumentNumberingAndIdempotency : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "document_sequences",
                schema: "platform",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    scope_type = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    scope_id = table.Column<Guid>(type: "uuid", nullable: false),
                    document_type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    prefix = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    next_value = table.Column<long>(type: "bigint", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_document_sequences", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "idempotency_keys",
                schema: "platform",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    key = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    operation = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    request_hash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    status = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    response_json = table.Column<string>(type: "jsonb", nullable: true),
                    status_code = table.Column<int>(type: "integer", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    completed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_idempotency_keys", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_document_sequences_tenant_id_scope_type_scope_id_document_t~",
                schema: "platform",
                table: "document_sequences",
                columns: new[] { "tenant_id", "scope_type", "scope_id", "document_type" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_idempotency_keys_tenant_id_operation_key",
                schema: "platform",
                table: "idempotency_keys",
                columns: new[] { "tenant_id", "operation", "key" },
                unique: true);

            migrationBuilder.Sql(
                """
                ALTER TABLE platform.document_sequences ENABLE ROW LEVEL SECURITY;
                ALTER TABLE platform.document_sequences FORCE ROW LEVEL SECURITY;
                CREATE POLICY tenant_isolation ON platform.document_sequences
                    USING (tenant_id = nullif(current_setting('app.tenant_id', true), '')::uuid)
                    WITH CHECK (tenant_id = nullif(current_setting('app.tenant_id', true), '')::uuid);

                ALTER TABLE platform.idempotency_keys ENABLE ROW LEVEL SECURITY;
                ALTER TABLE platform.idempotency_keys FORCE ROW LEVEL SECURITY;
                CREATE POLICY tenant_isolation ON platform.idempotency_keys
                    USING (tenant_id = nullif(current_setting('app.tenant_id', true), '')::uuid)
                    WITH CHECK (tenant_id = nullif(current_setting('app.tenant_id', true), '')::uuid);

                COMMENT ON TABLE platform.document_sequences IS
                    'Gapless document sequence per tenant/scope/type; row lock only.';
                COMMENT ON TABLE platform.idempotency_keys IS
                    'Transactional idempotency ledger; unique tenant/operation/key.';
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "document_sequences",
                schema: "platform");

            migrationBuilder.DropTable(
                name: "idempotency_keys",
                schema: "platform");
        }
    }
}
