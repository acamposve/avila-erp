-- ÁVILA ERP — least-privilege PostgreSQL role bootstrap.
--
-- This file contains NO passwords and is safe to commit. Roles are created
-- NOLOGIN; deployment/secret management grants LOGIN credentials separately.
-- It is idempotent and must run after EF migrations, on every environment.
--
-- Scope:
--   avila_app       tenant-scoped business tables + tenant-scoped platform tables
--   avila_identity  identity schema only
--   avila_platform  global tenant records + tenant lifecycle only

DO $roles$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'avila_app') THEN
        EXECUTE 'CREATE ROLE avila_app';
    END IF;
    IF NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'avila_identity') THEN
        EXECUTE 'CREATE ROLE avila_identity';
    END IF;
    IF NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'avila_platform') THEN
        EXECUTE 'CREATE ROLE avila_platform';
    END IF;
END
$roles$;

ALTER ROLE avila_app NOSUPERUSER NOBYPASSRLS NOCREATEDB NOCREATEROLE;
ALTER ROLE avila_identity NOSUPERUSER NOBYPASSRLS NOCREATEDB NOCREATEROLE;
ALTER ROLE avila_platform NOSUPERUSER NOBYPASSRLS NOCREATEDB NOCREATEROLE;

DO $connect$
BEGIN
    EXECUTE format(
        'GRANT CONNECT ON DATABASE %I TO avila_app, avila_identity, avila_platform',
        current_database());
END
$connect$;

REVOKE ALL ON SCHEMA admin, inventory, sales, billing, platform, identity FROM PUBLIC;
REVOKE ALL ON ALL TABLES IN SCHEMA admin, inventory, sales, billing, platform, identity FROM PUBLIC;

GRANT USAGE ON SCHEMA admin, inventory, sales, billing, platform TO avila_app;
GRANT USAGE ON SCHEMA identity TO avila_identity;
GRANT USAGE ON SCHEMA platform TO avila_platform;

-- Business tables: tenant RLS is forced by the migrations.
GRANT SELECT, INSERT, UPDATE, DELETE ON
    admin.companies,
    admin.branches,
    admin.parties,
    inventory.categories,
    inventory.warehouses,
    inventory.unit_of_measures,
    inventory.item_variants,
    inventory.bin_locations,
    inventory.items,
    sales.quotes,
    sales.quote_lines,
    sales.orders,
    sales.order_lines,
    billing.invoice_drafts,
    billing.invoice_draft_lines,
    platform.tenant_settings
TO avila_app;

-- Tenant-scoped platform tables. Audit and setting versions are append-only.
GRANT SELECT, INSERT ON platform.audit_events TO avila_app;
GRANT SELECT, INSERT ON platform.access_scopes TO avila_app;
GRANT SELECT, INSERT, UPDATE ON platform.support_access_grants TO avila_app;
GRANT SELECT, INSERT, UPDATE ON platform.document_sequences TO avila_app;
GRANT SELECT, INSERT, UPDATE ON platform.idempotency_keys TO avila_app;
GRANT SELECT, INSERT ON platform.setting_versions TO avila_app;
GRANT SELECT, INSERT ON platform.fiscal_rule_versions TO avila_app;
GRANT SELECT, INSERT ON platform.fiscal_document_snapshots TO avila_app;
GRANT SELECT, INSERT, UPDATE ON platform.outbox_events TO avila_app;
GRANT SELECT, INSERT, UPDATE ON platform.approval_rules TO avila_app;
GRANT SELECT, INSERT, UPDATE ON platform.approval_requests TO avila_app;
GRANT SELECT ON platform.tenant_lifecycle TO avila_app;
GRANT SELECT, INSERT, UPDATE ON platform.feature_flags TO avila_app;
GRANT SELECT, INSERT, UPDATE ON platform.usage_counters TO avila_app;

-- Identity schema is isolated from the app role.
GRANT SELECT, INSERT, UPDATE, DELETE ON ALL TABLES IN SCHEMA identity TO avila_identity;

-- Platform role can manage tenant records and lifecycle, nothing else.
GRANT SELECT, INSERT, UPDATE ON platform.tenants TO avila_platform;
GRANT SELECT, INSERT, UPDATE ON platform.tenant_lifecycle TO avila_platform;

COMMENT ON ROLE avila_app IS 'ÁVILA API tenant role; RLS enforced; no platform global tables.';
COMMENT ON ROLE avila_identity IS 'ÁVILA identity role; identity schema only.';
COMMENT ON ROLE avila_platform IS 'ÁVILA platform role; global tenants/lifecycle only.';
