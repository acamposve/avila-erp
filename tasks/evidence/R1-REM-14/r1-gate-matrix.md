# R1 / G1 gate matrix — R1-REM-14

**Release:** R1 — ÁVILA Core
**Gate:** G1
**Base:** `2591a4e` (`main`, PR #29 merged)
**Evaluation date:** 2026-11-15
**Result:** **12/12 PASS at code/rehearsal level — G1 remains CLOSED pending explicit sign-off**
**Prepared by:** AI agent · **Owner/approver:** @acamposve

> This matrix is evidence for **R1-REM-14**. It does **not** open G1, authorize deployment or start R2. Opening G1 requires a separate explicit decision by @acamposve.

## 1. Summary

| Status | Criteria | Meaning |
|---|---:|---|
| PASS | 12 | Met at the stated R1 evidence level |
| PARTIAL | 0 | — |
| FAIL | 0 | — |
| **Total** | **12** | **Gate criteria complete; release sign-off pending** |

Full suite: **114/114** (19 spike + 95 production). Main CI: **PASS** (1m12s); CodeQL: **PASS** (2m18s). Link check: **0 broken / 205 files**.

## 2. R1 exit criteria

| # | Criterion | Prior (T-013) | Now | Evidence | Residual caveat |
|---:|---|---|---|---|---|
| 1 | Complete provisioning of organization, company, branch and administrator | PASS (service/DB) | **PASS** | [REM-01 verification](../R1-REM-01/verification.md), [REM-02](../R1-REM-02/verification.md), provisioning/lifecycle tests; PRs #17–#21 | No HTTP provisioning endpoint/UI; staged across identity/platform stores. |
| 2 | Isolation verified between two tenants in all resources and jobs | PARTIAL | **PASS** | [REM-03](../R1-REM-03/verification.md), [REM-05](../R1-REM-05/verification.md), [REM-06](../R1-REM-06/verification.md), [REM-07](../R1-REM-07/verification.md); RLS registry, job runner, export isolation; PRs #19–#23 | No transactional outbox/real queue; R1 has no deferred jobs beyond runner. |
| 3 | RLS active, tested and audited; application role without privileges | PARTIAL | **PASS** | Forced RLS + policy registry, `avila_app` no global tenant privileges, denied attempts audited; [REM-03](../R1-REM-03/verification.md), [REM-04](../R1-REM-04/verification.md) | RLS DDL changes are migration/PR-reviewed but not captured by a DB event trigger; production role provisioning is script-reviewed, not applied to a shared DB. |
| 4 | RBAC with company/branch/warehouse scope and authorization tests | PASS (service) | **PASS** | Authorization service + HTTP `403` on export, denial audit; [REM-04](../R1-REM-04/verification.md), [REM-07](../R1-REM-07/verification.md) | Business endpoint surface is still small; no separate warehouse module in R1. |
| 5 | MFA, sessions and revocation operational | PARTIAL | **PASS** | Mandatory MFA, TOTP enrollment, hashed single-use recovery, 8h/30m sessions, role/password revocation, step-up; [REM-08](../R1-REM-08/verification.md), [REM-09](../R1-REM-09/verification.md) | No WebAuthn/device notification; recovery codes do not expire by date. |
| 6 | Append-only audit, queryable and exportable | PARTIAL | **PASS** | Append-only trigger/grants, per-tenant SHA-256 chain, verify/query/NDJSON export, audit-of-audit; [REM-10](../R1-REM-10/verification.md) | Legacy pre-chain rows are `hash_version=0`; no external anchor/WORM; cursor is offset-based. |
| 7 | Concurrent numbering without duplicates or gaps | PASS (internal) | **PASS** | [T-009 verification](../T-009/verification.md): 10k unique/gapless, scope/rollback; PRs #17–#21 | Internal quote/order only; no fiscal series administration (R2). |
| 8 | Complete export of a tenant's data | **FAIL** | **PASS** | Deterministic export registry + identity/audit/admin contributors, manifest, streaming, HTTP E2E; [REM-06](../R1-REM-06/verification.md), [REM-07](../R1-REM-07/verification.md) | R1 modules only (PLT/ADM + identity/audit); sales/inventory/billing belong to R2; export excludes operational secrets. |
| 9 | Backups and restore rehearsed | PARTIAL | **PASS** | Full-schema `pg_dump`/`pg_restore` with two tenants, RLS/force/policy, counts and hash-chain verification in CI; [REM-13](../R1-REM-13/verification.md); PR #27 | Testcontainers, not production volume; no PITR/WAL/object-storage encryption; RPO/RTO provisional 24h/4h. |
| 10 | End-to-end observability and basic alerts | PARTIAL | **PASS** | Auth/allowlisted metrics, OTel ASP.NET traces/metrics with OTLP, Prometheus 5xx/latency/readiness/backup rules, `promtool` tests, Alertmanager drill; [REM-12](../R1-REM-12/verification.md); PR #29 | No provisioned OTLP collector/dashboards or production paging; alert receiver is a placeholder. |
| 11 | CI/CD with tests, migrations and security scanning | PARTIAL | **PASS** | CI build/test/clean migrations, CodeQL, High/Critical dependency gate, Gitleaks, CycloneDX SBOM; [REM-11](../R1-REM-11/verification.md); PR #28 | No deployment/CD job (by R1 policy); no signed SBOM/container/IaC scan. |
| 12 | Foundational ADRs approved | PARTIAL | **PASS** | ADR-001…013 accepted, including money, audit, identity and cloud; [ADR index](../../../docs/decisions/README.md) | ADR-009 external accounting validation remains for R2; DEC-001 cloud region is a pre-cloud placeholder; no production provider resources. |

## 3. Why this is 12/12

All twelve original gate blockers have a merged implementation plus automated evidence:

- the **FAIL** (complete export) is closed by the export registry, identity/audit contributors, manifest and HTTP E2E;
- the **8 PARTIALs** are closed by RLS/job/export isolation, denial audit, MFA/recovery/session lifecycle, audit chain/query/export, full restore rehearsal, authenticated telemetry/alerts, and CI security/SBOM;
- **ADR-012** resolves the missing identity-stack decision; ADR-009 remains an R2 external-validation dependency, not an R1 code gate.

“PASS” means **R1 code/rehearsal evidence**, not production deployment. The operational caveats in [operational-caveats.md](operational-caveats.md) remain part of the release decision.

## 4. Stories and acceptance criteria

The 12 gate criteria are complete, but the separate story/AC rollup is not fully green:

- **Stories:** 14 PASS, 3 PARTIAL (company/branch edit, commercial plans/usage, operator console).
- **AC:** 7 PASS, 8 PARTIAL (HTTP denial/audit breadth, suspension guard coverage, plan/module activation, auto-onboarding notification, fiscal document version, end-to-end OTel correlation test).

These are not hidden by the 12/12 criteria result. See [stories-ac-matrix.md](stories-ac-matrix.md). They require either explicit exception acceptance or a scope decision before a production release; they do not change the gate-criteria count.

## 5. Decision requested

**Recommendation: keep G1 CLOSED until @acamposve explicitly signs the release decision.**

Requested decision:

1. Accept this 12/12 gate-criteria matrix as R1-REM-14 evidence.
2. Decide whether to:
   - **A — Conditional G1 release:** accept the named story/AC and operational caveats; G1 opens only by explicit sign-off, with owners/expiry.
   - **B — Keep G1 closed:** close the remaining story/AC gaps first.
3. Do not treat PR approval as G1 approval or deployment authorization.

No deployment, production migration, shared restore or R2 work is authorized by this document.

## 6. Evidence index

- [Stories and acceptance criteria](stories-ac-matrix.md)
- [Operational caveats](operational-caveats.md)
- [Release sign-off request](release-signoff-request.md)
- [Verification](verification.md)
- [Execution log](execution-log.md)
- [Raw tests](raw/test-output.txt)
- [API smoke](raw/api-smoke.txt)
- [Link check](raw/link-check.txt)
- [CI baseline](raw/ci.txt)
- [Git baseline](raw/git-log.txt)
