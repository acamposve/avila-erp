# Audit integrity, query and export

**Estado:** R1-REM-10 · **Fecha:** 2026-11-15 · **Dueño:** @acamposve

## 1. Hash chain

`platform.audit_events` añade:

| Columna | Uso |
|---|---|
| `previous_hash` | Hash del evento anterior del tenant |
| `event_hash` | SHA-256 canónico del evento + previous |
| `hash_version` | `1` para eventos encadenados; `0` para legado |

- La escritura toma `pg_advisory_xact_lock` por tenant.
- El payload canónico incluye tenant, actor, acción, entidad, before/after canónicos, correlación, timestamp y previous.
- Las filas previas a la migración quedan `hash_version=0` y no participan en la cadena; los eventos nuevos inician/continúan la cadena v1.
- `AuditIntegrityService.VerifyAsync` recorre la cadena y detecta:
  - `previous_hash` incorrecto;
  - `event_hash` recalculado distinto;
  - eliminación de un evento intermedio (el siguiente previous no coincide).

## 2. Consulta

`GET /api/v1/admin/audit`

Filtros: `from`, `to`, `action`, `actorUserId`, `entityType`, `limit`, `cursor`.

- Cursor opaco (offset codificado en base64).
- Orden estable `occurred_at` + `id`.
- RLS por tenant.
- Requiere `administration.audit.read` (owner/admin).
- Cada consulta escribe `audit.queried`.

## 3. Exportación

`GET /api/v1/admin/audit/export`

- NDJSON: una línea por evento + manifest `{type:"manifest", record_count}`.
- Páginas de 200, escritura incremental.
- Escribe `audit.exported`.
- Mismo permiso y aislamiento de tenant.

## 4. Verificación

`GET /api/v1/admin/audit/verify`

Devuelve:

```json
{ "isValid": true, "checkedEvents": 42, "firstInvalidEventId": null, "reason": null }
```

Escribe `audit.verified`.

## 5. Audit-of-audit

`audit.queried`, `audit.exported` y `audit.verified` se escriben con `ExportAuditWriter` en transacción independiente, con actor, correlación y metadatos de filtros. No incluyen PII ni tokens.

## 6. Límites

- Cursor por offset puede desplazarse si se insertan eventos entre páginas; para exportaciones grandes se recomienda repetir hasta manifest.
- Filas legado `hash_version=0` no se verifican; no hay backfill retroactivo de hash.
- El último evento eliminado no puede detectarse solo con la cadena; requiere backups/monitoreo.
- No hay firma asimétrica ni anclaje externo (WORM).
- No hay endpoint de purga/legal hold (ADR-011 lo define).
