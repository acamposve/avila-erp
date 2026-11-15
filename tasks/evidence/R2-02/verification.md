# R2-02 — Inventory catalog

- **Branch:** `feat/r2-inventory-catalog` (stacked R2-01)
- **Tests:** **120/120 PASS** (19 spike + 101 production)
- **Status:** implemented; awaiting review

## Delivered

- Categories, UoMs, warehouses, item variants, bins/locations; item tax-classification fields (no rate catalog).
- Forced RLS + `tenant_isolation` policies + registry rows.
- App-role grants.
- `Version` concurrency tokens.
- `InventoryExportContributor` (module key `inventory`, deterministic records).
- HTTP catalog endpoints with `TenantWriteGuardMiddleware`, permission checks, and audit events.
- Price precision widened to `numeric(19,6)`; no tax rates hardcoded in new catalog fields.
- Test: tenant A/B isolation, export, RLS registry count.

## Evidence

- `src/Modules/Inventory/Avila.Modules.Inventory/InventoryDbContext.cs`
- `src/Modules/Inventory/Avila.Modules.Inventory/InventoryModule.cs`
- `src/Modules/Inventory/Avila.Modules.Inventory/InventoryExportContributor.cs`
- `src/Avila.Api/Inventory/InventoryCatalogEndpoints.cs`
- `tests/Avila.ProductionTests/InventoryCatalogTests.cs`
- `raw/test-output.txt`, `raw/link-check.txt`

## Boundaries

- No stock ledger/movements/reservations (R2-03/04).
- No new tax rates; legacy `TaxRate` remains for R1 compatibility.
- No R2-03 work; stop after this PR.
