# T-036 — SSO y tenant dedicado

**Fase:** R6 · **Depende de:** [T-033](T-033-enterprise-decisions.md), [T-006](T-006-identity.md); decisiones de identidad e infraestructura aprobadas.

## Trabajo
- Implementar SSO para un cliente enterprise y aprovisionamiento de tenant dedicado con el mismo código.
- Preservar autorización, aislamiento y observabilidad; no tocar infraestructura compartida sin autorización.

## Aceptación y evidencia
- Demostrar SSO y tenant dedicado con pruebas de acceso cruzado denegado y evidencia en entorno autorizado.
- Trazabilidad: `EP-PLT-04`, `PLT-006`, `ADM-021` si aprobado, `AC-PLT-03`, `AC-SEC-02`; [multi-tenancy](../docs/architecture/multi-tenancy.md).

## Flujo obligatorio
Crear una rama nueva `feature/t-036-enterprise-platform` desde `main`. Al terminar y validar, hacer commit Conventional Commits, push y crear un PR con evidencia; solicitar aprobación a **@acamposve**. No aprobar ni fusionar el propio PR.
