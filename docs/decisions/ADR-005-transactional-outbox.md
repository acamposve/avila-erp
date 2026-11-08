# ADR-005 — Transactional outbox e inbox

- **Estado:** Aceptado
- **Fecha:** 2026-11-08
- **Decisores:** Arquitectura
- **Reemplaza a:** —

## 1. Contexto

Muchas operaciones de ÁVILA ERP deben producir efectos secundarios confiables: notificar, proyectar, sincronizar, enviar un correo, generar un PDF, integrar un proveedor fiscal o actualizar un reporte. Si el efecto se ejecuta dentro de la transacción, una falla externa revierte el negocio o lo bloquea; si se ejecuta después sin registro, un fallo del proceso pierde el evento.

Se necesita **entrega al menos una vez** con idempotencia del consumidor, sin introducir un broker externo en la etapa inicial.

## 2. Decisión

Se adopta el patrón **transactional outbox** con **inbox** para consumidores:

1. Los eventos de integración se escriben en `shared.outbox_messages` **en la misma transacción** que el cambio de negocio.
2. Un despachador en `Avila.Worker` lee lotes con `FOR UPDATE SKIP LOCKED`, publica en el manejador correspondiente y marca el mensaje como procesado.
3. Los consumidores registran `event_id` en `shared.inbox_messages` con restricción única `(consumer, event_id)` para garantizar idempotencia.
4. La entrega es **at-least-once**; los manejadores DEBEN ser idempotentes.
5. Reintentos con backoff exponencial y límite; al agotarse pasan a **dead letter** (`shared.outbox_dead_letters`) con alerta.
6. El orden se garantiza **por clave de agregado** (`aggregate_id`), no globalmente.
7. No se usa un broker externo (RabbitMQ, Kafka, SQS) en la etapa inicial. Se reevalúa con ADR si la escala o la integración lo exigen.
8. Los **eventos de dominio** que participan de invariantes o contabilización se manejan en proceso, dentro de la misma transacción; los **eventos de integración** van al outbox después del commit lógico.

## 3. Esquema mínimo

```sql
CREATE TABLE shared.outbox_messages (
  id uuid PRIMARY KEY,
  tenant_id uuid NOT NULL,
  event_type text NOT NULL,
  event_version int NOT NULL,
  aggregate_type text NOT NULL,
  aggregate_id uuid NOT NULL,
  payload jsonb NOT NULL,
  occurred_at timestamptz NOT NULL,
  correlation_id uuid NOT NULL,
  causation_id uuid,
  status text NOT NULL DEFAULT 'pending',
  attempts int NOT NULL DEFAULT 0,
  next_attempt_at timestamptz NOT NULL DEFAULT now(),
  processed_at timestamptz,
  last_error text
);

CREATE INDEX ix_outbox_pending
  ON shared.outbox_messages (next_attempt_at)
  WHERE status = 'pending';
```

El `payload` sigue un sobre versionado: `event_id`, `event_type`, `version`, `tenant_id`, `occurred_at`, `correlation_id`, `causation_id`, `data`. Nunca incluye secretos ni PII innecesaria.

## 4. Reglas

- El outbox se escribe en la misma transacción; si la transacción falla, no hay evento.
- Un evento publicado no se modifica; se compensa con otro evento.
- Los manejadores no escriben en tablas de otros módulos: llaman a contratos o a su propio almacenamiento.
- El despachador es multi-tenant y aplica cuotas por tenant para evitar que uno monopolice el lote.
- Los mensajes procesados se archivan por política de retención (`DEC-008`).
- Las integraciones externas usan claves de idempotencia propias, además del `event_id`.

## 5. Consecuencias

### Positivas
- Consistencia entre cambio de negocio y evento sin 2PC.
- Recuperación ante caídas sin pérdida de eventos.
- Trazabilidad completa de qué se publicó y cuándo.
- Operación simple: una tabla y un worker.

### Negativas
- Latencia de unos segundos entre el hecho y el efecto.
- Crecimiento de la tabla; requiere archivado y monitoreo.
- Posibles duplicados: los consumidores deben ser idempotentes.
- El orden global no está garantizado.

## 6. Alternativas consideradas

| Alternativa | Motivo de descarte |
|---|---|
| Publicar dentro de la transacción | Falla externa revierte el negocio o deja inconsistencia |
| Broker externo desde el inicio | Complejidad operativa y de infraestructura innecesaria |
| Cambio de datos + job programado sin outbox | Pérdida de eventos ante fallos |
| CDC (Change Data Capture) | Complejidad y dependencia de infraestructura; reevaluar a escala |

## 7. Verificación

- Prueba: transacción fallida no deja evento.
- Prueba: caída del worker y reinicio no pierde ni duplica efectos.
- Prueba: consumidor idempotente ante evento repetido.
- Prueba: dead letter y alerta ante fallo persistente.
- Monitoreo: antigüedad del evento pendiente más antiguo (alerta > 5 min).

## 8. Referencias

- [integration-strategy.md](../architecture/integration-strategy.md)
- [modular-monolith.md](../architecture/modular-monolith.md)
- [ADR-001](ADR-001-modular-monolith.md)
