# Tareas del plan de ejecución

Este directorio desglosa [el plan de ejecución](../docs/project-execution-plan.md). Cada archivo `T-*.md` es una unidad de trabajo propuesta, no una tarea ya aprobada ni terminada. Los IDs son propios de este directorio y no sustituyen IDs de historias, requisitos, criterios, decisiones o riesgos.

## Uso

1. Seleccionar una tarea y verificar sus dependencias, las decisiones abiertas, los requisitos, los criterios de aceptación y el estado real del código antes de empezar.
2. Si falta aprobación, fuente normativa, criterio verificable o autorización para infraestructura, detener la parte bloqueada y escalarla. Las tareas de decisiones preparan evidencia para quien decide; no autorizan al ejecutor a aprobarlas.
3. Cada tarea exige una rama nueva y un PR separado con evidencia, revisión humana y aprobación de **@acamposve**. La aprobación del PR no reemplaza aprobaciones de arquitectura, seguridad o especialistas. No fusionar, desplegar, modificar workflows ni ejecutar migraciones en entornos compartidos sin autorización.
4. Si una tarea resulta demasiado grande, dividirla en cambios revisables conservando requisitos, dependencias y el flujo de rama/PR por cada parte. No inventar fechas.

## Evidencia obligatoria

Cada tarea debe dejar un paquete en [`tasks/evidence/T-XXX/`](evidence/README.md) con:

- `execution-log.md` — fecha, rama, commit, entorno, comandos y resultados;
- `verification.md` — criterios de aceptación contra evidencia verificable;
- `code-map.md` — implementación, migraciones, pruebas y PR; o el sustituto aprobado para tareas de decisión/compuerta.

Una tarea sin esos artefactos **no está validada**, aunque el código exista o el PR esté aprobado. Los estados permitidos y la plantilla están en [evidence/README.md](evidence/README.md). Las primeras tareas no deben empezar hasta que el responsable autorice la implementación.

## Índice

| Fase | Tareas |
|---|---|
| 0 — Preparación | [T-001](T-001-baseline.md), [T-002](T-002-decisions.md), [T-003](T-003-hardening-backlog.md), [T-004](T-004-r5-dependency.md) |
| R1 — Core | [T-005](T-005-tenancy-uow.md), [T-006](T-006-identity.md), [T-007](T-007-provisioning.md), [T-008](T-008-authorization-audit.md), [T-009](T-009-numbering-idempotency.md), [T-010](T-010-configuration.md), [T-011](T-011-platform-lifecycle.md), [T-012](T-012-operations.md), [T-013](T-013-r1-gate.md) |
| R2 — Comercial | [T-014](T-014-catalog-stock.md), [T-015](T-015-inventory-controls.md), [T-016](T-016-purchasing.md), [T-017](T-017-sales.md), [T-018](T-018-fiscal-decisions.md), [T-019](T-019-billing.md), [T-020](T-020-treasury.md), [T-021](T-021-import-reports.md), [T-022](T-022-r2-gate.md) |
| R3 — Finance | [T-023](T-023-accounting-validation.md), [T-024](T-024-accounting-posting.md), [T-025](T-025-finance-cutover.md), [T-026](T-026-r3-gate.md) |
| R4 — Retail | [T-027](T-027-retail-decisions.md), [T-028](T-028-retail-delivery.md), [T-029](T-029-r4-gate.md) |
| R5 — People | [T-030](T-030-payroll-validation.md), [T-031](T-031-payroll-delivery.md), [T-032](T-032-r5-gate.md) |
| R6 — Enterprise | [T-033](T-033-enterprise-decisions.md), [T-034](T-034-integrations.md), [T-035](T-035-production-crm.md), [T-036](T-036-enterprise-platform.md), [T-037](T-037-r6-gate.md) |

Las tareas de compuerta preparan y registran evidencia; el cierre del release requiere la decisión humana pertinente y todos los criterios de [releases](../backlog/releases.md). Una aprobación del PR por sí sola no abre la compuerta.
