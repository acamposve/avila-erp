# R1 G1 sign-off package

**Gate:** G1
**Candidate:** `feat/r1-fiscal-trace-closure` (after PR A #31)
**Gate criteria:** **12/12 PASS**
**Stories:** 15 PASS + 2 accepted deferrals
**AC:** 13 PASS + 2 accepted deferrals
**Current state:** **G1 CLOSED**
**Owner:** @acamposve

## 1. Final gate delta

| Criterion | Status |
|---|---|
| 1 Provisioning | PASS |
| 2 Isolation all resources/jobs | PASS |
| 3 RLS/roles/audit | PASS |
| 4 RBAC | PASS |
| 5 MFA/sessions/revocation | PASS |
| 6 Append-only audit query/export | PASS |
| 7 Numbering | PASS |
| 8 Complete R1 export | PASS |
| 9 Backup/restore rehearsal | PASS |
| 10 Observability/alerts | PASS |
| 11 CI security/SBOM | PASS |
| 12 Foundational ADRs | PASS |

PR A closed US-ADM-001, HTTP denial/audit/suspension, and the AC-ADM-03 security pattern. PR B closes AC-FIS-01 and AC-OBS-01.

## 2. Accepted exceptions (DEC-021)

- US-PLT-003 commercial plans/E2E metering.
- US-PLT-004 operator console.
- AC-PLT-01 module/limit activation.
- AC-PLT-02 auto-onboarding notification.
- Literal AC-ADM-03 stock-adjust → Inventory/R2.
- Pre-cloud: OTLP collector, Alertmanager receiver, Terraform apply, PITR/WAL, WORM anchor.
- Owner: @acamposve; expiry: **Production Readiness Gate**.

## 3. Sign-off requested

```text
G1 decision: APPROVE R1 RELEASE — 12/12 gate criteria; accepted exceptions per DEC-021; owner @acamposve; expiry Production Readiness Gate.
Date: <YYYY-MM-DD>
```

or keep G1 closed with named blockers.

## 4. Explicitly not authorized

- Deployment or production migrations.
- Production restore/backup execution.
- Starting R2.
- Auto-opening G1.

## 5. Evidence

- [12/12 final matrix](r1-gate-matrix-delta.md)
- [Stories/AC](stories-ac-matrix.md)
- [PR B verification](verification.md)
- [DEC-021](../../../docs/decisions/DEC-021-r1-scope-exceptions.md)
- [PR A evidence](../R1-CLOSURE-A/verification.md)
