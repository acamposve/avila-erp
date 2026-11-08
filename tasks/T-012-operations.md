# T-012 — Operación, CI y recuperación

**Fase:** R1 · **Depende de:** [T-002](T-002-decisions.md), [T-005](T-005-tenancy-uow.md); autorización explícita para infraestructura y workflows.

## Trabajo
- Preparar observabilidad, health checks, métricas, alertas y runbooks sin secretos ni PII.
- Con autorización, preparar CI con build, pruebas, seguridad y migraciones en limpio; ensayar respaldo y restauración en entorno autorizado.
- Documentar RPO/RTO, roles, límites y procedimiento de migración/backfill bajo RLS.

## Aceptación y evidencia
- Evidencia de pipeline, migración limpia, trazas y simulacro reproducible de restauración; pendientes o riesgos sin autorización quedan documentados.
- Trazabilidad: `EP-INT-01`, `AC-OBS-01`, [pruebas](../docs/architecture/testing-strategy.md), [releases](../backlog/releases.md).

## Flujo obligatorio
Crear una rama nueva `feature/t-012-operations` desde `main`. Al terminar y validar, hacer commit Conventional Commits, push y crear un PR con evidencia; solicitar aprobación a **@acamposve**. No aprobar ni fusionar el propio PR.
