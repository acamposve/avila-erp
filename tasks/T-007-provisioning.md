# T-007 — Aprovisionamiento y maestros administrativos

**Fase:** R1 · **Depende de:** [T-005](T-005-tenancy-uow.md), [T-006](T-006-identity.md).

## Trabajo
- Implementar aprovisionamiento de organización, empresa, sucursal y administrador, con preferencias y aislamiento.
- Cubrir empresas, sucursales, terceros con roles y perfil fiscal sin inventar requisitos regulatorios.

## Aceptación y evidencia
- Dos organizaciones sintéticas se aprovisionan sin acceso cruzado; perfil incompleto bloquea emisión cuando corresponda.
- Trazabilidad: `US-PLT-001`, `US-ADM-001`, `US-ADM-002`, `US-ADM-010`, `AC-ADM-01`, `AC-ADM-05`; [administración](../docs/domain/administration.md).

## Flujo obligatorio
Crear una rama nueva `feature/t-007-provisioning` desde `main`. Al terminar y validar, hacer commit Conventional Commits, push y crear un PR con evidencia; solicitar aprobación a **@acamposve**. No aprobar ni fusionar el propio PR.
