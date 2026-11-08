# T-003 — Verification

**Task:** [T-003 — Refinar trabajo de hardening](../../T-003-hardening-backlog.md)
**Status:** `implemented` — awaiting human review. Not `verified`.
**Date:** 2026-11-08

## 1. Acceptance criteria

| Criterio de T-003 | Evidencia | Resultado |
|---|---|---|
| Mapear P1/P2 relevantes a historias, ADR, pruebas, responsables y evidencia | [hardening-refinement.md](../../../docs/architecture/hardening-refinement.md): 13 refinamientos, 9 P1, 9 P2 | **Pass** |
| Separar spikes de código de producción | §6 of the refinement document; P1 matrix has `Evidencia en spike` and `Evidencia de producción` | **Pass** |
| Cada historia refinada tiene dependencia y criterios de cierre trazables a las pruebas | §3 table has `Depende de`, `Pruebas`, `Criterio de cierre` per refinement | **Pass** |
| No declarar cerrado un hallazgo sin implementación y evidencia | P1 matrix: **all 9 open**; P2 matrix: all open/blocked; no closure | **Pass** |

## 2. Refinement coverage

| Measure | Count |
|---|---:|
| P1 findings | 9 |
| P1 closed | **0** |
| P2 findings with direct impact | 9 |
| P2 closed | **0** |
| P2 out of scope | 5 (`P2-05`…`P2-09`) |
| Refined work units | 13 |
| Production implementation claimed | 0 |

## 3. P1 status

| P1 | Refined story/task | Closure tests | Current evidence | Closed |
|---|---|---|---|---|
| P1-01 Stock | `US-INV-004/005` / T-014/T-015 | T-01, T-02 | Spike passed; production absent | **No** |
| P1-02 Payments | `US-TES-002/003` / T-020 | T-03 | Spike once; production absent | **No** |
| P1-03 Transaction | `US-PLT-002` / T-005 | T-04 | Spike passed; production partial | **No** |
| P1-04 Posting | `US-FAC-001` / T-019/T-024 | T-05, T-06, T-15 | Spike passed; production absent | **No** |
| P1-05 RLS/pool | `US-PLT-002` / T-005 | T-07, T-07b, T-08 | Spike T-07/T-08; T-07b pending | **No** |
| P1-06 `platform` | `US-PLT-002` / T-005 | T-09, T-09b | Spike passed; production absent | **No** |
| P1-07 Numbering | `US-ADM-006` / T-009 | T-10, T-11, T-11b | Spike 10k; production placeholders | **No** |
| P1-08 Cutover | `US-CON-004` (proposed) / T-025 | T-12 | Spike simplified; production absent | **No** |
| P1-09 R5→R3 | T-004 | T-13 | Docs corrected; review pending | **No** |

## 4. Priority decisions

- **P0/R1:** T-005 (UoW/RLS/platform), T-009 (numbering/idempotency), T-004 (R5 dependency).
- **P1/R2:** stock, payments, credit, posting/outbox, backfill.
- **P2/R3:** period close, cutover, FX.
- **Out:** P2-05…P2-09 and P3.

## 5. Dependencies that remain open

| Dependency | Blocks | Owner | Status |
|---|---|---|---|
| `DEC-001` region/latency | R1 environment | @acamposve | Open |
| `DEC-008` legal retention | R1/R2 | @acamposve + external legal | Open |
| `D-02` cutover hybrid confirmation | P1-08 | @acamposve + accounting | Open |
| `D-05` module levels | P2-01 | @acamposve | Open |
| `D-14` FX policy | P2-03/T-20 | @acamposve + accounting | Open |
| `D-17` commercial/fiscal boundary | P2-04 | @acamposve | Approved; contract test pending |
| Capacity | All execution | @acamposve | Not quantified |

## 6. What was not done

- No production code, migration, schema or API.
- No task started or closed by this refinement.
- No P1/P2 marked closed.
- No fiscal/accounting/legal values validated.
- No Terraform apply or environment change.
- No new story ID was assigned; `US-CON-004` is marked as proposed.

## 7. Evidence

- [hardening-refinement.md](../../../docs/architecture/hardening-refinement.md)
- [architecture-hardening-plan.md](../../../docs/architecture/architecture-hardening-plan.md)
- [spike-verification-2026-11-08.md](../../../docs/architecture/spike-verification-2026-11-08.md)
- [sprint-backlog.md](../../../backlog/sprint-backlog.md)
- [releases.md](../../../backlog/releases.md)
- [execution-log.md](execution-log.md)

## 8. Conclusion

T-003 meets its acceptance criteria: the hardening work is mapped to stories, ADRs, tests, owners and dependencies, and spike evidence is explicitly separated from production. No finding is closed. The next task is **T-004**, and **T-005** remains the first code task.
