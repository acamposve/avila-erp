# T-014 — Catálogo, almacenes y kardex

**Fase:** R2 · **Depende de:** [T-013](T-013-r1-gate.md) y apertura humana de G1.

## Trabajo
- Implementar artículos, servicios, variantes, SKU y unidades/conversiones; almacenes, existencias y kardex.
- Registrar movimientos trazables y costo promedio con aritmética decimal conforme al dominio.

## Aceptación y evidencia
- Pruebas de conversión de unidades, saldo igual a movimientos, aislamiento y casos dorados de costos.
- Trazabilidad: `US-INV-001`…`004`, `AC-INV-01`, `AC-INV-02`, `AC-INV-05`; [inventario](../docs/domain/inventory.md).

## Flujo obligatorio
Crear una rama nueva `feature/t-014-catalog-stock` desde `main`. Al terminar y validar, hacer commit Conventional Commits, push y crear un PR con evidencia; solicitar aprobación a **@acamposve**. No aprobar ni fusionar el propio PR.
