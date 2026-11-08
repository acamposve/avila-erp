# T-027 — Decisiones de POS offline y hardware

**Fase:** R4 · **Depende de:** [T-022](T-022-r2-gate.md) y apertura humana de G2.

## Trabajo
- Refinar POS y preparar resolución de `DEC-010`, `DEC-013` y `DEC-017`: legalidad de emisión offline, matriz de hardware y conflictos de sincronización.
- Solicitar validación fiscal y acordar pruebas de contingencia con los responsables.

## Aceptación y evidencia
- Opciones, impacto, responsables y estado se registran en [decisiones](../docs/decisions/README.md); los puntos legales no validados permanecen bloqueados.
- No prometer emisión fiscal offline sin aprobación especializada y fuentes verificadas.

## Flujo obligatorio
Crear una rama nueva `docs/t-027-retail-decisions` desde `main`. Al terminar y validar, hacer commit Conventional Commits, push y crear un PR con evidencia; solicitar aprobación a **@acamposve**. No aprobar ni fusionar el propio PR.
