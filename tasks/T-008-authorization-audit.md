# T-008 — Permisos, auditoría y soporte

**Fase:** R1 · **Depende de:** [T-006](T-006-identity.md), [T-007](T-007-provisioning.md).

## Trabajo
- Implementar RBAC y alcance empresa/sucursal/almacén, denegación por defecto y autorización por recurso en servidor.
- Registrar auditoría append-only y acceso de soporte con rol, ticket, justificación, expiración y mínimo privilegio.

## Aceptación y evidencia
- Probar permiso denegado sin efectos, separación de tenants, registro actor/antes/después/correlación y protección ante modificación de auditoría.
- Trazabilidad: `US-ADM-004`, `US-ADM-005`, `US-ADM-011`, `AC-ADM-03`, `AC-ADM-04`, `AC-PLT-03`; [seguridad](../docs/architecture/security.md).

## Flujo obligatorio
Crear una rama nueva `feature/t-008-authorization-audit` desde `main`. Al terminar y validar, hacer commit Conventional Commits, push y crear un PR con evidencia; solicitar aprobación a **@acamposve**. No aprobar ni fusionar el propio PR.
