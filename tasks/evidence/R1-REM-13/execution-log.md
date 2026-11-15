# R1-REM-13 — Execution log

- **Fecha:** 2026-11-15
- **Tarea:** R1-REM-13 — Full-schema backup/restore rehearsal
- **Preparado por:** agente de IA
- **Responsable/aprobador:** @acamposve
- **Rama:** `feat/r1-rem-13-restore-drill`
- **Commit base:** `6a13bdc`
- **Commit de evidencia:** `1fd2a57`
- **PR:** [#27](https://github.com/acamposve/avila-erp/pull/27)
- **Estado:** `implemented` — pendiente de revisión humana; **no** `verified`

## Comandos ejecutados

| # | Comando | Resultado | Evidencia |
|---|---|---|---|
| 1 | `dotnet test Avila.slnx` | **110/110 passed** (19 spike + 91 producción) | [`raw/test-output.txt`](raw/test-output.txt) |
| 2 | `FullRestoreRehearsalTests` | 1/1; dump/restore, RLS, counts, hash chain | [`raw/test-output.txt`](raw/test-output.txt) |
| 3 | Comprobación de enlaces Markdown | 0 enlaces roto / 186 archivos | [`raw/link-check.txt`](raw/link-check.txt) |

## Resultado

- `pg_dump -Fc` de la base completa.
- `pg_restore` en `avila_restore` dentro del mismo contenedor.
- Dos tenants sintéticos restaurados.
- RLS/force RLS y políticas presentes.
- Lectura RLS de A no mezcla B.
- Cadena de auditoría SHA-256 válida tras el restore.
- Test automatizado en CI (`dotnet test`).
- T-013 no se re-ejecuta; G1 sigue cerrado.

## Bloqueos / pendientes

| Pendiente | Dueño | Estado |
|---|---|---|
| Restore en entorno autorizado con volumen real | Operaciones | No ejecutado |
| PITR/WAL/cifrado en reposo | Operaciones | Fuera de alcance |
| RPO/RTO medidos | Operaciones | Provisional 24h/4h |
| Restore de roles/cluster | Operaciones | Roles presentes por el mismo contenedor |
| Aprobación de este PR | @acamposve | Pendiente |

## Nota de alcance

El simulacro usa Testcontainers y datos sintéticos; no es un restore de producción.
