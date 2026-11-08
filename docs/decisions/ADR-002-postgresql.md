# ADR-002 — PostgreSQL como base de datos

- **Estado:** Aceptado
- **Fecha:** 2026-11-08
- **Decisores:** Arquitectura, Operaciones
- **Reemplaza a:** —

## 1. Contexto

El sistema requiere: transacciones ACID, integridad referencial estricta, tipos numéricos exactos, restricciones e índices compuestos por tenant, aislamiento por fila como defensa adicional, consultas analíticas razonables, JSON para metadatos acotados, respaldos con recuperación a un punto en el tiempo y operación administrada en la nube.

El equipo trabaja con C#/.NET y Entity Framework Core. La base debe soportar un modelo multi-tenant compartido y crecer por releases sin cambiar de motor.

## 2. Decisión

Se adopta **PostgreSQL 18** como base de datos relacional única del producto, con:

- **Npgsql** y **EF Core 10** como acceso principal; SQL explícito para reportes y operaciones masivas.
- **Un esquema por módulo** (`sales`, `inventory`, `billing`, `accounting`, …) y un esquema `platform` para tenancy, identidad, auditoría, numeración y outbox.
- **Row Level Security (RLS)** habilitada y forzada en tablas de negocio.
- **Tipos exactos:** `numeric` para dinero y cantidades, `uuid` para identificadores, `timestamptz` para marcas de tiempo, `date` para fechas de negocio.
- **Índices y constraints compuestos** que incluyan `tenant_id`.
- **Recuperación a un punto en el tiempo (PITR)** y respaldos verificados.
- Servicio administrado (o PostgreSQL autogestionado con operación equivalente) con réplica de lectura para reportes en etapas posteriores.

## 3. Consecuencias

### Positivas
- RLS permite una defensa adicional real contra fugas entre tenants.
- `numeric` evita errores de punto flotante en dinero.
- Restricciones diferibles, `CHECK`, índices parciales y `EXCLUDE` cubren invariantes difíciles de expresar en el dominio.
- Excelente soporte de JSONB para metadatos y auditoría.
- Particionado declarativo y réplicas para crecimiento.
- Licencia permisiva y ecosistema maduro; sin costo de licencia por núcleo.

### Negativas
- Requiere disciplina de vacío (`VACUUM`), estadísticas y tuning de consultas.
- RLS exige roles correctamente configurados y pruebas dedicadas; mal configurada da falsa seguridad.
- El pool de conexiones debe manejar `SET LOCAL` por transacción; PgBouncer en modo transacción requiere cuidado.
- Migraciones de larga duración deben diseñarse en línea y por fases.

## 4. Alternativas consideradas

| Alternativa | Motivo de descarte |
|---|---|
| SQL Server | Costo de licencia, menor afinidad con el stack cloud elegido, RLS menos flexible para este caso |
| MySQL / MariaDB | Menor soporte de RLS, tipos y extensiones; ecosistema menos adecuado para analítica |
| Base de datos por tenant | Complejidad operativa y de migraciones; se reserva para tenant dedicado en R6 |
| Almacén NoSQL | No garantiza integridad relacional ni transacciones multi-entidad requeridas |

## 5. Reglas operativas

1. Versión mínima soportada: **PostgreSQL 18**. Las extensiones se aprueban por ADR.
2. Roles: `avila_app` (sin `BYPASSRLS`, sin DDL), `avila_migrator` (DDL controlado), `avila_platform_ops` (operaciones auditadas).
3. Migraciones con EF Core por módulo, historial en el esquema del módulo, forward-only y compatibles hacia atrás.
4. Prohibido deshabilitar RLS en producción; cualquier excepción requiere aprobación y auditoría.
5. Respaldos: PITR 35 días, snapshots diarios 30 días, mensuales 12 meses (sujeto a `DEC-008`).
6. Extensiones previstas: `pgcrypto` (si se requiere cifrado), `pg_trgm` (búsqueda), `unaccent` (búsqueda en español). Cada una requiere revisión.
7. Conexiones: pool acotado por instancia; `SET LOCAL app.tenant_id` dentro de transacción; nunca `SET` global.

## 6. Verificación

- Prueba de restauración PITR trimestral.
- Introspección de esquema que verifica RLS, `tenant_id` e índices.
- Pruebas de concurrencia con PostgreSQL real (Testcontainers), no con proveedor en memoria.
- Revisión de planes de consulta para las consultas críticas.

## 7. Referencias

- [database-design.md](../architecture/database-design.md)
- [multi-tenancy.md](../architecture/multi-tenancy.md)
- [ADR-003](ADR-003-multi-tenancy.md)
