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

## Re-evaluación final

La matriz histórica de T-013 (3 PASS / 8 PARTIAL / 1 FAIL, G1 NO-GO) queda conservada como evidencia de 2026-11-14. La re-evaluación **R1-REM-14** sobre `main` `2591a4e` da **12/12 PASS** a nivel código/ensayo y mantiene **G1 CERRADO** hasta la firma explícita de @acamposve. Ver [matriz final](../tasks/evidence/R1-REM-14/r1-gate-matrix.md) y [solicitud de firma](../tasks/evidence/R1-REM-14/release-signoff-request.md).
