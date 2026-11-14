# R1 stories and acceptance criteria — T-013

**Base:** `cae8bbd` · **Date:** 2026-11-14
**Rule:** PASS means the story/AC is met at the tested level; PARTIAL means a named gap remains. Gate status still follows the stricter R1 matrix.

## 1. Stories in R1 scope

| Story | Scope | Status | Evidence / gap |
|---|---|---|---|
| US-PLT-001 | Provision org, company, branch, administrator | **PASS** | T-007 3 tests; no HTTP endpoint |
| US-PLT-002 | Resolve tenant context and RLS per request | **PASS** | T-005 19 tests; middleware/RLS |
| US-ADM-001 | Create and edit companies/branches | **PARTIAL** | Create tested; edit/update absent |
| US-ADM-002 | Register parties as customer/supplier | **PASS** | T-007 party creation test |
| US-ADM-003 | Invite user and accept multi-org membership | **PASS** | T-006 invitation/switch tests |
| US-ADM-004 | Roles/permissions with company/branch scope | **PASS (service)** | T-008 5 tests; no UI/HTTP 403 |
| US-ADM-005 | Append-only audit of sensitive operations | **PASS** | T-008 append-only/immutability tests |
| US-ADM-006 | Configure series and document numbering | **PASS (internal)** | T-009 10k/scope/rollback; no fiscal series UI |
| US-ADM-007 | MFA and password policy | **PARTIAL** | MFA/lockout/policy pass; recovery/email verification absent |
| US-ADM-008 | Sessions, expiry and remote revocation | **PASS (service)** | T-006 expiry/logout/revoke/switch tests |
| US-ADM-009 | Versioned parameters with effective date/source | **PASS** | T-010 5 tests; immutable history |
| US-ADM-010 | Fiscal profile and pre-emission validation | **PASS** | T-007 fiscal gate; Billing block |
| US-ADM-011 | Audited support access with justification | **PASS** | T-008 ticket/expiry/read-only/audit |
| US-PLT-003 | Plans, limits and usage measurement | **PARTIAL** | T-011 limits/flags; no commercial plans, no E2E metering |
| US-ADM-012 | Approval flows by type and amount | **PASS (service)** | T-010 threshold/segregation/audit |
| US-ADM-013 | Suspension, reactivation and data export | **PARTIAL** | T-011 suspend/reactivate/admin export; export incomplete |
| US-PLT-004 | Operator console and feature flags | **PARTIAL** | T-011 flags pass; no operator console |

**Stories:** 12 PASS, 5 PARTIAL, 0 FAIL.
A PASS at service level is not an HTTP/E2E pass.

## 2. R1 acceptance criteria

| AC | Status | Evidence / gap |
|---|---|---|
| AC-ADM-01 Provision org with company/branch | **PASS** | T-007 integration/isolation |
| AC-ADM-02 Multi-org switch without data mixing | **PASS** | T-006 switch/membership tests |
| AC-ADM-03 Denied stock adjust → 403, no effect, audited | **PARTIAL** | Service denial/no effect/audit exists; no HTTP 403 or denied-attempt audit |
| AC-ADM-04 Sensitive operation audit actor/tenant/before-after/correlation/immutable | **PASS** | T-008 audit tests |
| AC-ADM-05 Incomplete fiscal profile blocks invoice | **PASS** | T-007 negative test + rollback |
| AC-ADM-06 Branch series concurrent without crossover | **PASS (scope)** | T-009 branch/company scope; Sales uses company scope only |
| AC-ADM-07 Suspended tenant write block; read/export; audit | **PARTIAL** | T-011 guard/read/export; guard not called by all commands; lifecycle audit absent |
| AC-SEC-01 Cross-tenant resource by ID fails 404/403 and is audited | **PARTIAL** | DB/service isolation; no HTTP 404/403 contract or denied-attempt audit |
| AC-SEC-02 MFA + revocation invalidates refresh/access | **PARTIAL** | MFA/access revocation pass; no refresh token model |
| AC-SEC-03 Limited role rejected server-side | **PARTIAL** | Service tests pass; no HTTP endpoint |
| AC-PLT-01 Plan limit blocks module/limit without data loss, audited | **PARTIAL** | Limit consume passes; no module activation or E2E |
| AC-PLT-02 Trial onboarding auto-provisions, meters, notifies | **PARTIAL** | Provisioning/metering services; no auto-onboarding/notification |
| AC-PLT-03 Support access audited/read-only/expires | **PASS** | T-008 support test |
| AC-FIS-01 Effective fiscal rule; document retains version | **PARTIAL** | Version selection/history pass; no document snapshot/version field |
| AC-OBS-01 Critical operation trace/metrics/PII-free log/audit correlated | **PARTIAL** | Logs/metrics/audit; no distributed trace/exporter; not one end-to-end correlation test |

**Acceptance criteria:** 6 PASS, 9 PARTIAL, 0 FAIL.

## 3. Gate interpretation

Passing stories/ACs at service level is necessary but not sufficient for R1:

- the R1 matrix has a hard **FAIL** on complete export;
- several PARTIALs are release-critical (audit export, alerts, security scan, HTTP authorization);
- G1 remains closed.
