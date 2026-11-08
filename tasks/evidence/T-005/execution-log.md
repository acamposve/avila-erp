# T-005 — Execution log

- **Fecha:** 2026-11-08
- **Tarea:** [T-005 — Contexto tenant, UoW y RLS](../../T-005-tenancy-uow.md)
- **Preparado por:** agente de IA
- **Responsable/aprobador:** @acamposve
- **Rama:** `feature/t-005-tenancy-uow`
- **Commit base:** `ab71fa5` (`Merge pull request #7`)
- **Commit de evidencia:** pendiente al redactar; ver el PR de la rama
- **PR:** pendiente al redactar; se registra al abrir el PR
- **Entorno:** local Windows + PostgreSQL 18 Testcontainers
- **Estado:** `implemented` — pendiente de revisión humana; **no** `verified`

## Comandos ejecutados

| # | Comando | Resultado | Evidencia |
|---|---|---|---|
| 1 | `dotnet test Avila.slnx --logger "console;verbosity=minimal"` | **38/38 passed** (19 spike + 19 producción) | [`raw/test-output.txt`](raw/test-output.txt) |
| 2 | `GET http://127.0.0.1:5099/health` | HTTP 200 `Healthy` | [`raw/api-health.txt`](raw/api-health.txt) |
| 3 | Migración `PlatformDbContext` | `platform.tenants` global; `platform.tenant_settings` con RLS forzada | [`src/Avila.Platform/Migrations/20261108180733_Initial.cs`](../../../src/Avila.Platform/Migrations/20261108180733_Initial.cs) |
| 4 | Pruebas de producción | 19/19 | [`raw/test-output.txt`](raw/test-output.txt) |
| 5 | Comprobación de enlaces Markdown | 0 enlaces roto | Reporte del PR |

## Resultado

- `TenantUnitOfWork` mantiene una conexión y una transacción por caso de uso; `set_config(..., true)` no deja tenant residual en el pool.
- `TenantUnitOfWorkMiddleware` abre/commitea/revierte la UoW de `/api`; rechaza una petición sin tenant resuelto.
- `platform.tenants` es global y **no** se concede a `avila_app`; `platform.tenant_settings` tiene RLS forzada.
- `avila_app` no es superusuario y no tiene `BYPASSRLS`.
- El backfill corre por tenant, con una UoW/transacción por tenant.
- Las pruebas de límites de módulo no encuentran referencias directas entre implementaciones.
- T-005 queda `implemented`; no se declara cerrado ningún P1.

## Bloqueos registrados

| Bloqueo | Dueño | Estado |
|---|---|---|
| Identidad real que resuelva `ITenantContext` | @acamposve / T-006 | Pendiente; middleware listo, sin endpoint de negocio |
| Endpoints de negocio autenticados | T-006 | Pendiente |
| PgBouncer (`T-07b`) | @acamposve / Operaciones | No adoptado por `D-07`; N/A |
| Roles de producción (`avila_app`, `avila_migrator`) | @acamposve / Operaciones | Solo provisionados en Testcontainers |
| Cobertura `platform` más allá de `tenant_settings` | T-006/T-008/T-010 | Pendiente |
| Aprobación de este PR | @acamposve | Pendiente |

## Nota de alcance

Este log no implementa T-006 (identidad), no expone endpoints de negocio y no aplica migraciones en entornos compartidos. El middleware queda conectado a `/api`, pero ningún endpoint `/api` está publicado todavía.
