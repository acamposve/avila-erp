# T-009 — Numeración e idempotencia transaccional

**Fase:** R1 · **Depende de:** [T-005](T-005-tenancy-uow.md), [T-007](T-007-provisioning.md).

## Trabajo
- Implementar series, ámbito y numeración transaccional conforme a ADR-006/008; reemplazar marcadores de posición cuando aplique.
- Proveer persistencia idempotente para comandos críticos, incluidos reintentos concurrentes y rollback.

## Aceptación y evidencia
- 10.000 emisiones concurrentes sin duplicados ni huecos injustificados, ámbitos empresa/sucursal aislados y rollback correcto; solicitudes con la misma clave producen un único efecto.
- Trazabilidad: `US-ADM-006`, `AC-ADM-06`, `AC-FAC-01`, [ADR-006](../docs/decisions/ADR-006-document-numbering.md), [ADR-008](../docs/decisions/ADR-008-concurrency-by-aggregate.md).

## Flujo obligatorio
Crear una rama nueva `feature/t-009-numbering-idempotency` desde `main`. Al terminar y validar, hacer commit Conventional Commits, push y crear un PR con evidencia; solicitar aprobación a **@acamposve**. No aprobar ni fusionar el propio PR.
