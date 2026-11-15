# R1-CLOSURE-A — Verification

**PR:** A — `feat/r1-adm-write-surface`
**Base:** `4508426`
**Status:** implemented; awaiting review. G1 remains **CLOSED**, R2 **FROZEN**.

## 1. Acceptance criteria

| Criterion | Evidence | Result |
|---|---|---|
| US-ADM-001 company edit | PATCH endpoint + validation + 409 + audit | **PASS** |
| US-ADM-001 branch edit | PATCH endpoint + validation + 409 + audit | **PASS** |
| Optimistic concurrency | `Version` + EF concurrency token; stale → 409 | **PASS** |
| Before/after audit | `administration.company.updated` / `branch.updated` | **PASS** |
| HTTP 403 member | `authorization.denied` persisted | **PASS** |
| Cross-tenant/missing 404 | no row, `security.resource.not_found` | **PASS** |
| Suspension all writes | 423 before handler; no data mutation; audit | **PASS** |
| Safe routes exempt | auth/export/read requests not blocked | **PASS (design + E2E)** |
| AC-ADM-03 security pattern | denied company/branch mutation, no effect, audit | **PASS** |
| Literal stock adjust | deferred Inventory/R2, DEC-021 | **DEFERRED** |

## 2. Tests

- Full suite: **115/115** (19 spike + 96 production).
- HTTP E2E: owner login → mandatory MFA enrollment → company create → company update → stale 409 → branch create/update → member 403 → suspended 423.
- Audit assertions: `administration.company.created`, `administration.company.updated`, `authorization.denied`, `tenant.write.blocked`; no suspended company row.

## 3. Delta rollup

| Rollup | Before | After A | Remaining |
|---|---:|---:|---|
| Stories PASS | 14/17 | **15/17** | US-PLT-003, US-PLT-004 (DEC-021) |
| AC PASS | 7/15 | **11/15** | AC-PLT-01/02 (DEC-021), AC-FIS-01, AC-OBS-01 (PR B) |

## 4. Security/tenancy

- No client-supplied tenant ID.
- Authorization is server-side; owner/admin only for writes.
- RLS remains active; cross-tenant ID returns 404 without data.
- Denied/not-found/blocked audit writes use independent transactions where the business transaction rolls back.
- No secrets/PII in audit metadata.

## 5. Open gaps

- Branch/company read/list endpoints are not included (edit uses created IDs).
- No UI.
- AC-FIS-01 and AC-OBS-01 remain for PR B.
- Literal stock adjust is deferred to R2.
- G1 remains closed; no deployment.

## 6. Evidence

- [Contracts](../../../src/Modules/Administration/Avila.Modules.Administration.Contracts/AdministrationContracts.cs)
- [Module](../../../src/Modules/Administration/Avila.Modules.Administration/AdministrationModule.cs)
- [Endpoints](../../../src/Avila.Api/Administration/AdministrationEndpoints.cs)
- [Write guard](../../../src/Avila.Api/Tenancy/TenantWriteGuardMiddleware.cs)
- [Migration](../../../src/Modules/Administration/Avila.Modules.Administration/Migrations/20261115113305_AddAdministrationVersion.cs)
- [E2E test](../../../tests/Avila.ProductionTests/AdministrationHttpE2eTests.cs)
- [DEC-021](../../../docs/decisions/DEC-021-r1-scope-exceptions.md)
- [execution-log.md](execution-log.md)
