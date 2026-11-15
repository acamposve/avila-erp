# R1 final stories and acceptance criteria

**Base:** `2251923` + PR B · **Date:** 2026-11-15

## Stories

| Story | Status | Evidence / exception |
|---|---|---|
| US-PLT-001 | PASS | Provisioning tests |
| US-PLT-002 | PASS | RLS/tenant tests |
| US-ADM-001 | **PASS** | PR A company/branch edit + 409 + audit |
| US-ADM-002 | PASS | Party tests |
| US-ADM-003 | PASS | Invitation/switch/MFA |
| US-ADM-004 | PASS | RBAC + HTTP 403 |
| US-ADM-005 | PASS | Audit append-only/chain |
| US-ADM-006 | PASS | Internal numbering |
| US-ADM-007 | PASS | MFA/recovery/rate/reset |
| US-ADM-008 | PASS | Session lifecycle |
| US-ADM-009 | PASS | Versioned parameters |
| US-ADM-010 | PASS | Fiscal profile gate |
| US-ADM-011 | PASS | Support access |
| US-ADM-012 | PASS | Approvals |
| US-ADM-013 | PASS | Suspension/reactivation/export |
| US-PLT-003 | **DEFERRED (accepted)** | DEC-021; Production Readiness Gate |
| US-PLT-004 | **DEFERRED (accepted)** | DEC-021; Production Readiness Gate |

**Stories:** 15 PASS, 2 accepted deferrals, 0 FAIL.

## Acceptance criteria

| AC | Status | Evidence / exception |
|---|---|---|
| AC-ADM-01 | PASS | Provisioning |
| AC-ADM-02 | PASS | Multi-org |
| AC-ADM-03 | **PASS (security pattern)** | PR A denied mutation; literal stock adjust deferred to Inventory/R2/DEC-021 |
| AC-ADM-04 | PASS | Audit chain |
| AC-ADM-05 | PASS | Fiscal profile block |
| AC-ADM-06 | PASS | Numbering scope |
| AC-ADM-07 | **PASS** | 423 write guard/read/export/audit |
| AC-SEC-01 | **PASS** | HTTP 404/403 + independent denied/not-found audit |
| AC-SEC-02 | **PASS** | MFA/revocation/replay |
| AC-SEC-03 | **PASS** | HTTP 403 + denied audit |
| AC-PLT-01 | **DEFERRED (accepted)** | DEC-021 |
| AC-PLT-02 | **DEFERRED (accepted)** | Auto-onboarding notification only; DEC-021 |
| AC-PLT-03 | PASS | Support access |
| AC-FIS-01 | **PASS** | Immutable rule versions + document snapshots |
| AC-OBS-01 | **PASS** | OTel HTTP/job trace correlation + zero tenant/user tags |

**AC:** 13 PASS, 2 accepted deferrals, 0 FAIL.

## Gate

All **12 R1 gate criteria remain PASS** at code/rehearsal level. G1 is not opened by this matrix; explicit owner sign-off is required.
