# R1-REM-04 — Execution log

- **Fecha:** 2026-11-14
- **Tarea:** R1-REM-04 — Tenant context for background jobs/workers
- **Preparado por:** agente de IA
- **Responsable/aprobador:** @acamposve
- **Rama:** `feat/r1-rem-04-tenant-jobs`
- **Commit base:** `005bd70`
- **Commit de evidencia:** `e8ca2f6`
- **PR:** [#20](https://github.com/acamposve/avila-erp/pull/20)
- **Estado:** `implemented` — pendiente de revisión humana; **no** `verified`

## Comandos ejecutados

| # | Comando | Resultado | Evidencia |
|---|---|---|---|
| 1 | `dotnet test Avila.slnx` | **77/77 passed** (19 spike + 58 producción; base main 54 + 4 nuevos) | [`raw/test-output.txt`](raw/test-output.txt) |
| 2 | `TenantJobRunnerTests` | 4/4 | [`raw/test-output.txt`](raw/test-output.txt) |
| 3 | Comprobación de enlaces Markdown | 0 enlaces roto / 155 archivos | [`raw/link-check.txt`](raw/link-check.txt) |

## Resultado

- `ITenantJobRunner` rechaza `Guid.Empty` y `job` nulo en el límite.
- Limpia `TenantExecutionContext` de forma síncrona antes de iniciar.
- Cada job usa UoW con `SET LOCAL app.tenant_id` explícito.
- 24 jobs A/B intercalados mantienen aislamiento; 12 empresas por tenant.
- Un job fallido revierte y no deja empresa.
- `TenantAccessGuard` bloquea jobs de tenants suspendidos.
- T-013 no se re-ejecuta; G1 sigue cerrado.

## Bloqueos / pendientes

| Pendiente | Dueño | Estado |
|---|---|---|
| Outbox/worker real, cola, reintentos y dead-letter | R2+ | Fuera de alcance |
| Hosted service/registro DI del worker | R2+ | No implementado |
| Aislamiento de jobs de solo lectura | R1-REM-04+ | Contrato actual es de escritura |
| Aprobación de este PR | @acamposve | Pendiente |

## Nota de alcance

No se implementa una cola, un worker hosted ni reintentos. El runner es in-process y sirve como frontera de tenant para futuros consumidores.
