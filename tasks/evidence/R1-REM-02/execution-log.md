# R1-REM-02 — Execution log

- **Fecha:** 2026-11-14
- **Tarea:** R1-REM-02 — Roles y grants de PostgreSQL
- **Preparado por:** agente de IA
- **Responsable/aprobador:** @acamposve
- **Rama:** `feat/r1-rem-02-postgres-roles`
- **Worktree:** `C:\proyectos\avila-erp-rem02`
- **Commit base:** `cae8bbd`
- **Commit de evidencia:** `cd5f3f6`
- **PR:** [#18](https://github.com/acamposve/avila-erp/pull/18)
- **Estado:** `implemented` — pendiente de revisión humana; **no** `verified`

## Comandos ejecutados

| # | Comando | Resultado | Evidencia |
|---|---|---|---|
| 1 | `dotnet test Avila.slnx` | **73/73 passed** (19 spike + 54 producción) | [`raw/test-output.txt`](raw/test-output.txt) |
| 2 | `RoleBootstrapTests` | 1/1; roles idempotentes y least privilege | [`raw/test-output.txt`](raw/test-output.txt) |
| 3 | `roles.sql` ejecutado dos veces | Sin error; atributos y grants verificados | [`ops/postgres/roles.sql`](../../../ops/postgres/roles.sql) |
| 4 | Comprobación de enlaces Markdown | 0 enlaces roto | Reporte del PR |

## Resultado

- `avila_app`, `avila_identity` y `avila_platform` se crean sin contraseñas y sin privilegios elevados.
- `avila_app` tiene DML solo en tablas de negocio/tenant; no lee `platform.tenants` ni `identity.*`.
- `avila_identity` solo accede a `identity`; grants suficientes para la futura exportación de identidad.
- `avila_platform` solo accede a `tenants` y `tenant_lifecycle`.
- Auditoría: app puede SELECT/INSERT, no UPDATE/DELETE.
- Testcontainers ejecuta exactamente `ops/postgres/roles.sql`; solo las credenciales desechables se asignan después. Las credenciales de test son aleatorias por ejecución y no están en el repositorio.
- El script es idempotente.
- T-012 permanece cerrado; no se abre G1.

## Bloqueos / pendientes

| Pendiente | Dueño | Estado |
|---|---|---|
| Aplicación de roles en producción | Operaciones | No ejecutada; script listo, requiere autorización |
| Grants de tablas futuras (R2) | Plataforma | Cada migración deberá añadir grant explícito; sin `DEFAULT PRIVILEGES` amplios |
| RLS audit/policy registry | R1-REM-03 | Planificado |
| Aprobación de este PR | @acamposve | Pendiente |

## Nota de alcance

No se despliega, no se crean usuarios reales ni contraseñas de producción, y no se aplican migraciones en entornos compartidos.
