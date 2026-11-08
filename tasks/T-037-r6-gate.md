# T-037 — Preparar evidencia de salida R6

**Fase:** R6 · **Depende de:** [T-034](T-034-integrations.md), [T-035](T-035-production-crm.md), [T-036](T-036-enterprise-platform.md).

## Trabajo
- Consolidar evidencia de partner, API y sandbox, producción costeada, conversión CRM, SSO y tenant dedicado.
- Verificar aprobación de licencia y decisiones aplicables antes de cualquier publicación.

## Aceptación y evidencia
- Matriz completa de [criterios R6](../backlog/releases.md), responsables y evidencias; cualquier pendiente bloquea el cierre.
- Solicitar decisión humana: el PR aprobado no sustituye la autorización de release ni de despliegue.

## Flujo obligatorio
Crear una rama nueva `docs/t-037-r6-gate` desde `main`. Al terminar y validar, hacer commit Conventional Commits, push y crear un PR con evidencia; solicitar aprobación a **@acamposve**. No aprobar ni fusionar el propio PR.
