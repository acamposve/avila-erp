# R1-REM-03 — Execution log

- **Fecha:** 2026-11-14
- **Tarea:** R1-REM-03 — RLS audit, policy registry, denied-attempt audit
- **Preparado por:** agente de IA
- **Responsable/aprobador:** @acamposve
- **Rama:** `feat/r1-rem-03-rls-audit`
- **Commit base:** `005bd70`
- **Commit de evidencia:** `6deaf4a`
- **PR:** [#19](https://github.com/acamposve/avila-erp/pull/19)
- **Estado:** `implemented` — pendiente de revisión humana; **no** `verified`

## Comandos ejecutados

| # | Comando | Resultado | Evidencia |
|---|---|---|---|
| 1 | `dotnet test Avila.slnx` | **76/76 passed** (19 spike + 57 producción) | [`raw/test-output.txt`](raw/test-output.txt) |
| 2 | `RlsPolicyTests` | 1/1; catálogo real + registro + `pg_policy` | [`raw/test-output.txt`](raw/test-output.txt) |
| 3 | `SecurityAuditTests` | 2/2; auditoría persiste tras rollback | [`raw/test-output.txt`](raw/test-output.txt) |
| 4 | Migración `AddRlsPolicyRegistry` | Registro de todas las tablas con `tenant_id` | [`src/Avila.Platform/Migrations/20261114214054_AddRlsPolicyRegistry.cs`](../../../src/Avila.Platform/Migrations/20261114214054_AddRlsPolicyRegistry.cs) |
| 5 | Comprobación de enlaces Markdown | 0 enlaces roto / 155 archivos | [`raw/link-check.txt`](raw/link-check.txt) |

## Resultado

- `platform.rls_policy_registry` inventaría cada tabla tenant-scoped.
- La prueba introspecta `pg_class` y `pg_policy` en PostgreSQL vivo: RLS, force RLS y política con `app.tenant_id` son obligatorios.
- Una tabla con `tenant_id` no registrada o sin política falla el build.
- `SecurityAuditWriter` escribe la denegación en una UoW/conexión/transacción independiente y la confirma antes de retornar.
- El rollback de la transacción de negocio no borra el evento de seguridad.
- Intentos de permiso y cross-tenant quedan auditados con actor, tenant objetivo, tenant de origen, motivo y correlación.
- T-013 no se re-ejecuta todavía; G1 sigue cerrado.

## Bloqueos / pendientes

| Pendiente | Dueño | Estado |
|---|---|---|
| Cadena de hash y consulta/exportación de auditoría | R1-REM-10 | Planificado |
| Aplicación de la migración/roles en producción | Operaciones | Requiere autorización |
| Cobertura de accesos directos por `psql` | Seguridad | PostgreSQL no emite un evento de aplicación; documentado |
| Aprobación de este PR | @acamposve | Pendiente |

## Nota de alcance

No se implementa la cadena de hash, el endpoint de consulta/exportación ni purga/legal hold. No se despliega ni se aplican migraciones compartidas.
