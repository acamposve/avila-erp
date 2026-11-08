# T-024 — Contabilidad, posting y cierre

**Fase:** R3 · **Depende de:** [T-023](T-023-accounting-validation.md); políticas contables y requisitos pertinentes aprobados.

## Trabajo
- Implementar plan de cuentas, asientos y reversión, reportes, períodos y cierre con control concurrente.
- Integrar posting síncrono transaccional cuando CON está activo, unicidad de posting, partida doble y separación del outbox.

## Aceptación y evidencia
- Un asiento inválido revierte la operación crítica; uno válido produce exactamente un asiento; BD rechaza partida desbalanceada.
- Probar bloqueo de cierre, reversión y conciliación; `EP-CON-01`…`03`, `AC-CON-01`…`05`, [ADR-009](../docs/decisions/ADR-009-accounting-and-cutover.md).

## Flujo obligatorio
Crear una rama nueva `feature/t-024-accounting-posting` desde `main`. Al terminar y validar, hacer commit Conventional Commits, push y crear un PR con evidencia; solicitar aprobación a **@acamposve**. No aprobar ni fusionar el propio PR.
