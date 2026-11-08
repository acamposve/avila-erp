# T-022 — Preparar piloto y evidencia de salida R2

**Fase:** R2 · **Depende de:** [T-014](T-014-catalog-stock.md) a [T-021](T-021-import-reports.md), validación fiscal y decisiones bloqueantes.

## Trabajo
- Preparar onboarding y manual; coordinar piloto autorizado, migración y validación externa fiscal.
- Consolidar resultados de facturas, inventario, notas, cartera, arqueo, aislamiento, concurrencia y decisiones comerciales.

## Aceptación y evidencia
- Matriz de todos los [criterios R2](../backlog/releases.md), incluido un mes de facturas reales sin correcciones manuales, evidencias de especialista y precios definidos.
- G2 queda cerrado si falta cualquier criterio; el PR no autoriza despliegue ni sustituye la decisión humana de release.

## Flujo obligatorio
Crear una rama nueva `docs/t-022-r2-gate` desde `main`. Al terminar y validar, hacer commit Conventional Commits, push y crear un PR con evidencia; solicitar aprobación a **@acamposve**. No aprobar ni fusionar el propio PR.
