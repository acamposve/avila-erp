# T-005 — Contexto tenant, UoW y RLS

**Fase:** R1 · **Depende de:** [T-002](T-002-decisions.md), [T-003](T-003-hardening-backlog.md); `DEC-003` para integración con identidad.

## Trabajo
- Integrar contexto validado de tenant y UoW compartida por petición o mensaje conforme a ADR-007, incluidas lecturas y rollback entre módulos.
- Cubrir RLS en tablas de negocio y `platform`, roles de mínimo privilegio y procedimiento de backfill bajo RLS. No confiar solo en filtros EF.
- Agregar pruebas de límites entre módulos y contexto en conexiones reutilizadas.

## Aceptación y evidencia
- PostgreSQL real: rechazo de acceso cruzado, pool sin tenant residual, introspección RLS y fallo inyectado con rollback completo.
- Trazabilidad: `US-PLT-002`, `AC-SEC-01`, [ADR-007](../docs/decisions/ADR-007-transaction-and-tenant-context.md) y T-04/T-07/T-07b/T-08/T-09/T-16 del [hardening](../docs/architecture/architecture-hardening-plan.md).

## Flujo obligatorio
Crear una rama nueva `feature/t-005-tenancy-uow` desde `main`. Al terminar y validar, hacer commit Conventional Commits, push y crear un PR con evidencia; solicitar aprobación a **@acamposve**. No aprobar ni fusionar el propio PR.
