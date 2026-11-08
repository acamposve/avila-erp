# T-023 — Validación contable y decisiones R3

**Fase:** R3 · **Depende de:** [T-022](T-022-r2-gate.md) y apertura humana de G2.

## Trabajo
- Refinar requisitos y solicitar a especialistas decisiones y evidencia para `DEC-007`, `DEC-009`, `DEC-012` y políticas contables pendientes del ADR-009.
- Definir casos dorados, criterios de cutover, conciliación y tratamiento de diferencias cambiarias sin imponer reglas no aprobadas.

## Aceptación y evidencia
- Decisiones y fuentes tienen responsable, alcance bloqueado y estado registrado; casos contables cuentan con aprobación externa cuando sea necesaria.
- No se cierra una decisión por silencio; referencia: [ADR-009](../docs/decisions/ADR-009-accounting-and-cutover.md), [fuentes](../docs/compliance/regulatory-sources.md).

## Flujo obligatorio
Crear una rama nueva `docs/t-023-accounting-validation` desde `main`. Al terminar y validar, hacer commit Conventional Commits, push y crear un PR con evidencia; solicitar aprobación a **@acamposve**. No aprobar ni fusionar el propio PR.
