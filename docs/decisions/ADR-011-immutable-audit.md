# ADR-011 — Auditoría inmutable, consulta y retención

- **Estado:** **Aceptado** — 2026-11-14.
- **Decisores:** @acamposve (Arquitectura, Seguridad, Legal).
- **Complementa:** ADR-003, ADR-007, ADR-008.
- **Alcance:** auditoría de operaciones sensibles en R1/R2.

## 1. Contexto

`platform.audit_events` ya es append-only por trigger y grants, pero faltan reglas explícitas de integridad, consulta, exportación y retención. La documentación anterior usaba el nombre no normativo `platform.audit_log`; la tabla física canónica es **`platform.audit_events`**.

## 2. Decisión

### 2.1 Tabla canónica y escritura

- La tabla canónica es `platform.audit_events`, con RLS forzada por `app.tenant_id`.
- Solo se permite `INSERT` y `SELECT` al rol de aplicación; `UPDATE` y `DELETE` están prohibidos por grants y por trigger.
- Cada evento registra: `tenant_id`, `actor_user_id`, `action`, `entity_type`, `entity_id`, `before_json`, `after_json`, `correlation_id`, `occurred_at`.
- `before_json`/`after_json` se minimizan y enmascaran; nunca contienen contraseñas, tokens, claves, secretos MFA, datos bancarios completos ni PII innecesaria.
- Las correcciones se registran como **nuevo evento**; nunca se edita el anterior.

### 2.2 Cadena de integridad

Cada evento incorpora:

| Campo | Regla |
|---|---|
| `previous_hash` | Hash del evento anterior del mismo tenant; `NULL` solo en el primero |
| `event_hash` | SHA-256 de la representación canónica del evento + `previous_hash` |
| `hash_version` | Versión del algoritmo; inicia en `1` |

- La inserción se serializa por tenant para preservar el orden de la cadena.
- La representación canónica excluye `event_hash` y usa JSON con claves ordenadas.
- Un verificador recomputa la cadena y reporta el primer evento inválido.
- La cadena detecta alteraciones; no sustituye backups ni control de acceso.

### 2.3 Consulta y exportación

- La consulta requiere permiso `administration.audit.read` y alcance del tenant; nunca acepta un tenant del cliente.
- Filtros mínimos: rango de fechas, actor, acción, entidad y correlación; paginación por cursor.
- La exportación usa el mismo contrato de contribuyentes que la exportación de tenant o un exportador dedicado, con manifiesto, conteos y hash.
- **Consultar o exportar auditoría también se audita** (`audit.read`, `audit.export`), con actor, correlación y resultado.
- Un tenant solo puede consultar/exportar su propia auditoría.

### 2.4 Retención y legal hold

- Retención por defecto para auditoría y registros fiscales: **7 años**, configurable por tenant/jurisdicción.
- La retención es una política de datos, no un permiso para borrar: el borrado requiere autorización, legal hold y procedimiento documentado.
- Un `legal_hold` bloquea cualquier purga, incluido el fin de retención.
- La purga, si procede, se registra como evento de ciclo de vida y no altera eventos dentro de retención.
- `DEC-008` queda con baseline aprobado; validaciones legales específicas de jurisdicción pueden endurecer, no acortar por defecto.

### 2.5 Nombres y compatibilidad

Las referencias a `platform.audit_log` en ADR-007 y en documentación previa son **no normativas**. Este ADR fija `platform.audit_events` como nombre canónico y no cambia la semántica de auditoría de ADR-007.

## 3. Consecuencias

- La auditoría es verificable, consultable y exportable sin exponer secretos.
- La cadena introduce una escritura serializada por tenant; el volumen debe medirse.
- La retención de 7 años exige backup, legal hold y proceso de purga aprobado.

## 4. Referencias

- [Seguridad](../architecture/security.md)
- [Multi-tenancy](../architecture/multi-tenancy.md)
- [ADR-003](ADR-003-multi-tenancy.md)
- [ADR-007](ADR-007-transaction-and-tenant-context.md)
- [DEC-008](README.md)
