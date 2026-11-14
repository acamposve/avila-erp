# T-013 — Execution log

- **Fecha:** 2026-11-14
- **Tarea:** [T-013 — Preparar evidencia de salida R1](../../T-013-r1-gate.md)
- **Preparado por:** agente de IA
- **Responsable/aprobador:** @acamposve
- **Rama:** `docs/t-013-r1-gate`
- **Commit base:** `cae8bbd` (`Merge pull request #15`)
- **Commit de evidencia:** pendiente al redactar; ver el PR
- **PR:** pendiente al redactar; se registra al abrir el PR
- **Entorno:** local Windows + PostgreSQL 18 Testcontainers
- **Estado:** `implemented` — evidencia consolidada; **G1 permanece cerrado**
- **Resultado del gate:** **NO-GO**

## Comandos ejecutados

| # | Comando / revisión | Resultado | Evidencia |
|---|---|---|---|
| 1 | `dotnet test Avila.slnx --logger "console;verbosity=minimal"` | **72/72 passed** (19 spike + 53 producción) | [`raw/test-output.txt`](raw/test-output.txt) |
| 2 | API smoke `/health`, `/health/live`, `/health/ready`, `/metrics` | 200 + correlación | [`raw/api-smoke.txt`](raw/api-smoke.txt) |
| 3 | CI main `cae8bbd` + PR #15 | Success; build + tests + migraciones limpias | [`raw/ci-baseline.txt`](raw/ci-baseline.txt) |
| 4 | Matriz R1 de 12 criterios | 3 PASS, 8 PARTIAL, 1 FAIL → **NO-GO** | [`r1-gate-matrix.md`](r1-gate-matrix.md) |
| 5 | Historias R1 (17) | 12 PASS, 5 PARTIAL | [`stories-ac-matrix.md`](stories-ac-matrix.md) |
| 6 | Criterios de aceptación R1 (15) | 6 PASS, 9 PARTIAL | [`stories-ac-matrix.md`](stories-ac-matrix.md) |
| 7 | Decisiones, riesgos, ADR y P1 | Bloqueos documentados; 0 P1 cerrados | [`decisions-risks.md`](decisions-risks.md) |
| 8 | Comprobación de enlaces Markdown | 0 enlaces roto | Reporte del PR |

## Resultado

- Los 12 criterios de salida R1 están evaluados con evidencia y dueño.
- El criterio de **exportación completa** es **FAIL**.
- Ocho criterios son **PARTIAL** y bloquean el gate estricto.
- R1 no se aprueba; **G1 permanece cerrado**.
- La aprobación de este PR no abre G1 ni autoriza despliegue.
- Se solicita decisión humana: aceptar la matriz y elegir remediación (A/B/C).
- T-013 queda `implemented`; no se declara R1 aprobado.

## Bloqueos registrados

| Bloqueo | Dueño | Estado |
|---|---|---|
| Exportación completa del tenant | Plataforma | **FAIL** — solo maestros administrativos |
| Auditoría consultable/exportable | Seguridad | PARTIAL |
| Alertas, dashboards y trazas | Operaciones | PARTIAL |
| Escaneo de seguridad en CI | Plataforma/Seguridad | PARTIAL |
| Restore autorizado de producción | Operaciones | PARTIAL |
| ADR de identidad y `DEC-001`/`DEC-008` | @acamposve | PARTIAL |
| Aprobación de este PR | @acamposve | Pendiente |

## Nota de alcance

No se implementa código en T-013, no se abre G1, no se despliega y no se modifican datos. La matriz evalúa el estado real; no sustituye la decisión de release.
