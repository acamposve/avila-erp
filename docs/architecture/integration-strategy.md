# Estrategia de integración

**Versión:** 1.0 · **Fecha:** 2026-11-08 · **Estado:** línea base

## 1. Principios

1. **Contratos explícitos y versionados.** Nada se integra por acceso directo a tablas.
2. **Idempotencia por defecto.** Toda operación que crea o modifica estado acepta una clave.
3. **Fallo aislado.** Un proveedor externo caído no bloquea la operación interna; se reintenta o se degrada.
4. **Trazabilidad.** Cada llamada y evento tiene `correlation_id` y queda registrada.
5. **Sin secretos en el cliente.** Las integraciones se autentican en el servidor.
6. **Compatibilidad hacia atrás.** Cambios incompatibles crean versión nueva.

## 2. API pública (R6)

| Aspecto | Decisión |
|---|---|
| Estilo | REST/JSON sobre HTTPS |
| Base | `/api/v1` |
| Versionado | Mayor en la ruta (`v1`); cambios compatibles dentro de la versión |
| Formato | JSON con decimales como string; fechas ISO 8601 con zona |
| Documentación | OpenAPI 3.1 + portal de desarrolladores |
| Autenticación | OAuth2 client credentials (preferido) o API key con scopes |
| Autorización | Scopes por recurso y tenant; nunca acceso global |
| Paginación | Cursor opaco (`cursor`, `limit`), no offset profundo |
| Filtros | Parámetros documentados y acotados |
| Errores | RFC 9457 Problem Details con `code`, `detail`, `correlation_id` |
| Idempotencia | `Idempotency-Key` en POST que crean documentos o pagos |
| Concurrencia | `ETag` + `If-Match` en actualizaciones |
| Límites | Cuota por credencial y por tenant; `429` con `Retry-After` |
| Sandbox | Tenant de pruebas con datos sintéticos y proveedores simulados |

### Recursos iniciales previstos
`/customers`, `/suppliers`, `/items`, `/warehouses`, `/stock`, `/quotes`, `/orders`, `/invoices`, `/payments`, `/receivables`, `/payables`, `/journal-entries`, `/webhooks`.

Los recursos se publican solo cuando su módulo está estable y con criterios de aceptación cumplidos.

## 3. Eventos de integración

Se publican por outbox ([ADR-005](../decisions/ADR-005-transactional-outbox.md)). Catálogo inicial:

| Evento | Origen | Consumidores previstos |
|---|---|---|
| `sales.order.confirmed.v1` | Ventas | Inventario, integraciones, notificaciones |
| `sales.order.cancelled.v1` | Ventas | Inventario, notificaciones |
| `billing.invoice.issued.v1` | Facturación | Contabilidad, tesorería, integraciones, PDF |
| `billing.invoice.voided.v1` | Facturación | Contabilidad, tesorería, integraciones |
| `billing.credit-note.issued.v1` | Facturación | Contabilidad, tesorería, comisiones |
| `treasury.receipt.registered.v1` | Tesorería | Contabilidad, notificaciones |
| `treasury.payment.registered.v1` | Tesorería | Contabilidad, notificaciones |
| `treasury.payment.applied.v1` | Tesorería | Contabilidad, cartera |
| `inventory.stock.moved.v1` | Inventario | Reportes, integraciones |
| `inventory.stock.below-reorder.v1` | Inventario | Notificaciones, compras |
| `purchasing.purchase-order.approved.v1` | Compras | Notificaciones, proveedor |
| `purchasing.goods-receipt.posted.v1` | Compras | Inventario, contabilidad |
| `accounting.entry.posted.v1` | Contabilidad | Reportes, integraciones |
| `payroll.run.approved.v1` | Nómina | Tesorería, contabilidad |
| `tenant.plan.changed.v1` | Plataforma | Administración, notificaciones |

Reglas:

- El nombre incluye módulo, agregado, acción y versión.
- El payload es un snapshot suficiente para el consumidor; no obliga a consultar el origen.
- Los eventos no se editan; se versionan.
- Los consumidores toleran campos nuevos y fallan de forma controlada ante faltantes obligatorios.

## 4. Webhooks salientes

- Firma HMAC-SHA256 sobre `timestamp + body` con secreto por suscripción.
- Reintentos con backoff exponencial (por ejemplo 1 min, 5 min, 30 min, 2 h, 6 h; máximo configurable).
- Entrega al menos una vez; el receptor debe ser idempotente con `event_id`.
- Registro de entregas, códigos de respuesta y errores.
- Desactivación automática tras fallos persistentes con notificación.
- Prohibido incluir PII innecesaria o secretos en el payload.

## 5. Adaptadores externos

| Integración | Dirección | Patrón | Estado |
|---|---|---|---|
| Proveedor de emisión fiscal | Salida | Adaptador + reintentos + contingencia | `DEC-002` pendiente |
| Pasarelas / medios de pago | Salida y webhook | Adaptador + conciliación | `DEC-009` pendiente |
| Bancos (estados de cuenta) | Entrada | Importación de archivo + reglas de match | R3 |
| Correo transaccional | Salida | Cola + plantillas + rebote | R2 |
| Almacenamiento de objetos | Salida | SDK con URLs firmadas | R2 |
| E-commerce | Entrada/salida | API + webhooks + mapeo de catálogo | R6 |
| BI | Salida | Exportación o réplica de lectura | R6 |
| Facturación SaaS (suscripciones) | Salida | Pasarela + conciliación | R2 |

Cada adaptador:

1. Vive en `Avila.Modules.Integration` o en el módulo dueño.
2. Traduce el modelo externo al interno (anti-corruption layer).
3. Tiene timeout, reintentos, circuit breaker y métricas.
4. No expone el modelo externo al dominio.
5. Se prueba con simuladores y, si es posible, con sandbox del proveedor.

## 6. Importación y exportación

### Importación
- Plantillas CSV/XLSX versionadas y documentadas.
- Validación previa con reporte de errores por fila y columna.
- Carga por lotes idempotentes (`import_job_id`), con progreso y reanudación.
- No se importa a ciegas: el usuario confirma el resumen y los errores.
- Datos maestros primero; documentos y saldos después.

### Exportación
- Formatos abiertos (CSV, XLSX, JSON) y PDF para documentos.
- Incluye metadatos: tenant, filtros, fecha, usuario y versión.
- Autorizada y auditada.
- Suficiente para reconstruir la información en otro sistema (requisito `ADM-016`).

## 7. Comunicación interna

- Los módulos se comunican por contratos en proceso; nunca por HTTP interno ni por tablas.
- Las consultas entre módulos usan contratos de consulta o proyecciones propias.
- Los eventos internos críticos se manejan en la transacción; los externos por outbox.
- Ver [modular-monolith.md](modular-monolith.md).

## 8. Manejo de fallos

| Fallo | Respuesta |
|---|---|
| Timeout de proveedor | Reintento con backoff; si persiste, dead letter y alerta |
| Error 4xx del proveedor | No reintentar; registrar y notificar; requiere corrección |
| Error 5xx del proveedor | Reintentar; circuit breaker tras umbral |
| Webhook fallido | Reintentos; desactivación con notificación |
| Evento duplicado | Inbox lo descarta; operación idempotente |
| Evento fuera de orden | Se tolera por versión del agregado o se descarta con alerta |
| Cuota excedida | 429 con `Retry-After`; el cliente reintenta |

## 9. Contratos y pruebas

- Contratos documentados en OpenAPI y en el catálogo de eventos.
- Pruebas de contrato para consumidores internos y partners.
- Versionado semántico de eventos; compatibilidad verificada en CI.
- Sandbox y credenciales de prueba para partners.
- Certificación de un partner antes de producción (`AC-INT-01`).

## 10. Seguridad de integraciones

- TLS obligatorio; sin HTTP plano.
- Credenciales con scopes mínimos y rotación.
- Firma de webhooks y validación de timestamp.
- Límites de tasa y tamaño de payload.
- Sin secretos ni PII en logs.
- Revisión de seguridad de cada proveedor externo.
- Ver [security.md](security.md).

## 11. Pendientes

- `DEC-002`: medio de emisión fiscal.
- `DEC-009`: integración bancaria.
- `DEC-015`: BI y réplica de lectura.
- Formatos oficiales de reportes fiscales: **PENDIENTE DE VALIDACIÓN LEGAL / CONTABLE**.
- Catálogo definitivo de API pública: se congela al inicio de R6.

## 12. Documentos relacionados

- [Arquitectura](architecture-overview.md)
- [ADR-005](../decisions/ADR-005-transactional-outbox.md)
- [Seguridad](security.md)
- [Requisitos INT](../requirements/functional-requirements.md)
- [Facturación](../compliance/invoicing.md)
