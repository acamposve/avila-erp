# T-008 — Execution log

- **Fecha:** 2026-11-14
- **Tarea:** [T-008 — Permisos, auditoría y soporte](../../T-008-authorization-audit.md)
- **Preparado por:** agente de IA
- **Responsable/aprobador:** @acamposve
- **Rama:** `feature/t-008-authorization-audit`
- **Commit base:** `8262783` (`Merge pull request #10`)
- **Commit de evidencia:** `a56431f`
- **PR:** [#11](https://github.com/acamposve/avila-erp/pull/11)
- **Entorno:** local Windows + PostgreSQL 18 Testcontainers
- **Estado:** `implemented` — pendiente de revisión humana; **no** `verified`

## Comandos ejecutados

| # | Comando | Resultado | Evidencia |
|---|---|---|---|
| 1 | `dotnet test Avila.slnx --logger "console;verbosity=minimal"` | **54/54 passed** (19 spike + 35 producción) | [`raw/test-output.txt`](raw/test-output.txt) |
| 2 | `GET http://127.0.0.1:5099/health` | HTTP 200 `Healthy` | [`raw/api-health.txt`](raw/api-health.txt) |
| 3 | Migración `AddAuthorizationAuditSupport` | `access_scopes`, `audit_events`, `support_access_grants` con RLS y trigger append-only | [`src/Avila.Platform/Migrations/20261114195019_AddAuthorizationAuditSupport.cs`](../../../src/Avila.Platform/Migrations/20261114195019_AddAuthorizationAuditSupport.cs) |
| 4 | Pruebas de autorización/auditoría | 5/5 | [`raw/test-output.txt`](raw/test-output.txt) |
| 5 | Comprobación de enlaces Markdown | 0 enlaces roto | Reporte del PR |

## Resultado

- RBAC por plantilla: `owner`/`admin` con alcance total; `member` con permisos limitados.
- Scope explícito empresa/sucursal/almacén en `platform.access_scopes`; sin scope, denegado.
- Permiso denegado no modifica datos; no hay escritura de negocio.
- Auditoría con actor, tenant, antes/después, correlación y timestamp.
- `platform.audit_events` es append-only: trigger y grants bloquean UPDATE/DELETE.
- Acceso de soporte con ticket, justificación, expiración y solo lectura por defecto; otorgamiento/revocación auditados.
- T-008 queda `implemented`; no se declara ningún P1 cerrado.

## Bloqueos registrados

| Bloqueo | Dueño | Estado |
|---|---|---|
| Endpoint HTTP con `403` y auditoría del intento | T-008/T-012 | Servicio listo; no hay endpoint de negocio protegido |
| Catálogo definitivo de permisos y roles personalizados | @acamposve / Producto | Plantillas estáticas; sin UI/CRUD |
| Auditoría de intentos denegados en transacción separada | @acamposve | Pendiente; el rechazo no persiste auditoría si el llamador revierte |
| Exportación/consulta de auditoría para auditor | T-008+ | Solo tabla y consulta ORM |
| Operador de plataforma real para soporte | T-006/T-011 | `SupportUserId` sin rol de plataforma dedicado |
| Roles/grants de producción | Operaciones | Solo Testcontainers |
| Aprobación de este PR | @acamposve | Pendiente |

## Nota de alcance

Este log no implementa endpoints de negocio, no expone una consola de soporte y no crea roles personalizados. La autorización se cubre a nivel de servicio; el `403` HTTP y la auditoría del intento quedan para la capa de endpoints.
