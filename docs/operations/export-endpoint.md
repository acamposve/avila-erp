# Tenant export endpoint

**Estado:** R1-REM-07 · **Fecha:** 2026-11-15 · **Dueño:** @acamposve

## 1. Contrato

```http
GET /api/v1/tenant/export
Authorization: Bearer <session>
Accept: application/x-ndjson
```

No recibe `tenantId` en query, ruta ni body. Si se envía `?tenantId=...`, responde `400 TENANT_OVERRIDE_NOT_ALLOWED`.

| Condición | Respuesta |
|---|---|
| Sin sesión | `401` |
| Rol `member` u otro no owner/admin | `403` |
| Owner/admin sin step-up reciente (5 min) | `403 STEP_UP_REQUIRED` |
| `tenantId` intentado por el llamador | `400` |
| Owner/admin | `200 application/x-ndjson` |
| Tenant suspendido | `200` (lectura/exportación permitida) |
| Fallo antes de iniciar stream | `500` + auditoría `failed` |
| Fallo después de iniciar stream | conexión abortada + auditoría `failed` |

El tenant se toma de `SessionPrincipal` y se contrasta con la UoW; no hay override.

## 2. NDJSON

Líneas:

1. `{"type":"export","format":"avila.tenant-export/v1","tenant_id":"...","generated_at":"..."}`
2. `{"type":"section","module":"administration","order":10,"schema_version":1}`
3. `{"type":"record","module":"administration","entity":"company","id":"...","data":{...}}`
4. `{"type":"section_end","module":"administration","record_count":N,"sha256":"..."}`
5. ...
6. `{"type":"manifest","format":"...","tenant_id":"...","sections":[...],"sha256":"..."}`

- Se escribe una línea a la vez; no se materializa todo el tenant.
- Los contribuyentes R1 implementan `ITenantExportStreamContributor`.
- Cada registro se hashea canónicamente; el manifest incluye hashes por sección y hash global.
- `generated_at` no forma parte del hash.
- `Cache-Control: no-store`.

## 3. Auditoría

Eventos en `platform.audit_events` con `entity_type=tenant_export`:

| Acción | Cuándo |
|---|---|
| `tenant.export.requested` | Antes de abrir el stream |
| `tenant.export.completed` | Después del manifest |
| `tenant.export.failed` | Excepción/cancelación |

Se escriben con `ExportAuditWriter`, en una UoW/conexión/transacción independiente. Un rollback de la lectura no borra el evento.

## 4. Suspensión

La exportación **no** usa `TenantAccessGuard`; es una operación de lectura/portabilidad. Un tenant suspendido puede exportar. No se permite escribir.

## 5. Límites

- No hay cuota de tamaño ni cursor; el cliente debe consumir el stream.
- No hay zip/firma detached; el hash va en el manifest.
- No hay endpoint de descarga asíncrona.
- La entrega sigue dependiendo de TLS y del control de acceso de la API.
- No se exportan secretos ni tokens (ver R1-REM-06).
