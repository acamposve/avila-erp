# T-004 — Execution log

- **Fecha:** 2026-11-08
- **Tarea:** [T-004 — Revisar coherencia de la dependencia R5→R3](../../T-004-r5-dependency.md)
- **Preparado por:** agente de IA
- **Responsable/aprobador:** @acamposve
- **Rama:** `docs/t-004-r5-dependency`
- **Commit base:** `93bd224` (`Merge pull request #6`)
- **Commit de evidencia:** pendiente al redactar; ver el PR de la rama
- **PR:** pendiente al redactar; se registra al abrir el PR
- **Entorno:** local Windows; documentación, sin cambios de entorno
- **Estado:** `implemented` — pendiente de revisión humana; **no** `verified`

## Comandos ejecutados

| # | Comando | Resultado | Evidencia |
|---|---|---|---|
| 1 | `Select-String` de `R5`, `R3`, `NOM-008` en `docs/` y `backlog/` | 8 referencias comparadas; una contradicción | [review](../../../docs/architecture/r5-r3-dependency-review.md) |
| 2 | Lectura de `releases.md §6/§8` | R5 = `R1/R2/R3` | [releases](../../../backlog/releases.md) |
| 3 | Lectura de `acceptance-criteria.md §10` | `AC-NOM-04` exige pago y provisión/asiento | [AC](../../../docs/requirements/acceptance-criteria.md) |
| 4 | Lectura de `functional-requirements.md §12` | `NOM-008` provisión contable; `NOM-010` pago | [RF](../../../docs/requirements/functional-requirements.md) |
| 5 | `dotnet test Avila.slnx` | 21/21 (baseline; sin cambios de código) | [`../T-001/raw/test-output.txt`](../T-001/raw/test-output.txt) |
| 6 | Comprobación de enlaces Markdown | 0 enlaces roto | Reporte del PR |

## Resultado

- Se identificó la contradicción: `roadmap.md §2` dice R1/R2; el resto del corpus dice R1/R2/R3.
- La decisión `D-04` está aprobada; la opción coherente es **añadir R3**, no retirar `NOM-008`.
- Se registró `F-23` en [documentation-review.md](../../../docs/documentation-review.md) como **pendiente de aprobación**.
- No se modificó el roadmap ni el alcance.
- T-004 queda `implemented`; la corrección de la tabla y `T-13` siguen pendientes.

## Bloqueos registrados

| Bloqueo | Dueño | Estado |
|---|---|---|
| Aprobación de la corrección de `roadmap.md §2` | @acamposve | Pendiente |
| `T-13` verificación documental | @acamposve | Pendiente |
| R5 depende de R3 y G3 | @acamposve | Explícito y bloqueado |
| Aprobación de este PR | @acamposve | Pendiente |

## Nota de alcance

Este log no modifica el roadmap, no implementa nómina ni contabilidad y no ejecuta migraciones. La dependencia R5→R3 permanece bloqueada hasta la corrección aprobada y la verificación `T-13`.
