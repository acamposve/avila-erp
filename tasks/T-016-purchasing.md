# T-016 — Compras y recepción

**Fase:** R2 · **Depende de:** [T-014](T-014-catalog-stock.md), [T-015](T-015-inventory-controls.md), [T-010](T-010-configuration.md).

## Trabajo
- Implementar proveedores, órdenes y aprobación, recepción parcial/total, factura con validación de tres vías y devolución.
- Registrar moneda, tasa, fuente, fecha, flete y costos mediante contratos de módulo.

## Aceptación y evidencia
- Probar recepción parcial, diferencias fuera de tolerancia, aprobación, devolución y costo trazable, además de aislamiento e idempotencia.
- Trazabilidad: `US-COM-001`…`004`, `AC-COM-01`…`03`; [compras](../docs/domain/purchasing.md).

## Flujo obligatorio
Crear una rama nueva `feature/t-016-purchasing` desde `main`. Al terminar y validar, hacer commit Conventional Commits, push y crear un PR con evidencia; solicitar aprobación a **@acamposve**. No aprobar ni fusionar el propio PR.
