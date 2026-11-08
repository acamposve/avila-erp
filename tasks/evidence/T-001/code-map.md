# T-001 — Code map

T-001 is a baseline/evidence task. It does not change production code.

## 1. Sources reviewed

| Artefacto | Ruta | Propósito |
|---|---|---|
| Task spec | [`tasks/T-001-baseline.md`](../../T-001-baseline.md) | Scope and acceptance |
| Execution plan | [`docs/project-execution-plan.md`](../../../docs/project-execution-plan.md) | Phase 0 and R1 sequence |
| R1 slice | [`docs/architecture/r1-vertical-slice.md`](../../../docs/architecture/r1-vertical-slice.md) | What exists and what does not |
| Spike verification | [`docs/architecture/spike-verification-2026-11-08.md`](../../../docs/architecture/spike-verification-2026-11-08.md) | 19 spike tests |
| Task validation | [`docs/tasks-validation.md`](../../../docs/tasks-validation.md) | 37-task evidence matrix |
| Sprint backlog | [`backlog/sprint-backlog.md`](../../../backlog/sprint-backlog.md) | Existing stories and dependencies |
| R1 exit criteria | [`backlog/releases.md`](../../../backlog/releases.md) | G1 criteria |
| Open decisions | [`docs/decisions/README.md`](../../../docs/decisions/README.md) | `DEC-001`, `DEC-003`, `DEC-008` |
| Architecture audit | [`docs/architecture/independent-architecture-audit.md`](../../../docs/architecture/independent-architecture-audit.md) | P1/P2 findings |
| Hardening plan | [`docs/architecture/architecture-hardening-plan.md`](../../../docs/architecture/architecture-hardening-plan.md) | T-04/T-07/T-08/T-09b/T-16 |

## 2. Code inspected

| Area | Path | Observation |
|---|---|---|
| UoW and tenant context | `src/Avila.Platform/Tenancy/` | Shared connection/transaction and `set_config`; request-scoped wiring missing |
| R1 application flow | `src/Avila.Application/SalesFlowService.cs` | Opens/commits its own UoW; no request scope |
| API host | `src/Avila.Api/Program.cs` | Health only; no auth or business endpoints |
| Module migrations | `src/Modules/*/Migrations/` | Forced RLS for four schemas; no `platform` schema |
| Spike tests | `tests/Avila.IntegrationTests/` | 19 tests |
| Production tests | `tests/Avila.ProductionTests/` | 2 tests |

## 3. Changes in this package

| Artefacto | Change |
|---|---|
| `tasks/evidence/T-001/execution-log.md` | New |
| `tasks/evidence/T-001/verification.md` | New |
| `tasks/evidence/T-001/code-map.md` | New |
| `tasks/evidence/T-001/raw/*` | New reproducible command outputs |

No source, migration, test, dependency or environment configuration was changed.

## 4. Boundaries

- No R1 closure.
- No G1 opening.
- No T-005 implementation.
- No migration applied to a shared database.
- No secret or PII included.
- No production logging added; the absence of structured logging is a finding, not something this task fixes.
