# T-035 — Producción y CRM

**Fase:** R6 · **Depende de:** [T-033](T-033-enterprise-decisions.md), módulos operativos y contables de R2/R3.

## Trabajo
- Implementar BOM, orden de producción, consumos, desperdicio y costos mediante contratos de módulos.
- Implementar prospectos, oportunidades y conversión a cotización/venta, conforme a requisitos refinados.

## Aceptación y evidencia
- Una orden de producción se costea y concilia con inventario y contabilidad; una oportunidad se convierte en venta con trazabilidad y aislamiento.
- Trazabilidad: `EP-PRO-01`, `EP-CRM-01`, `AC-PRO-01`…`02`, `AC-CRM-01`…`02`; [producción](../docs/domain/production.md).

## Flujo obligatorio
Crear una rama nueva `feature/t-035-production-crm` desde `main`. Al terminar y validar, hacer commit Conventional Commits, push y crear un PR con evidencia; solicitar aprobación a **@acamposve**. No aprobar ni fusionar el propio PR.
