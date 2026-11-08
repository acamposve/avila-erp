# T-006 — Identidad, membresías y seguridad de sesión

**Fase:** R1 · **Depende de:** [T-002](T-002-decisions.md), [T-005](T-005-tenancy-uow.md); `DEC-003` aprobada.

## Trabajo
- Implementar identidad según decisión aprobada, invitaciones, membresías multi-organización y selección validada de tenant.
- Implementar MFA, política de cuenta, expiración y revocación de sesiones; no exponer endpoints de negocio sin autenticación y autorización.

## Aceptación y evidencia
- Probar invitación y cambio de organización sin mezcla de datos, rechazo de token/tenant manipulado, MFA y revocación.
- Trazabilidad: `US-ADM-003`, `US-ADM-007`, `US-ADM-008`, `AC-ADM-02`, `AC-SEC-02`; [seguridad](../docs/architecture/security.md).

## Flujo obligatorio
Crear una rama nueva `feature/t-006-identity` desde `main`. Al terminar y validar, hacer commit Conventional Commits, push y crear un PR con evidencia; solicitar aprobación a **@acamposve**. No aprobar ni fusionar el propio PR.
