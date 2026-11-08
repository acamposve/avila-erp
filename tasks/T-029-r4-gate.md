# T-029 — Preparar evidencia de salida R4

**Fase:** R4 · **Depende de:** [T-028](T-028-retail-delivery.md), validación fiscal y de hardware.

## Trabajo
- Consolidar evidencia de caída de red, sincronización, cierre Z, facturación, hardware y procedimiento de contingencia.

## Aceptación y evidencia
- Matriz completa de [criterios R4](../backlog/releases.md) y aprobaciones externas; cualquier pendiente mantiene G4 cerrado.
- Solicitar decisión humana: el PR aprobado no sustituye la autorización de release.

## Flujo obligatorio
Crear una rama nueva `docs/t-029-r4-gate` desde `main`. Al terminar y validar, hacer commit Conventional Commits, push y crear un PR con evidencia; solicitar aprobación a **@acamposve**. No aprobar ni fusionar el propio PR.
