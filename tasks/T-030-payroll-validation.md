# T-030 — Validación laboral y decisiones de nómina

**Fase:** R5 · **Depende de:** [T-004](T-004-r5-dependency.md), [T-026](T-026-r3-gate.md), apertura humana de G3 y coherencia documental resuelta.

## Trabajo
- Refinar requisitos y preparar decisiones `DEC-011` y `DEC-014` con especialista laboral y Arquitectura.
- Identificar fuentes oficiales, vigencias y casos dorados para nómina, prestaciones, utilidades y liquidación.

## Aceptación y evidencia
- Cada regla tiene fuente, revisión y estado; las no aprobadas mantienen `PENDIENTE DE VALIDACIÓN LEGAL / CONTABLE`.
- No se inventan fórmulas ni se considera aprobada una decisión por el PR; [fuentes](../docs/compliance/regulatory-sources.md), [laboral](../docs/compliance/labor-regulations.md).

## Flujo obligatorio
Crear una rama nueva `docs/t-030-payroll-validation` desde `main`. Al terminar y validar, hacer commit Conventional Commits, push y crear un PR con evidencia; solicitar aprobación a **@acamposve**. No aprobar ni fusionar el propio PR.
