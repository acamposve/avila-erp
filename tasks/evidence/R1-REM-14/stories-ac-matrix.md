# R1 stories and acceptance criteria — R1-REM-14

**Base:** `2591a4e` · **Date:** 2026-11-15
**Rule:** PASS means met at the tested level; PARTIAL means a named gap remains. This rollup does **not** override the 12/12 gate-criteria result, but it is part of the formal release decision model in `backlog/releases.md`.

## 1. Stories

| Story | Status | Change since T-013 | Evidence / remaining gap |
|---|---|---|---|
| US-PLT-001 Provision org/company/branch/admin | **PASS** | — | REM-01/02 tests; no HTTP/UI provisioning |
| US-PLT-002 Resolve tenant/RLS per request | **PASS** | — | RLS isolated/per-request tests |
| US-ADM-001 Create and edit companies/branches | **PARTIAL** | — | Create tested; edit/update still absent |
| US-ADM-002 Register parties | **PASS** | — | Party create/isolation |
| US-ADM-003 Invite user/multi-org | **PASS** | — | Invitation/switch/MFA |
| US-ADM-004 Roles/permissions scoped | **PASS (service+export HTTP)** | HTTP 403 on export, denial audit | Other business endpoints still limited |
| US-ADM-005 Append-only audit | **PASS** | — | Append-only trigger/grants + chain |
| US-ADM-006 Series/numbering | **PASS (internal)** | — | Internal quote/order; fiscal R2 |
| US-ADM-007 MFA and password policy | **PASS** | **Partial → PASS** | TOTP, recovery, rate limit, reset/verify |
| US-ADM-008 Sessions/expiry/revoke | **PASS** | — | 8h/30m, role/password revoke, replay |
| US-ADM-009 Versioned parameters | **PASS** | — | Effective date/source/history |
| US-ADM-010 Fiscal profile/pre-emission | **PASS** | — | Fiscal gate; Billing block |
| US-ADM-011 Support access | **PASS** | — | Ticket/expiry/read-only/audit |
| US-PLT-003 Plans, limits, usage | **PARTIAL** | — | Limits/flags exist; no commercial plans/E2E metering |
| US-ADM-012 Approval flows | **PASS** | — | Threshold/segregation/audit |
| US-ADM-013 Suspension/reactivation/export | **PASS** | **Partial → PASS** | Suspend/reactivate + complete R1 export |
| US-PLT-004 Operator console/flags | **PARTIAL** | — | Flags pass; no operator console |

**Stories:** **14 PASS, 3 PARTIAL, 0 FAIL.**

## 2. Acceptance criteria

| AC | Status | Evidence / gap |
|---|---|---|
| AC-ADM-01 Provision org/company/branch | **PASS** | Integration/isolation |
| AC-ADM-02 Multi-org switch no mixing | **PASS** | Switch/membership tests |
| AC-ADM-03 Denied stock adjust → 403/no effect/audit | **PARTIAL** | Service denial/audit; no stock-adjust HTTP endpoint |
| AC-ADM-04 Sensitive audit fields/immutable | **PASS** | Audit + hash chain |
| AC-ADM-05 Incomplete fiscal profile blocks invoice | **PASS** | Negative + rollback |
| AC-ADM-06 Branch series concurrent | **PASS (scope)** | Branch/company scope; Sales company scope only |
| AC-ADM-07 Suspended tenant write block/read/export/audit | **PARTIAL** | Guard/read/export pass; guard not wired into every command |
| AC-SEC-01 Cross-tenant resource fails 404/403/audited | **PARTIAL** | RLS/service + export safe; broad HTTP contract absent |
| AC-SEC-02 MFA + revocation invalidates access | **PASS** | MFA, password/role revoke, replay; no refresh by ADR-012 |
| AC-SEC-03 Limited role rejected server-side | **PARTIAL** | Service + export 403; limited HTTP surface |
| AC-PLT-01 Plan limit blocks module/limit/audited | **PARTIAL** | Limit consume; no module activation/E2E |
| AC-PLT-02 Trial auto-provision/meter/notify | **PARTIAL** | Provision/meter; no auto-onboarding/notification |
| AC-PLT-03 Support access audited/read-only/expires | **PASS** | Support tests |
| AC-FIS-01 Effective fiscal rule; document retains version | **PARTIAL** | Version selection/history; no document snapshot field |
| AC-OBS-01 Critical operation trace/metrics/log/audit correlated | **PARTIAL** | OTel/metrics/audit exist; no collector or single end-to-end correlation test |

**Acceptance criteria:** **7 PASS, 8 PARTIAL, 0 FAIL.**

## 3. Release interpretation

The 12 gate criteria are PASS at code/rehearsal level. The release model also requires stories done and AC evidence; three stories and eight ACs remain partial.

Therefore:

- **G1 gate-criteria assessment: 12/12 PASS.**
- **Release readiness: CONDITIONAL** until @acamposve explicitly accepts these named story/AC residuals (with owner/expiry) or authorizes closing them before release.
- G1 remains **CLOSED** until that separate decision.
