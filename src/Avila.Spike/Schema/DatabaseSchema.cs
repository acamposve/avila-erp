namespace Avila.Spike.Schema;

/// <summary>
/// Minimal schema for the architecture spike. This is NOT a production migration.
/// It exists only to prove RLS, shared transactions, and aggregate locks against PostgreSQL.
/// </summary>
public static class DatabaseSchema
{
    public const string Ddl = """
        CREATE ROLE avila_app LOGIN PASSWORD 'avila_app_pw' NOSUPERUSER NOBYPASSRLS;
        GRANT CONNECT ON DATABASE avila TO avila_app;

        CREATE SCHEMA billing;
        CREATE SCHEMA treasury;
        CREATE SCHEMA inventory;
        CREATE SCHEMA accounting;
        CREATE SCHEMA platform;

        CREATE TABLE billing.invoices (
            id uuid PRIMARY KEY,
            tenant_id uuid NOT NULL,
            company_id uuid NOT NULL,
            number bigint NOT NULL,
            status text NOT NULL,
            accounting_status text NOT NULL DEFAULT 'not_applicable',
            journal_entry_id uuid NULL,
            created_at timestamptz NOT NULL DEFAULT now()
        );
        CREATE UNIQUE INDEX ux_invoices_number
            ON billing.invoices (tenant_id, company_id, number);

        CREATE TABLE treasury.receivables (
            id uuid PRIMARY KEY,
            tenant_id uuid NOT NULL,
            customer_id uuid NOT NULL,
            invoice_id uuid NULL,
            original_amount numeric(19,4) NOT NULL,
            applied_amount numeric(19,4) NOT NULL DEFAULT 0,
            currency char(3) NOT NULL,
            status text NOT NULL,
            CONSTRAINT ck_receivable_original_positive CHECK (original_amount > 0),
            CONSTRAINT ck_receivable_applied_nonnegative CHECK (applied_amount >= 0)
        );
        CREATE INDEX ix_receivables_tenant ON treasury.receivables (tenant_id, id);

        CREATE TABLE treasury.payment_allocations (
            id uuid PRIMARY KEY,
            tenant_id uuid NOT NULL,
            payment_id uuid NOT NULL,
            receivable_id uuid NOT NULL,
            amount numeric(19,4) NOT NULL,
            allocation_type text NOT NULL DEFAULT 'application',
            created_at timestamptz NOT NULL DEFAULT now(),
            CONSTRAINT ck_allocation_amount_positive CHECK (amount > 0)
        );
        CREATE UNIQUE INDEX ux_allocation_once
            ON treasury.payment_allocations (tenant_id, payment_id, receivable_id, allocation_type);

        CREATE TABLE inventory.stock_balances (
            tenant_id uuid NOT NULL,
            warehouse_id uuid NOT NULL,
            item_id uuid NOT NULL,
            quantity numeric(19,6) NOT NULL,
            reserved numeric(19,6) NOT NULL DEFAULT 0,
            average_cost numeric(19,6) NOT NULL DEFAULT 0,
            allow_negative boolean NOT NULL DEFAULT false,
            updated_at timestamptz NOT NULL DEFAULT now(),
            PRIMARY KEY (tenant_id, warehouse_id, item_id),
            CONSTRAINT ck_stock_reserved_nonnegative CHECK (reserved >= 0),
            CONSTRAINT ck_stock_quantity_policy CHECK (quantity >= 0 OR allow_negative)
        );

        CREATE TABLE inventory.stock_movements (
            id uuid PRIMARY KEY,
            tenant_id uuid NOT NULL,
            warehouse_id uuid NOT NULL,
            item_id uuid NOT NULL,
            quantity numeric(19,6) NOT NULL,
            unit_cost numeric(19,6) NOT NULL,
            source_type text NOT NULL,
            source_id uuid NOT NULL,
            source_line_id uuid NOT NULL,
            movement_type text NOT NULL,
            created_at timestamptz NOT NULL DEFAULT now()
        );
        CREATE UNIQUE INDEX ux_movement_source
            ON inventory.stock_movements (tenant_id, source_type, source_id, source_line_id, movement_type);
        CREATE INDEX ix_movements_tenant_item
            ON inventory.stock_movements (tenant_id, warehouse_id, item_id);

        CREATE TABLE platform.tenant_notes (
            id uuid PRIMARY KEY,
            tenant_id uuid NOT NULL,
            note text NOT NULL,
            created_at timestamptz NOT NULL DEFAULT now()
        );

        CREATE TABLE platform.document_sequences (
            tenant_id uuid NOT NULL,
            company_id uuid NOT NULL,
            scope_type text NOT NULL,
            scope_id uuid NOT NULL,
            document_type text NOT NULL,
            series text NOT NULL,
            fiscal_year int NOT NULL,
            next_value bigint NOT NULL,
            updated_at timestamptz NOT NULL DEFAULT now(),
            PRIMARY KEY (tenant_id, company_id, scope_type, scope_id, document_type, series, fiscal_year),
            CONSTRAINT ck_sequence_scope CHECK (scope_type IN ('company', 'branch'))
        );

        CREATE TABLE platform.idempotency_records (
            tenant_id uuid NOT NULL,
            idempotency_key text NOT NULL,
            operation text NOT NULL,
            request_hash text NOT NULL,
            status text NOT NULL,
            status_code int NULL,
            response_body text NULL,
            created_at timestamptz NOT NULL DEFAULT now(),
            expires_at timestamptz NOT NULL,
            PRIMARY KEY (tenant_id, idempotency_key, operation)
        );

        CREATE TABLE accounting.accounts (
            id uuid PRIMARY KEY,
            tenant_id uuid NOT NULL,
            code text NOT NULL,
            name text NOT NULL,
            is_postable boolean NOT NULL DEFAULT true,
            UNIQUE (tenant_id, code)
        );

        CREATE TABLE accounting.journal_entries (
            id uuid PRIMARY KEY,
            tenant_id uuid NOT NULL,
            posting_key text NOT NULL,
            source_type text NOT NULL,
            source_id uuid NOT NULL,
            entry_type text NOT NULL,
            status text NOT NULL DEFAULT 'posted',
            created_at timestamptz NOT NULL DEFAULT now(),
            UNIQUE (tenant_id, posting_key)
        );

        CREATE TABLE accounting.journal_lines (
            id uuid PRIMARY KEY,
            tenant_id uuid NOT NULL,
            journal_entry_id uuid NOT NULL,
            account_id uuid NOT NULL,
            debit numeric(19,4) NOT NULL DEFAULT 0,
            credit numeric(19,4) NOT NULL DEFAULT 0,
            CONSTRAINT ck_journal_line_side CHECK (debit >= 0 AND credit >= 0 AND (debit = 0 OR credit = 0)),
            CONSTRAINT ck_journal_line_amount CHECK (debit > 0 OR credit > 0)
        );

        CREATE TABLE accounting.periods (
            tenant_id uuid NOT NULL,
            company_id uuid NOT NULL,
            period_id uuid NOT NULL,
            status text NOT NULL DEFAULT 'open',
            PRIMARY KEY (tenant_id, company_id, period_id),
            CONSTRAINT ck_period_status CHECK (status IN ('open', 'closing', 'closed', 'reopening'))
        );

        CREATE TABLE accounting.company_settings (
            tenant_id uuid NOT NULL,
            company_id uuid NOT NULL,
            accounting_enabled boolean NOT NULL DEFAULT false,
            cutover_id uuid NULL,
            PRIMARY KEY (tenant_id, company_id)
        );

        CREATE TABLE accounting.cutover_runs (
            id uuid PRIMARY KEY,
            tenant_id uuid NOT NULL,
            company_id uuid NOT NULL,
            cutover_date date NOT NULL,
            status text NOT NULL DEFAULT 'prepared',
            created_at timestamptz NOT NULL DEFAULT now(),
            activated_at timestamptz NULL,
            CONSTRAINT ck_cutover_status CHECK (status IN ('prepared', 'reconciled', 'active', 'failed'))
        );

        CREATE TABLE accounting.opening_links (
            id uuid PRIMARY KEY,
            tenant_id uuid NOT NULL,
            cutover_id uuid NOT NULL,
            source_type text NOT NULL,
            source_id uuid NOT NULL,
            journal_entry_id uuid NOT NULL,
            amount numeric(19,4) NOT NULL
        );

        CREATE FUNCTION accounting.assert_journal_balanced()
        RETURNS trigger
        LANGUAGE plpgsql
        AS $$
        DECLARE
            v_tenant uuid;
            v_entry uuid;
            v_debit numeric;
            v_credit numeric;
        BEGIN
            v_tenant := NEW.tenant_id;
            v_entry := NEW.journal_entry_id;

            SELECT COALESCE(SUM(debit), 0), COALESCE(SUM(credit), 0)
            INTO v_debit, v_credit
            FROM accounting.journal_lines
            WHERE tenant_id = v_tenant AND journal_entry_id = v_entry;

            IF v_debit <> v_credit THEN
                RAISE EXCEPTION 'Journal entry % is unbalanced: debit %, credit %',
                    v_entry, v_debit, v_credit;
            END IF;

            RETURN NULL;
        END;
        $$;

        CREATE CONSTRAINT TRIGGER trg_journal_balanced
        AFTER INSERT OR UPDATE ON accounting.journal_lines
        DEFERRABLE INITIALLY DEFERRED
        FOR EACH ROW
        EXECUTE FUNCTION accounting.assert_journal_balanced();

        ALTER TABLE billing.invoices ENABLE ROW LEVEL SECURITY;
        ALTER TABLE billing.invoices FORCE ROW LEVEL SECURITY;
        CREATE POLICY tenant_isolation ON billing.invoices
            USING (tenant_id = nullif(current_setting('app.tenant_id', true), '')::uuid)
            WITH CHECK (tenant_id = nullif(current_setting('app.tenant_id', true), '')::uuid);

        ALTER TABLE treasury.receivables ENABLE ROW LEVEL SECURITY;
        ALTER TABLE treasury.receivables FORCE ROW LEVEL SECURITY;
        CREATE POLICY tenant_isolation ON treasury.receivables
            USING (tenant_id = nullif(current_setting('app.tenant_id', true), '')::uuid)
            WITH CHECK (tenant_id = nullif(current_setting('app.tenant_id', true), '')::uuid);

        ALTER TABLE treasury.payment_allocations ENABLE ROW LEVEL SECURITY;
        ALTER TABLE treasury.payment_allocations FORCE ROW LEVEL SECURITY;
        CREATE POLICY tenant_isolation ON treasury.payment_allocations
            USING (tenant_id = nullif(current_setting('app.tenant_id', true), '')::uuid)
            WITH CHECK (tenant_id = nullif(current_setting('app.tenant_id', true), '')::uuid);

        ALTER TABLE inventory.stock_balances ENABLE ROW LEVEL SECURITY;
        ALTER TABLE inventory.stock_balances FORCE ROW LEVEL SECURITY;
        CREATE POLICY tenant_isolation ON inventory.stock_balances
            USING (tenant_id = nullif(current_setting('app.tenant_id', true), '')::uuid)
            WITH CHECK (tenant_id = nullif(current_setting('app.tenant_id', true), '')::uuid);

        ALTER TABLE inventory.stock_movements ENABLE ROW LEVEL SECURITY;
        ALTER TABLE inventory.stock_movements FORCE ROW LEVEL SECURITY;
        CREATE POLICY tenant_isolation ON inventory.stock_movements
            USING (tenant_id = nullif(current_setting('app.tenant_id', true), '')::uuid)
            WITH CHECK (tenant_id = nullif(current_setting('app.tenant_id', true), '')::uuid);

        ALTER TABLE platform.tenant_notes ENABLE ROW LEVEL SECURITY;
        ALTER TABLE platform.tenant_notes FORCE ROW LEVEL SECURITY;
        CREATE POLICY tenant_isolation ON platform.tenant_notes
            USING (tenant_id = nullif(current_setting('app.tenant_id', true), '')::uuid)
            WITH CHECK (tenant_id = nullif(current_setting('app.tenant_id', true), '')::uuid);

        ALTER TABLE platform.document_sequences ENABLE ROW LEVEL SECURITY;
        ALTER TABLE platform.document_sequences FORCE ROW LEVEL SECURITY;
        CREATE POLICY tenant_isolation ON platform.document_sequences
            USING (tenant_id = nullif(current_setting('app.tenant_id', true), '')::uuid)
            WITH CHECK (tenant_id = nullif(current_setting('app.tenant_id', true), '')::uuid);

        ALTER TABLE platform.idempotency_records ENABLE ROW LEVEL SECURITY;
        ALTER TABLE platform.idempotency_records FORCE ROW LEVEL SECURITY;
        CREATE POLICY tenant_isolation ON platform.idempotency_records
            USING (tenant_id = nullif(current_setting('app.tenant_id', true), '')::uuid)
            WITH CHECK (tenant_id = nullif(current_setting('app.tenant_id', true), '')::uuid);

        ALTER TABLE accounting.accounts ENABLE ROW LEVEL SECURITY;
        ALTER TABLE accounting.accounts FORCE ROW LEVEL SECURITY;
        CREATE POLICY tenant_isolation ON accounting.accounts
            USING (tenant_id = nullif(current_setting('app.tenant_id', true), '')::uuid)
            WITH CHECK (tenant_id = nullif(current_setting('app.tenant_id', true), '')::uuid);

        ALTER TABLE accounting.journal_entries ENABLE ROW LEVEL SECURITY;
        ALTER TABLE accounting.journal_entries FORCE ROW LEVEL SECURITY;
        CREATE POLICY tenant_isolation ON accounting.journal_entries
            USING (tenant_id = nullif(current_setting('app.tenant_id', true), '')::uuid)
            WITH CHECK (tenant_id = nullif(current_setting('app.tenant_id', true), '')::uuid);

        ALTER TABLE accounting.journal_lines ENABLE ROW LEVEL SECURITY;
        ALTER TABLE accounting.journal_lines FORCE ROW LEVEL SECURITY;
        CREATE POLICY tenant_isolation ON accounting.journal_lines
            USING (tenant_id = nullif(current_setting('app.tenant_id', true), '')::uuid)
            WITH CHECK (tenant_id = nullif(current_setting('app.tenant_id', true), '')::uuid);

        ALTER TABLE accounting.periods ENABLE ROW LEVEL SECURITY;
        ALTER TABLE accounting.periods FORCE ROW LEVEL SECURITY;
        CREATE POLICY tenant_isolation ON accounting.periods
            USING (tenant_id = nullif(current_setting('app.tenant_id', true), '')::uuid)
            WITH CHECK (tenant_id = nullif(current_setting('app.tenant_id', true), '')::uuid);

        ALTER TABLE accounting.company_settings ENABLE ROW LEVEL SECURITY;
        ALTER TABLE accounting.company_settings FORCE ROW LEVEL SECURITY;
        CREATE POLICY tenant_isolation ON accounting.company_settings
            USING (tenant_id = nullif(current_setting('app.tenant_id', true), '')::uuid)
            WITH CHECK (tenant_id = nullif(current_setting('app.tenant_id', true), '')::uuid);

        ALTER TABLE accounting.cutover_runs ENABLE ROW LEVEL SECURITY;
        ALTER TABLE accounting.cutover_runs FORCE ROW LEVEL SECURITY;
        CREATE POLICY tenant_isolation ON accounting.cutover_runs
            USING (tenant_id = nullif(current_setting('app.tenant_id', true), '')::uuid)
            WITH CHECK (tenant_id = nullif(current_setting('app.tenant_id', true), '')::uuid);

        ALTER TABLE accounting.opening_links ENABLE ROW LEVEL SECURITY;
        ALTER TABLE accounting.opening_links FORCE ROW LEVEL SECURITY;
        CREATE POLICY tenant_isolation ON accounting.opening_links
            USING (tenant_id = nullif(current_setting('app.tenant_id', true), '')::uuid)
            WITH CHECK (tenant_id = nullif(current_setting('app.tenant_id', true), '')::uuid);

        GRANT USAGE ON SCHEMA billing, treasury, inventory, accounting, platform TO avila_app;
        GRANT SELECT, INSERT, UPDATE, DELETE ON ALL TABLES IN SCHEMA billing, treasury, inventory, accounting, platform TO avila_app;
        """;
}
