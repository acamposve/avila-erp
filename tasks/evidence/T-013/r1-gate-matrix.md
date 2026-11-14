# R1 / G1 gate matrix — 2026-11-14

**Release:** R1 — ÁVILA Core
**Gate:** G1
**Base:** `cae8bbd` (`main`, PR #15 merged)
**Result:** **NO-GO — G1 remains CLOSED**
**Prepared by:** AI agent · **Owner/approver:** @acamposve

> This matrix is evidence for **T-013**. Approving the T-013 PR does **not** open G1 and does **not** authorize deployment. Opening G1 requires a separate explicit decision by @acamposve.

## 1. Summary

| Status | Criteria | Meaning |
|---|---:|---|
| PASS | 3 | Met at the stated level |
| PARTIAL | 8 | Implemented/tested in part; missing a gate requirement |
| FAIL | 1 | Required capability absent |
| **Total** | **12** | **NO-GO** |

Full test suite: **72/72** (19 spike + 53 production). CI: build, tests and clean migrations pass. Those results prove the implemented slice, not the full R1 exit criteria.

## 2. R1 exit criteria

| # | Criterion | Status | Evidence | Remaining gap / owner |
|---:|---|---|---|---|
| 1 | Complete provisioning of organization, company, branch and administrator | **PASS (service/DB)** | [T-007 verification](../T-007/verification.md), 3 provisioning tests, [T-011 lifecycle](../T-011/verification.md) | No HTTP provisioning endpoint/UI; staged, not atomic across stores. Owner: @acamposve |
| 2 | Isolation verified between two tenants in all resources and jobs | **PARTIAL** | [T-005](../T-005/verification.md) 19 tests, RLS/backfill/pool/rollback; [T-007](../T-007/verification.md), [T-011](../T-011/verification.md) export isolation | No background jobs/outbox yet; no exhaustive per-resource matrix. Owner: Plataforma |
| 3 | RLS active, tested and audited; application role without privileges | **PARTIAL** | [T-005](../T-005/verification.md): forced RLS, introspection, `avila_app` no global tenants, no bypass | RLS changes/attempts are not independently audited; no production role provisioning. Owner: Plataforma/Seguridad |
| 4 | RBAC with company/branch/warehouse scope and authorization tests | **PASS (service)** | [T-008](../T-008/verification.md): 5 tests, scope isolation, deny-by-default | No HTTP `403` contract or endpoint integration. Owner: Seguridad |
| 5 | MFA, sessions and revocation operational | **PARTIAL** | [T-006](../T-006/verification.md): 8 tests, TOTP, expiry, logout/revoke, lockout | No password recovery/email verification, no HTTP rate limiting, MFA not mandatory for owners/admins. Owner: Seguridad |
| 6 | Append-only audit, queryable and exportable | **PARTIAL** | [T-008](../T-008/verification.md): append-only trigger/grants, actor/before/after/correlation | No query/export API/UI, no tamper-evident chain. Owner: Seguridad/Operaciones |
| 7 | Concurrent numbering without duplicates or gaps | **PASS (internal docs)** | [T-009](../T-009/verification.md): 10k unique/gapless, company/branch scope, rollback | Internal quote/order only; no fiscal numbering/series administration. Owner: Plataforma |
| 8 | Complete export of a tenant's data | **FAIL** | [T-011](../T-011/verification.md): export only companies, branches, parties | Sales, inventory, billing, identity and audit are not exported; no export manifest/verification. Owner: Plataforma |
| 9 | Backups and restore rehearsed | **PARTIAL** | [T-012](../T-012/execution-log.md): disposable `pg_dump`/`pg_restore` PASS with RLS | No shared/production restore, no full-schema/PITR, no approved RPO/RTO/retention. Owner: Operaciones |
| 10 | End-to-end observability and basic alerts | **PARTIAL** | [T-012](../T-012/verification.md): health, metrics, correlation logs, no-PII rules | No alerts/dashboards, no OpenTelemetry/exporter, `/metrics` unauthenticated, no outbox/DB metrics. Owner: Operaciones |
| 11 | CI/CD with tests, migrations and security scanning | **PARTIAL** | [CI baseline](raw/ci-baseline.txt), [PR #15 CI](../T-012/raw/ci-baseline.txt): build, 72 tests, clean migrations | No SAST/dependency/container scan; no deployment/CD; no migration gate beyond clean apply. Owner: Plataforma/Seguridad |
| 12 | Foundational ADRs approved | **PARTIAL** | [ADR index](../../../docs/decisions/README.md): ADR-001…009 accepted | ADR-009 external accounting validation pending; identity-stack ADR absent; `DEC-001` region and `DEC-008` legal validation open. Owner: @acamposve |

## 3. Gate rule

> Any failure or unaccepted partial keeps G1 closed. T-013 does not grant an exception.

The only criterion marked **FAIL** is complete tenant export. The eight PARTIAL criteria independently block a strict R1 exit. Test volume and green CI do not compensate for missing exit capabilities.

## 4. Decision requested

**Recommendation: keep G1 CLOSED.**

Requested from @acamposve:

1. Accept this matrix as the T-013 evidence package.
2. Confirm G1 remains closed.
3. Choose remediation scope before re-running the gate:
   - **Option A (recommended):** close all FAIL/PARTIAL blockers and re-run T-013.
   - **Option B:** explicitly accept named exceptions, with owner, expiry and risk acceptance; this is a release decision, not a task approval.
   - **Option C:** descope R1 formally; requires updating `backlog/releases.md` and dependent releases.

T-013 PR approval is **not** release approval. No deployment is authorized by this document.

## 5. Evidence index

- [Stories and acceptance criteria](stories-ac-matrix.md)
- [Decisions, risks and findings](decisions-risks.md)
- [Verification](verification.md)
- [Execution log](execution-log.md)
- [Raw tests](raw/test-output.txt)
- [API smoke](raw/api-smoke.txt)
- [Git baseline](raw/git-log.txt)
- [CI baseline](raw/ci-baseline.txt)
