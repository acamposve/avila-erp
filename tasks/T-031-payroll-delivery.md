# T-031 — RRHH y nómina trazable

**Fase:** R5 · **Depende de:** [T-030](T-030-payroll-validation.md); decisiones y reglas aplicables validadas.

## Trabajo
- Implementar expediente y contratos, conceptos y parámetros versionados, cálculo y recálculo determinista, recibos y liquidación.
- Integrar provisión y pago con contabilidad y tesorería; proteger datos sensibles y auditar accesos.

## Aceptación y evidencia
- Casos dorados aprobados por especialista, recálculo idéntico, trazabilidad de conceptos, integración conciliada y autorización de datos sensibles.
- Trazabilidad: `EP-RRH-01`, `EP-NOM-01`…`03`, `AC-RRH-01`…`02`, `AC-NOM-01`…`04`; [nómina](../docs/domain/payroll.md).

## Flujo obligatorio
Crear una rama nueva `feature/t-031-payroll-delivery` desde `main`. Al terminar y validar, hacer commit Conventional Commits, push y crear un PR con evidencia; solicitar aprobación a **@acamposve**. No aprobar ni fusionar el propio PR.
