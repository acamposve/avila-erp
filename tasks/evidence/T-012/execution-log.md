# T-012 — Execution log

- **Fecha:** 2026-11-14
- **Tarea:** [T-012 — Operación, CI y recuperación](../../T-012-operations.md)
- **Preparado por:** agente de IA
- **Responsable/aprobador:** @acamposve
- **Rama:** `feature/t-012-operations`
- **Commit base:** `4042d9c` (`Merge pull request #14`)
- **Commit de evidencia:** `0a8e6ad`
- **PR:** [#15](https://github.com/acamposve/avila-erp/pull/15) — fusionado
- **Aprobación:** @acamposve, 2026-11-14
- **Merge commit:** `cae8bbd`
- **Entorno:** local Windows + PostgreSQL 18 Testcontainers + Docker desechable
- **Estado:** `verified` — aprobado y fusionado

## Comandos ejecutados

| # | Comando | Resultado | Evidencia |
|---|---|---|---|
| 1 | `dotnet test Avila.slnx --logger "console;verbosity=minimal"` | **72/72 passed** (19 spike + 53 producción) | [`raw/test-output.txt`](raw/test-output.txt) |
| 2 | `GET /health`, `/health/live`, `/health/ready`, `/metrics`, `/` | 200; correlación presente | [`raw/api-observability.txt`](raw/api-observability.txt) |
| 3 | `scripts/backup-restore-rehearsal.ps1` | PASS: dump+restore, 2 filas, 1 por tenant post-RLS | [`raw/restore-rehearsal.txt`](raw/restore-rehearsal.txt) |
| 4 | Pruebas de observabilidad | 3/3 | [`raw/test-output.txt`](raw/test-output.txt) |
| 5 | CI de main/PR #14 | Success; build + 69 tests + migraciones limpias | [`raw/ci-baseline.txt`](raw/ci-baseline.txt) |
| 6 | Comprobación de enlaces Markdown | 0 enlaces roto | Reporte del PR |

## Resultado

- `/health` y `/health/live` sin dependencia de base.
- `/health/ready` consulta PostgreSQL; degradado si falta configuración.
- `/metrics` expone contadores agregados sin tenant/usuario/ruta.
- `X-Correlation-ID` se propaga o se genera; logs sin query/body/PII.
- Exception handler devuelve ProblemDetails genérico con `traceId`.
- Runbook, observabilidad y procedimiento de restore documentados.
- Simulacro local reproducible con PostgreSQL desechable: PASS.
- T-012 queda **`verified`**: aprobado y fusionado en PR #15 (`cae8bbd`).

## Bloqueos registrados

| Bloqueo | Dueño | Estado |
|---|---|---|
| Alertas/dashboards reales | Operaciones | No configurados |
| OpenTelemetry/exportador de trazas | @acamposve | No implementado; solo correlación/logs |
| Auth/allowlist de `/metrics` | Seguridad/T-012 | Pendiente; restringir por red |
| Backup/restore en entorno autorizado | @acamposve | **No ejecutado**; solo desechable |
| RPO/RTO/retención | @acamposve + `DEC-008` | Provisionales, sin aprobar |
| Migración/backfill real | Operaciones | Procedimiento documentado; no ejecutado |
| Aprobación de este PR | @acamposve | **Fusionado** |

## Nota de alcance

No se despliega, no se toca infraestructura compartida y no se ejecuta un restore de producción. El simulacro usa una tabla sintética con RLS, no la base completa de ÁVILA; sus límites están en [`docs/operations/backup-restore.md`](../../../docs/operations/backup-restore.md).
