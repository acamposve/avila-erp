# T-025 — Cutover y tesorería financiera

**Fase:** R3 · **Depende de:** [T-023](T-023-accounting-validation.md), [T-024](T-024-accounting-posting.md).

## Trabajo
- Implementar apertura híbrida y conciliación subledger-GL para activar R3 desde R2 conforme a ADR-009.
- Completar bancos, importación de estados, conciliación, multimoneda y libros/retenciones con políticas validadas.

## Aceptación y evidencia
- Dataset sintético R2 produce apertura única, balanza cuadrada y CxC/CxP conciliadas; probar repetición y rollback.
- Conciliación por archivo y casos FX trazables; `AC-CON-04`, `AC-CON-05`, `AC-TES-04`, `AC-TES-05`, `AC-FIS-03`.

## Flujo obligatorio
Crear una rama nueva `feature/t-025-finance-cutover` desde `main`. Al terminar y validar, hacer commit Conventional Commits, push y crear un PR con evidencia; solicitar aprobación a **@acamposve**. No aprobar ni fusionar el propio PR.
