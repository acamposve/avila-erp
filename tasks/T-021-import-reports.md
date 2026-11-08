# T-021 — Importación y reportes operativos

**Fase:** R2 · **Depende de:** [T-014](T-014-catalog-stock.md), [T-016](T-016-purchasing.md), [T-017](T-017-sales.md), [T-020](T-020-treasury.md).

## Trabajo
- Implementar migración/importación con validación, reporte de errores, datos sintéticos y control de acceso por tenant.
- Refinar reportes operativos de ventas, compras, stock, kardex, cartera y caja con RF y AC aprobados antes de implementarlos; no inventar requisitos faltantes.

## Aceptación y evidencia
- Importación reproducible con errores identificables y sin datos cruzados; reportes conciliados contra datos de origen y autorizados por recurso.
- Trazabilidad: `INT-004`, `AC-MIG-01`, `AUD-P2-05`; [epics](../backlog/epics.md).

## Flujo obligatorio
Crear una rama nueva `feature/t-021-import-reports` desde `main`. Al terminar y validar, hacer commit Conventional Commits, push y crear un PR con evidencia; solicitar aprobación a **@acamposve**. No aprobar ni fusionar el propio PR.
