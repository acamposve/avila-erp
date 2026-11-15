# R1 gate matrix delta — PR B

**Base:** `2251923` + PR B
**Result:** **12/12 PASS** at code/rehearsal level; G1 **CLOSED** pending sign-off.

| # | Criterion | Prior | Final | Delta evidence |
|---:|---|---|---|---|
| 1 | Provisioning | PASS | PASS | REM-01/02 + PR A |
| 2 | Isolation | PASS | PASS | RLS/jobs/export tests |
| 3 | RLS/roles/audit | PASS | PASS | policy registry + denial audits |
| 4 | RBAC | PASS | PASS | PR A 403 + audit |
| 5 | MFA/sessions | PASS | PASS | REM-08/09 + PR A E2E |
| 6 | Audit append-only/query/export | PASS | PASS | REM-10 + hash chain |
| 7 | Numbering | PASS | PASS | T-009 |
| 8 | Complete R1 export | PASS | PASS | REM-06/07 |
| 9 | Backup/restore | PASS | PASS | REM-13 |
| 10 | Observability/alerts | PASS | PASS | REM-12 + PR B job trace |
| 11 | CI security/SBOM | PASS | PASS | REM-11 |
| 12 | Foundational ADRs | PASS | PASS | ADR-001…013 + DEC-021 |

Full suite: **117/117**. No criterion regressed.

Residuals are accepted exceptions in [release-exception-register.md](release-exception-register.md), not gate-criteria failures.
