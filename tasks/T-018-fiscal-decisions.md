# T-018 — Preparar validaciones fiscales y comerciales R2

**Fase:** R2 · **Depende de:** [T-002](T-002-decisions.md); iniciar preparación antes de necesitar emisión definitiva.

## Trabajo
- Preparar opciones y solicitar resolución para `DEC-002`, `DEC-004`, `DEC-005`, `DEC-006`, `DEC-007`, `DEC-019` y `DEC-020` según su impacto.
- Coordinar consulta de fuentes oficiales, validación por especialista, proveedor fiscal y casos dorados; registrar fuente, vigencia y estado.

## Aceptación y evidencia
- Cada decisión tiene dueño, evidencia, alcance bloqueado y estado en [decisiones](../docs/decisions/README.md).
- No se autoaprueban políticas ni se inventan tasas, formatos o fórmulas; lo pendiente mantiene `PENDIENTE DE VALIDACIÓN LEGAL / CONTABLE`.

## Flujo obligatorio
Crear una rama nueva `docs/t-018-fiscal-decisions` desde `main`. Al terminar y validar, hacer commit Conventional Commits, push y crear un PR con evidencia; solicitar aprobación a **@acamposve**. No aprobar ni fusionar el propio PR.
