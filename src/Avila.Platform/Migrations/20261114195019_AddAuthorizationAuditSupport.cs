using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Avila.Platform.Migrations
{
    /// <inheritdoc />
    public partial class AddAuthorizationAuditSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "access_scopes",
                schema: "platform",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    scope_type = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    scope_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_access_scopes", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "audit_events",
                schema: "platform",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    actor_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    action = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    entity_type = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    entity_id = table.Column<Guid>(type: "uuid", nullable: true),
                    before_json = table.Column<string>(type: "jsonb", nullable: true),
                    after_json = table.Column<string>(type: "jsonb", nullable: true),
                    correlation_id = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    occurred_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_audit_events", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "support_access_grants",
                schema: "platform",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    support_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    ticket = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    justification = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    is_read_only = table.Column<bool>(type: "boolean", nullable: false),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    revoked_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_support_access_grants", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_access_scopes_tenant_id_user_id_scope_type_scope_id",
                schema: "platform",
                table: "access_scopes",
                columns: new[] { "tenant_id", "user_id", "scope_type", "scope_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_audit_events_tenant_id_occurred_at",
                schema: "platform",
                table: "audit_events",
                columns: new[] { "tenant_id", "occurred_at" });

            migrationBuilder.CreateIndex(
                name: "IX_support_access_grants_tenant_id_support_user_id_expires_at",
                schema: "platform",
                table: "support_access_grants",
                columns: new[] { "tenant_id", "support_user_id", "expires_at" });

            migrationBuilder.Sql(
                """
                ALTER TABLE platform.access_scopes ENABLE ROW LEVEL SECURITY;
                ALTER TABLE platform.access_scopes FORCE ROW LEVEL SECURITY;
                CREATE POLICY tenant_isolation ON platform.access_scopes
                    USING (tenant_id = nullif(current_setting('app.tenant_id', true), '')::uuid)
                    WITH CHECK (tenant_id = nullif(current_setting('app.tenant_id', true), '')::uuid);

                ALTER TABLE platform.audit_events ENABLE ROW LEVEL SECURITY;
                ALTER TABLE platform.audit_events FORCE ROW LEVEL SECURITY;
                CREATE POLICY tenant_isolation ON platform.audit_events
                    USING (tenant_id = nullif(current_setting('app.tenant_id', true), '')::uuid)
                    WITH CHECK (tenant_id = nullif(current_setting('app.tenant_id', true), '')::uuid);

                ALTER TABLE platform.support_access_grants ENABLE ROW LEVEL SECURITY;
                ALTER TABLE platform.support_access_grants FORCE ROW LEVEL SECURITY;
                CREATE POLICY tenant_isolation ON platform.support_access_grants
                    USING (tenant_id = nullif(current_setting('app.tenant_id', true), '')::uuid)
                    WITH CHECK (tenant_id = nullif(current_setting('app.tenant_id', true), '')::uuid);

                CREATE FUNCTION platform.reject_audit_mutation()
                RETURNS trigger
                LANGUAGE plpgsql
                AS $$
                BEGIN
                    RAISE EXCEPTION 'platform.audit_events is append-only';
                END;
                $$;

                CREATE TRIGGER audit_events_append_only
                    BEFORE UPDATE OR DELETE ON platform.audit_events
                    FOR EACH ROW EXECUTE FUNCTION platform.reject_audit_mutation();

                COMMENT ON TABLE platform.audit_events IS
                    'Append-only audit; INSERT/SELECT only for avila_app.';
                COMMENT ON TABLE platform.support_access_grants IS
                    'Time-boxed, read-only-by-default support access.';
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "access_scopes",
                schema: "platform");

            migrationBuilder.DropTable(
                name: "audit_events",
                schema: "platform");

            migrationBuilder.DropTable(
                name: "support_access_grants",
                schema: "platform");
        }
    }
}
