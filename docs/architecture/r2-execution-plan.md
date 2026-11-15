# R2 Execution Plan — ÁVILA Comercial

**Status:** approved by @acamposve on 2026-11-15
**G1:** OPEN
**R2:** implementation authorized by the approved plan
**ADRs:** ADR-001…013 frozen for R2; changes require a new ADR

## 1. Modules and boundaries

| Module | Owns | May not |
|---|---|---|
| Inventory | items/variants/UoM, warehouses/bins, movements, reservations, valuation | write sales/purchasing/ledger tables |
| Purchasing | suppliers terms, POs, receipts, purchase invoices, AP | deduct stock directly; write ledger |
| Sales/Billing | prices, quotes, orders, deliveries, fiscal invoices/notes, AR | deduct stock outside Inventory Contracts |
| Ledger | CoA, journals, posting rules, trial balance | own operational documents |

Cross-module effects use the transactional outbox only. No module reads another module DbContext/table.

## 2. R2 conventions (mandatory per PR)

- `tenant_id NOT NULL`, composite tenant FKs/indexes, forced RLS, `tenant_isolation`, registry row, app-role grant.
- ADR-010 precision: amounts `numeric(19,4)`, prices/qty `numeric(19,6)`, rates `numeric(19,8)`, percentages `numeric(9,6)`; half-away-from-zero; dual-currency snapshots.
- Fiscal documents capture immutable `FiscalDocumentSnapshot` at issuance.
- Every module registers `ITenantExportContributor`; deterministic schema/version.
- Mutable aggregates use `Version`/concurrency token; stale write `409`.
- Mutating routes pass `TenantWriteGuardMiddleware` (`423` suspended).
- Accepted changes use `AuditService` in the same UoW; denials use independent audit; hash chain always applies.
- Fiscal values are data only (`FiscalRuleVersion`); no hardcoded rates.

## 3. Outbox event catalogue (initial)

| Event | Producer | Consumers |
|---|---|---|
| `inventory.receipt.posted` | Inventory | Purchasing/AP, Ledger |
| `inventory.issue.posted` | Inventory | Billing/COGS, Ledger |
| `inventory.reservation.created/released` | Inventory | Sales |
| `purchasing.receipt.confirmed` | Purchasing | Inventory, AP |
| `purchasing.invoice.posted` | Purchasing | AP, Ledger |
| `billing.invoice.issued` | Billing | AR, Ledger |
| `billing.credit_note.issued` | Billing | AR, Ledger |
| `treasury.payment.posted` | AR/AP | Ledger |
| `fx.realized` | AR/AP | Ledger |
| `ledger.journal.posted` | Ledger | reporting/export |

## 4. Domain guardrails

### 4.1 Realized FX gain/loss

`CurrencyPair` captures document rate **and** settlement rate. AR/AP settlement rows retain:

- original document currency/base amounts and document rate;
- payment/settlement currency/base amounts and settlement rate;
- realized FX amount and rate date/source.

Settlement emits `fx.realized` with both rates and the computed difference; the Ledger consumes it. Historical documents are never recomputed from current rates.

### 4.2 Reservation vs physical deduction

- Sales Order → **reservation only**: `available = on_hand - reserved`.
- Delivery (or direct invoice without prior delivery) → **physical Kardex issue + COGS valuation**.
- Release reservation on delivery/cancel; never deduct twice.
- Contracts expose `ReserveAsync`, `ReleaseAsync`, `IssueAsync`; Billing cannot call stock deduction directly.

## 5. Milestones

| Milestone | PRs |
|---|---|
| M0 Foundations | R2-00 governance, R2-01 money/outbox/conformance |
| M1 Inventory + fiscal engine | R2-02 catalog, R2-03 stock ledger, R2-04 operations, R2-05 tax engine |
| M2 Purchasing/AP | R2-06 purchasing, R2-07 payables |
| M3 Sales/Billing/AR | R2-08 sales/reservations, R2-09 fiscal billing, R2-10 receivables |
| M4 Ledger | R2-11 ledger core, R2-12 integration/FX |
| M5 Gate | R2-13 pilot/export/restore/security gate |

Detailed PR acceptance criteria live in `tasks/evidence/R2-00/r2-execution-plan.md` history and each task’s evidence package.

## 6. Gates

- DEC-002 before real fiscal emission.
- DEC-007 before B2B withholding.
- ADR-009 external accounting validation before production posting.
- DEC-005 ratification before multi-currency pilot.
- No production deploy/shared migration without explicit authorization.
