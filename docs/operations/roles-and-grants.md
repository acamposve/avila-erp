# Roles y grants de PostgreSQL

**Estado:** R1-REM-02 · **Fecha:** 2026-11-14 · **Dueño:** @acamposve

## 1. Principio

Un solo archivo, [`ops/postgres/roles.sql`](../../ops/postgres/roles.sql), define los roles y grants de PostgreSQL. No contiene contraseñas. Se ejecuta **después** de las migraciones y es idempotente.

```bash
psql "$AVILA_ADMIN_CONNECTION" -v ON_ERROR_STOP=1 -f ops/postgres/roles.sql
```

Los roles se crean `NOLOGIN`. El despliegue/gestor de secretos otorga `LOGIN` y contraseña fuera del repositorio. Testcontainers ejecuta el mismo archivo y solo después asigna credenciales desechables.

## 2. Roles

| Rol | Propósito | Acceso |
|---|---|---|
| `avila_app` | API y comandos de negocio | Tablas de negocio con RLS + tablas `platform` por tenant |
| `avila_identity` | Login, membresías, MFA, sesiones | Solo esquema `identity` |
| `avila_platform` | Aprovisionamiento y lifecycle | Solo `platform.tenants` y `platform.tenant_lifecycle` |

Ninguno es superusuario, tiene `BYPASSRLS`, `CREATEDB` o `CREATEROLE`. `avila_app` **no** puede leer `platform.tenants` ni `identity.*`.

## 3. Grants

### `avila_app`

- `USAGE` en `admin`, `inventory`, `sales`, `billing`, `platform`.
- DML en `admin.companies`, `branches`, `parties`; `inventory.items`; ventas; borradores de factura; `platform.tenant_settings`.
- `platform.audit_events`: **SELECT, INSERT** (append-only).
- `platform.setting_versions`: **SELECT, INSERT** (append-only).
- `access_scopes`: SELECT, INSERT.
- `support_access_grants`, `document_sequences`, `idempotency_keys`, `approval_rules`, `approval_requests`, `feature_flags`, `usage_counters`: SELECT/INSERT/UPDATE según corresponda.
- `tenant_lifecycle`: **solo SELECT**; el cambio de estado usa `avila_platform`.

### `avila_identity`

- `USAGE` en `identity`.
- SELECT/INSERT/UPDATE/DELETE en todas las tablas de `identity`.
- Sin acceso a negocio, `platform` ni `public`.

### `avila_platform`

- `USAGE` en `platform`.
- SELECT/INSERT/UPDATE en `platform.tenants` y `platform.tenant_lifecycle`.
- Sin acceso a negocio, identidad ni auditoría.

## 4. Identidad y exportación

La futura exportación de identidad (R1-REM-06) usa `avila_identity`, no `avila_app`, y filtra por membresía del tenant. El grant de identidad es intencionalmente amplio **solo dentro de `identity`**; no incluye `platform` ni tablas de negocio.

La exportación debe excluir `password_hash`, `mfa_secret`, recovery codes, tokens de sesión y hashes de tokens.

## 5. Verificación

`RoleBootstrapTests` ejecuta el mismo `roles.sql` dos veces sobre Testcontainers y comprueba:

- atributos `rolsuper=false`, `rolbypassrls=false`, `rolcreatedb=false`, `rolcreaterole=false`;
- `avila_app` puede leer `admin.companies` y no `platform.tenants`/`identity.users`;
- `avila_identity` puede leer `identity.users` y no `admin.companies`;
- `avila_platform` puede actualizar `platform.tenants` y no `admin.companies`;
- auditoría: app puede INSERT/SELECT, no UPDATE/DELETE.

## 6. Operación

1. Aplicar migraciones con el rol administrador/migrador.
2. Ejecutar `roles.sql` con el mismo administrador.
3. Configurar `LOGIN`/credenciales con el gestor de secretos.
4. Verificar atributos y grants; no ejecutar `GRANT ALL`.
5. Cualquier tabla nueva requiere grant explícito y prueba de RLS; no se usan `DEFAULT PRIVILEGES` amplios.

## 7. Límites

- El script no crea bases de datos, extensiones ni contraseñas.
- No aplica migraciones.
- No reemplaza la revisión de seguridad del PR.
- El despliegue real de roles/credenciales sigue siendo una acción de operaciones autorizada.
