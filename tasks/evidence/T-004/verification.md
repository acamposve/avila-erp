# T-004 — Verification

**Task:** [T-004 — Revisar coherencia de la dependencia R5→R3](../../T-004-r5-dependency.md)
**Status:** `implemented` — awaiting human review. Not `verified`.
**Date:** 2026-11-08

## 1. Acceptance criteria

| Criterio de T-004 | Evidencia | Resultado |
|---|---|---|
| La revisión identifica secciones y decisión vigente | [r5-r3-dependency-review.md](../../../docs/architecture/r5-r3-dependency-review.md) §2–§3 | **Pass** |
| Produce una propuesta verificable | §4: exact one-line diff, option A vs B, recommendation | **Pass** |
| La dependencia permanece explícita y bloqueada | §5; `NOM-008`/`AC-NOM-04` retained; roadmap untouched | **Pass** |
| No modifica el alcance unilateralmente | No roadmap or requirement changed; `F-23` pending approval | **Pass** |

## 2. Contradiction found

| Location | Current statement | Correct? |
|---|---|---|
| `roadmap.md §2` summary table | R5 depends on `R1, R2` | **No** |
| `roadmap.md §7` dependencies | `R1; R2 (tesorería); R3 (contabilidad y provisión)` | Yes |
| `roadmap.md §10` diagram | `G3 → R5` | Yes |
| `releases.md §6` | `R1/R2/R3` | Yes |
| `releases.md §8` matrix | `R5 → R1, R2, R3` | Yes |
| `project-execution-plan.md §3` | R5 requires R3 dependency resolved | Yes |
| `AC-NOM-04` | Payroll payment and provision/journal reconcile | Yes; requires CON |
| `NOM-008` | Payroll accounting provision | Yes; requires CON |

One contradictory table, seven coherent references.

## 3. Decision and proposal

`D-04` is approved: add R3 or remove `NOM-008` from R5.

The corpus already retains `NOM-008` and `AC-NOM-04`, so the coherent option is:

```diff
- R5 depends on R1, R2
+ R5 depends on R1, R2, R3
```

This is a one-line consistency correction. It does not change scope.

Option B — removing `NOM-008` and accounting criteria from R5 — would be a scope change and is not recommended.

## 4. Why the dependency remains blocked

- The roadmap table is not yet corrected.
- `F-23` is `Pendiente de aprobación`.
- `T-13` has not verified the final state.
- R5 cannot start until R3 is validated and G3 is open.
- No payroll, accounting or provision code exists.

## 5. What was not done

- No roadmap/requirements edit.
- No code, migration or infrastructure.
- No payroll or accounting implementation.
- No legal/accounting validation.
- No R5 start.

## 6. Evidence

- [r5-r3-dependency-review.md](../../../docs/architecture/r5-r3-dependency-review.md)
- [documentation-review.md](../../../docs/documentation-review.md) — F-23
- [roadmap.md](../../../docs/product/roadmap.md)
- [releases.md](../../../backlog/releases.md)
- [acceptance-criteria.md](../../../docs/requirements/acceptance-criteria.md)
- [functional-requirements.md](../../../docs/requirements/functional-requirements.md)
- [execution-log.md](execution-log.md)

## 7. Conclusion

T-004 meets its acceptance criteria: it identifies the contradiction, cites the current decision, and produces a one-line correction proposal without changing scope. `AUD-P1-09` remains open until the correction is approved, applied and verified by T-13.
