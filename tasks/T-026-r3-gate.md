# T-026 — Preparar evidencia de salida R3

**Fase:** R3 · **Depende de:** [T-024](T-024-accounting-posting.md), [T-025](T-025-finance-cutover.md), validación externa.

## Trabajo
- Reunir evidencia de posting automático, cierre, reversión, balanza, conciliación bancaria y subledger-GL.
- Coordinar cierre mensual de piloto con contador y revisión de libros y retenciones por especialista.

## Aceptación y evidencia
- Matriz completa de [criterios R3](../backlog/releases.md), informes de especialistas y resultados reproducibles; cualquier pendiente mantiene G3 cerrado.
- Solicitar decisión humana: el PR aprobado no sustituye la autorización de release.

## Flujo obligatorio
Crear una rama nueva `docs/t-026-r3-gate` desde `main`. Al terminar y validar, hacer commit Conventional Commits, push y crear un PR con evidencia; solicitar aprobación a **@acamposve**. No aprobar ni fusionar el propio PR.
