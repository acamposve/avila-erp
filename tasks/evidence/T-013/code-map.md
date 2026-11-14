# T-013 — Code map

T-013 is docs/evidence only; no application code changed.

## 1. Gate evidence

| Artefacto | Ruta | Propósito |
|---|---|---|
| R1 gate matrix | `tasks/evidence/T-013/r1-gate-matrix.md` | 12 exit criteria, result, decision |
| Stories/AC matrix | `tasks/evidence/T-013/stories-ac-matrix.md` | 17 stories + 15 ACs |
| Decisions/risks | `tasks/evidence/T-013/decisions-risks.md` | DEC, ADR, RSK, P1 |
| Execution log | `tasks/evidence/T-013/execution-log.md` | Commands/results |
| Verification | `tasks/evidence/T-013/verification.md` | Acceptance and gate result |

## 2. Referenced evidence

| Area | Existing package |
|---|---|
| Tenancy/RLS/pooling | `tasks/evidence/T-005/` |
| Identity/MFA/session | `tasks/evidence/T-006/` |
| Provisioning | `tasks/evidence/T-007/` |
| RBAC/audit/support | `tasks/evidence/T-008/` |
| Numbering/idempotency | `tasks/evidence/T-009/` |
| Configuration/approvals | `tasks/evidence/T-010/` |
| Lifecycle/export | `tasks/evidence/T-011/` |
| Observability/restore | `tasks/evidence/T-012/` |

## 3. Raw evidence

| Artefacto | Ruta |
|---|---|
| Full test output | `tasks/evidence/T-013/raw/test-output.txt` |
| API smoke | `tasks/evidence/T-013/raw/api-smoke.txt` |
| CI baseline | `tasks/evidence/T-013/raw/ci-baseline.txt` |
| Git log | `tasks/evidence/T-013/raw/git-log.txt` |
| Environment | `tasks/evidence/T-013/raw/environment.txt` |

## 4. Documentation touched

| Documento | Change |
|---|---|
| `backlog/releases.md` | R1 gate status note |
| `docs/tasks-validation.md` | T-012 verified, T-013 implemented, F-08 |
| `docs/project-execution-plan.md` | R1 evidence status |

## 5. Boundaries

- No G1 opening.
- No deployment.
- No code changes.
- No risk acceptance.
- No descope.
- No production data.
