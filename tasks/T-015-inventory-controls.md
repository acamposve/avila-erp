# T-015 — Transferencias, reservas y control de stock

**Fase:** R2 · **Depende de:** [T-014](T-014-catalog-stock.md), [T-010](T-010-configuration.md).

## Trabajo
- Implementar transferencias, ajustes aprobados, conteos, lotes, vencimientos, reservas y reposición.
- Asegurar bloqueo por agregado, movimientos únicos y cálculo de costos coherente bajo concurrencia.

## Aceptación y evidencia
- PostgreSQL real: stock 10, dos ventas/reservas concurrentes de 8, solo una confirma y saldo queda en 2; kardex cuadra.
- Probar trazabilidad de lote, transferencia y autorización de ajustes. `US-INV-005`…`007`, `AC-INV-03`, `AC-INV-04`, `AC-INV-06`; [ADR-008](../docs/decisions/ADR-008-concurrency-by-aggregate.md).

## Flujo obligatorio
Crear una rama nueva `feature/t-015-inventory-controls` desde `main`. Al terminar y validar, hacer commit Conventional Commits, push y crear un PR con evidencia; solicitar aprobación a **@acamposve**. No aprobar ni fusionar el propio PR.
