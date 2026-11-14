# RLS, policy registry y auditoría de denegaciones

**Estado:** R1-REM-03 · **Fecha:** 2026-11-14 · **Dueño:** @acamposve

## 1. Registro de políticas

`platform.rls_policy_registry` es el inventario de tablas tenant-scoped:

| Columna | Uso |
|---|---|
| `table_schema`, `table_name` | PK de la tabla |
| `tenant_column` | Debe ser `tenant_id` |
| `policy_name` | `tenant_isolation` |
| `is_required` | `true` para tablas de negocio |
| `created_at` | Alta en el registro |

La migración inicial lo puebla desde `information_schema.columns` para todas las tablas con `tenant_id` en `admin`, `inventory`, `sales`, `billing` y `platform`.

### Añadir una tabla tenant-scoped

1. Crear la tabla con `tenant_id NOT NULL`.
2. Habilitar y forzar RLS.
3. Crear la política `tenant_isolation` con `current_setting('app.tenant_id', true)`.
4. Insertar la fila en `platform.rls_policy_registry`.
5. Ejecutar `RlsPolicyTests`; si falta cualquier paso, el build falla.

La prueba consulta el catálogo **real** de PostgreSQL (`pg_class`, `pg_policy`, `information_schema`) en Testcontainers. No valida solo el modelo EF.

## 2. Denegaciones auditadas

`SecurityAuditWriter` abre una **UoW/conexión/transacción independiente** para escribir `authorization.denied` en `platform.audit_events` y la confirma antes de volver al llamador.

Consecuencia:

- si la transacción de negocio se revierte, la denegación **permanece**;
- el evento registra tenant objetivo, actor, tenant del actor, permiso, motivo, scope y correlación;
- no se registran tokens, contraseñas ni PII.

Tipos:

| Acción | Motivo |
|---|---|
| `authorization.denied` | `permission`, `scope`, `cross_tenant` |

La autorización llama al writer antes de lanzar `AuthorizationException`. Si el writer falla, la operación falla de forma cerrada.

## 3. Pruebas

| Test | Verifica |
|---|---|
| `Live_catalog_has_forced_rls_and_registered_policy_for_every_tenant_table` | Toda tabla con `tenant_id` está registrada, tiene RLS/force y política con `app.tenant_id`; incluye `pg_class`/`pg_policy` |
| `Denied_permission_is_audited_after_business_transaction_rolls_back` | Evento persiste tras rollback; correlación y motivo |
| `Cross_tenant_attempt_is_audited_in_target_tenant_after_rollback` | Intento A→B auditado en el tenant objetivo, actor/tenant de origen |

## 4. Límites

- PostgreSQL no registra por sí solo cada lectura bloqueada por RLS; se auditan intentos de autorización de la aplicación. Un acceso directo por `psql` no genera un evento de aplicación.
- La cadena de hash y la consulta/exportación de auditoría son R1-REM-10.
- La aplicación de roles en producción sigue siendo una acción autorizada de operaciones.
- El registro no reemplaza la introspección de CI; ambos deben existir.
