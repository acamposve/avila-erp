# R1-REM-01 — Code map

Documentation-only change; no application code.

## 1. ADRs

| ADR | Ruta | Purpose |
|---|---|---|
| ADR-010 | `docs/decisions/ADR-010-monetary-precision.md` | Monetary precision/multi-currency |
| ADR-011 | `docs/decisions/ADR-011-immutable-audit.md` | Immutable audit/query/retention |
| ADR-012 | `docs/decisions/ADR-012-identity-sessions.md` | Opaque sessions/MFA |
| ADR-013 | `docs/decisions/ADR-013-topology-operations.md` | Topology/telemetry |

## 2. Reconciled docs

| Documento | Change |
|---|---|
| `docs/decisions/README.md` | ADR index + DEC-001/003/008 baseline |
| `docs/architecture/security.md` | Sessions/audit/retention naming and rules |
| `docs/product/roadmap.md` | DEC-001 baseline |
| `docs/requirements/PRD.md` | DEC-001 baseline |
| `docs/requirements/non-functional-requirements.md` | Retention baseline |

## 3. Evidence

| Artifact | Path |
|---|---|
| Execution log | `tasks/evidence/R1-REM-01/execution-log.md` |
| Verification | `tasks/evidence/R1-REM-01/verification.md` |
| Link check | `tasks/evidence/R1-REM-01/raw/link-check.txt` |
| Baseline | `tasks/evidence/R1-REM-01/raw/git-baseline.txt` |

## 4. Boundaries

- No code, migration, role or workflow change.
- No R1 gate opening.
- No production/deployment.
- No replacement of ADR-001…009.
