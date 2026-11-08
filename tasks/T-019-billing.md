# T-019 — Facturación y reglas fiscales versionadas

**Fase:** R2 · **Depende de:** [T-017](T-017-sales.md), [T-009](T-009-numbering-idempotency.md), [T-010](T-010-configuration.md), [T-018](T-018-fiscal-decisions.md); decisiones y validación aplicables.

## Trabajo
- Implementar emisión, snapshot y numeración; reglas fiscales versionadas, exenciones e IGTF solo según evidencia validada.
- Implementar representación, notas de crédito/débito, reversión inmutable y adaptador para el medio autorizado.
- Garantizar idempotencia y límites de contratos; integrar outbox solo para acciones post-commit según ADR-005/009.

## Aceptación y evidencia
- Pruebas de cálculo con casos dorados aprobados, vigencia, inmutabilidad, reintento, fallo del proveedor y numeración concurrente.
- Trazabilidad: `US-FAC-001`…`004`, `AC-FAC-01`…`05`, `AC-FIS-01`, `AC-FIS-02`, `AC-FIS-04`; [facturación](../docs/domain/billing.md).

## Flujo obligatorio
Crear una rama nueva `feature/t-019-billing` desde `main`. Al terminar y validar, hacer commit Conventional Commits, push y crear un PR con evidencia; solicitar aprobación a **@acamposve**. No aprobar ni fusionar el propio PR.
