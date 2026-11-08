# T-003 — Refinar trabajo de hardening

**Fase:** 0 · **Depende de:** [T-001](T-001-baseline.md).

## Trabajo
- Mapear los P1/P2 relevantes a historias, ADR, pruebas, responsables y evidencia; separar spikes de código de producción.
- Priorizar UoW, contexto tenant/RLS en lecturas y pooling, `platform`, numeración, concurrencia y migraciones bajo RLS.

## Aceptación y evidencia
- Cada historia refinada tiene dependencia y criterios de cierre trazables a las pruebas del [plan de hardening](../docs/architecture/architecture-hardening-plan.md).
- No se declara cerrado un hallazgo sin implementación y evidencia requeridas.

## Flujo obligatorio
Crear una rama nueva `docs/t-003-hardening-backlog` desde `main`. Al terminar y validar, hacer commit Conventional Commits, push y crear un PR con evidencia; solicitar aprobación a **@acamposve**. No aprobar ni fusionar el propio PR.
