# T-002 — Code map

T-002 is a decision/evidence task. It changes no application code.

## 1. Sources

| Artefacto | Ruta | Propósito |
|---|---|---|
| Task spec | [`tasks/T-002-decisions.md`](../../T-002-decisions.md) | Scope and acceptance |
| Decision register | [`docs/decisions/README.md`](../../../docs/decisions/README.md) | DEC-001/003/008 status and owners |
| Execution plan | [`docs/project-execution-plan.md`](../../../docs/project-execution-plan.md) | Phase 0 and R1 sequence |
| Security model | [`docs/architecture/security.md`](../../../docs/architecture/security.md) | Identity and tenancy context |
| T-001 evidence | [`tasks/evidence/T-001/`](../T-001/) | Baseline and capacity |
| PR #4 | https://github.com/acamposve/avila-erp/pull/4 | Merged decisions and CI |

## 2. Changes in this package

| Artefacto | Change |
|---|---|
| `tasks/evidence/T-002/execution-log.md` | New |
| `tasks/evidence/T-002/verification.md` | New |
| `tasks/evidence/T-002/decision-record.md` | New |
| `tasks/evidence/T-002/code-map.md` | New |
| `tasks/evidence/T-001/*` | Status updated to verified |
| `docs/tasks-validation.md` | Counts updated |

No source, migration, test, dependency or environment configuration was changed.

## 3. Boundaries

- No T-006 identity implementation.
- No Terraform apply or cloud resource.
- No fiscal/legal validation invented.
- No production migration or deployment.
- No secret or PII included.
