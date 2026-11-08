# T-013 — Preparar evidencia de salida R1

**Fase:** R1 · **Depende de:** [T-005](T-005-tenancy-uow.md) a [T-012](T-012-operations.md), [T-002](T-002-decisions.md).

## Trabajo
- Consolidar evidencia y estado de todos los criterios R1, historias, decisiones, riesgos y hallazgos aplicables.
- Ejecutar o referenciar pruebas de aislamiento, pooling, permisos, numeración, rollback, CI y recuperación.

## Aceptación y evidencia
- Matriz completa contra [criterios de R1](../backlog/releases.md), con enlaces a resultados y responsables; cualquier falla mantiene G1 cerrado.
- Solicitar decisión humana de release: aprobar este PR no equivale a abrir G1 ni autoriza despliegue.

## Flujo obligatorio
Crear una rama nueva `docs/t-013-r1-gate` desde `main`. Al terminar y validar, hacer commit Conventional Commits, push y crear un PR con evidencia; solicitar aprobación a **@acamposve**. No aprobar ni fusionar el propio PR.
