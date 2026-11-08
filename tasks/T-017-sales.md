# T-017 — Ventas, precios, reservas y crédito

**Fase:** R2 · **Depende de:** [T-015](T-015-inventory-controls.md), [T-009](T-009-numbering-idempotency.md), [T-010](T-010-configuration.md).

## Trabajo
- Implementar cotización versionada, conversión a pedido, entrega, listas de precios y descuentos aprobados.
- Confirmar pedidos con reserva y crédito concurrente; planificar comisiones/devoluciones con `DEC-019` resuelta antes de aplicar política definitiva.

## Aceptación y evidencia
- Cotización conserva cliente y líneas; descuentos sin aprobación se bloquean; pedidos concurrentes no exceden stock ni crédito.
- Trazabilidad: `US-VEN-001`…`004`, `AC-VEN-01`…`04`; [ventas](../docs/domain/sales.md), [ADR-008](../docs/decisions/ADR-008-concurrency-by-aggregate.md).

## Flujo obligatorio
Crear una rama nueva `feature/t-017-sales` desde `main`. Al terminar y validar, hacer commit Conventional Commits, push y crear un PR con evidencia; solicitar aprobación a **@acamposve**. No aprobar ni fusionar el propio PR.
