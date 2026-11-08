# Architecture Spike Verification — 2026-11-08

**Status:** Spike tests passed. **Production implementation and full P1 closure are still pending.**
**Scope:** T-01…T-19 from the [hardening plan](architecture-hardening-plan.md), including T-02, T-08, T-09b and T-11b.
**Environment:** .NET SDK 10.0.403, Docker 29.9.2, PostgreSQL 18 (`postgres:18-alpine`) via Testcontainers.

---

## 1. Result

```text
dotnet test Avila.slnx
Passed!  - Failed: 0, Passed: 19, Skipped: 0, Total: 19
```

| Test | Result | What it proves |
|---|---|---|
| T-01 — concurrent stock issues | **PASS** | Row lock prevents overselling; one commits, one fails; final quantity 2 |
| T-02 — crossing transfers | **PASS** | Both transfers use warehouse-id lock order; no deadlock; totals preserved |
| T-03 — concurrent payments | **PASS** | Row lock prevents over-allocation; one application of 60 |
| T-04/T-04b — module transaction | **PASS** | Billing and Treasury commit/rollback as one transaction |
| T-05 — invoice posting | **PASS** | One invoice, one journal, two balanced lines, linked entry |
| T-06 — accounting failure | **PASS** | Invoice, receivable, journal and number roll back |
| T-07 — RLS + pool reuse | **PASS** | Same backend PID; each tenant sees only its own data; no-context query returns zero rows |
| T-08 — read-only RLS | **PASS** | Read-only transaction keeps tenant context and isolation |
| T-09 — platform RLS | **PASS** | Cross-tenant write rejected with `42501`; reads isolated |
| T-09b — schema invariants | **PASS** | Every table with `tenant_id` has forced RLS and a policy; `avila_app` has no bypass |
| T-10 — company/branch numbering | **PASS** | Independent scopes produce 1, 2 and 1 |
| T-11 — 200 concurrent numbers | **PASS** | 200 unique, gapless numbers |
| T-11b — 10,000 concurrent numbers | **PASS** | 10,000 unique, gapless numbers from 1 to 10,000 |
| T-12 — R2→R3 cutover | **PASS** | Opening entries for AR and inventory; source links; invoice status; accounting enabled; idempotent rerun |
| T-14 — unbalanced journal | **PASS** | Direct SQL one-sided entry rejected by deferred trigger |
| T-15 — duplicate posting | **PASS** | Second posting rejected; exactly one journal and two lines |
| T-18 — idempotency in flight | **PASS** | Concurrent same key executes once and replays response |
| T-19 — close vs. posting | **PASS** | Close locks period; concurrent posting rejected; period closed with zero entries |

---

## 2. Evidence

Test project:

- `tests/Avila.IntegrationTests/PostgresFixture.cs`
- `tests/Avila.IntegrationTests/InventoryConcurrencyTests.cs`
- `tests/Avila.IntegrationTests/TransferConcurrencyTests.cs`
- `tests/Avila.IntegrationTests/PaymentConcurrencyTests.cs`
- `tests/Avila.IntegrationTests/TransactionRollbackTests.cs`
- `tests/Avila.IntegrationTests/AccountingPostingTests.cs`
- `tests/Avila.IntegrationTests/TenantIsolationTests.cs`
- `tests/Avila.IntegrationTests/ReadOnlyRlsTests.cs`
- `tests/Avila.IntegrationTests/PlatformIsolationTests.cs`
- `tests/Avila.IntegrationTests/SchemaInvariantTests.cs`
- `tests/Avila.IntegrationTests/NumberingTests.cs`
- `tests/Avila.IntegrationTests/JournalIntegrityTests.cs`
- `tests/Avila.IntegrationTests/IdempotencyTests.cs`
- `tests/Avila.IntegrationTests/PeriodCloseTests.cs`
- `tests/Avila.IntegrationTests/CutoverTests.cs`

Spike implementation:

- `src/Avila.Spike/Schema/DatabaseSchema.cs` — test-only schema, roles, RLS, balance trigger
- `src/Avila.Spike/Tenancy/TenantSession.cs` — transaction-local `set_config`
- `src/Avila.Spike/Tenancy/TenantUnitOfWork.cs` — shared connection/transaction across two contexts
- `src/Avila.Spike/Inventory/InventoryService.cs` — stock lock path
- `src/Avila.Spike/Inventory/TransferService.cs` — ordered two-balance transfer lock
- `src/Avila.Spike/Treasury/PaymentService.cs` — payment allocation lock path
- `src/Avila.Spike/Accounting/AccountingService.cs` — synchronous invoice posting with `posting_key`
- `src/Avila.Spike/Accounting/CutoverService.cs` — R2→R3 opening entries and source links
- `src/Avila.Spike/Accounting/PeriodService.cs` — period posting/close locks
- `src/Avila.Spike/Platform/IdempotencyService.cs` — in-flight idempotency
- `src/Avila.Spike/Billing/SalesInvoiceService.cs` — invoice + receivable + number + journal in one UoW
- `src/Avila.Spike/Billing/DocumentNumberService.cs` — scope-aware numbering

The schema is **not** a production migration. It exists to verify mechanisms before creating the real modules and migrations.

---

## 3. What is proven

1. Transaction-local `set_config` works with pooled connections and does not leak tenant state.
2. Stock, transfers, receivables, invoice posting, numbering, idempotency and period close have explicit, tested concurrency controls.
3. Two EF Core contexts can share one PostgreSQL transaction; rollback and commit affect both atomically.
4. RLS is active and forced for every current table with `tenant_id`; `avila_app` is not owner and has no `BYPASSRLS`.
5. Read-only transactions preserve RLS context.
6. Invoice, receivable, number and journal commit atomically; accounting failure rolls all back.
7. Platform tables are isolated and reject cross-tenant writes.
8. Company and branch numbering scopes are independent; 10,000 concurrent emissions are unique and gapless.
9. A deferred trigger rejects an unbalanced journal inserted by direct SQL.
10. Duplicate accounting events are rejected by `posting_key`.
11. Idempotency keys serialize concurrent duplicates and replay the stored response.
12. Period close excludes concurrent postings; R2→R3 cutover creates balanced opening entries and is idempotent.

---

## 4. What is NOT proven

| Gap | Status |
|---|---|
| PgBouncer transaction pooling | T-07b pending; `D-07` says not adopted initially |
| Full R2→R3 cutover with lots, taxes, cash and retained earnings | Simplified cutover only |
| FX differences | T-20 pending and blocked by `D-14` |
| Production migrations, roles, pooler, backup/restore | Not implemented |
| Production R1 modules and vertical slice | Not started |
| Multi-node API/worker behavior | Spike uses a single test process |
| Platform tables that do not yet exist in production | Current schema only |

---

## 5. P1 status after this spike

| P1 | Status | Reason |
|---|---|---|
| P1-01 Stock | **Mechanism verified; production pending** | T-01/T-02 passed; real module and lots pending |
| P1-02 Payments | **Mechanism verified; production pending** | T-03 passed; repeated-run evidence and real flow pending |
| P1-03 Cross-module transaction | **Mechanism verified; production pending** | T-04 passed; forced failure injection in TES/CON pending |
| P1-04 Posting | **Mechanism verified; production pending** | T-05/T-06/T-15 passed; real accounting module pending |
| P1-05 RLS/pool | **Substantially verified in spike** | T-07/T-08 passed; T-07b (PgBouncer) pending |
| P1-06 Platform RLS | **Verified for current schema** | T-09/T-09b passed; production inventory pending |
| P1-07 Numbering | **Mechanism verified; production pending** | T-10/T-11/T-11b passed at 10k; migration pending |
| P1-08 Cutover | **Mechanism verified; production pending** | T-12 passed with simplified dataset |
| P1-09 R5 dependency | **Review verified** | [T-004 review](r5-r3-dependency-review.md); one roadmap table and T-13 remain pending |

**No P1 is closed by this document.** The evidence covers mechanisms and the current test schema, not production implementation.

---

## 6. Next block

1. **T-07b** only if PgBouncer is adopted; otherwise record `D-07` as “not adopted” and close the verification item.
2. **T-20** after `D-14` accounting validation.
3. **Production migrations and R1 vertical slice**: tenant → company → party → item → quote → order → invoice draft.

---

## 7. References

- [Hardening plan](architecture-hardening-plan.md)
- [Independent audit](independent-architecture-audit.md)
- [ADR-007](../decisions/ADR-007-transaction-and-tenant-context.md)
- [ADR-008](../decisions/ADR-008-concurrency-by-aggregate.md)
- [ADR-009](../decisions/ADR-009-accounting-and-cutover.md)
