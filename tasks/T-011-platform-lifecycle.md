# T-011 — Planes, ciclo de vida y exportación

**Fase:** R1 · **Depende de:** [T-007](T-007-provisioning.md), [T-008](T-008-authorization-audit.md).

## Trabajo
- Implementar límites y medición, feature flags, consola de operador y suspensión/reactivación conforme al alcance R1.
- Implementar exportación completa por tenant y control de acceso durante suspensión, sin definir precios comerciales pendientes.

## Aceptación y evidencia
- Probar límites, bloqueo de escritura suspendida, política de lectura/exportación y rechazo de exportación cruzada.
- Trazabilidad: `US-PLT-003`, `US-PLT-004`, `US-ADM-013`, `AC-PLT-01`, `AC-PLT-02`, `AC-ADM-07`; [releases](../backlog/releases.md).

## Flujo obligatorio
Crear una rama nueva `feature/t-011-platform-lifecycle` desde `main`. Al terminar y validar, hacer commit Conventional Commits, push y crear un PR con evidencia; solicitar aprobación a **@acamposve**. No aprobar ni fusionar el propio PR.
