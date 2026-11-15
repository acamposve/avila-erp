# R1 export contributors

**Estado:** R1-REM-06 · **Fecha:** 2026-11-15 · **Dueño:** @acamposve

## 1. Contribuyentes

| Módulo | Order | Entidades | Fuente |
|---|---:|---|---|
| `administration` | 10 | companies, branches, parties | `AdministrationModule.ExportAsync` |
| `identity` | 20 | memberships, invitations | `avila_identity` + `IdentityDbContext` |
| `audit` | 30 | audit_events | `platform.audit_events` bajo RLS |

El orquestador no conoce estos módulos; solo recibe `ITenantExportContributor` en el composition root.

## 2. Identidad segura

- Conexión exclusiva `avila_identity`; el contributor verifica `current_user` y rechaza otro rol.
- Filtra `Membership.TenantId` y `Invitation.TenantId` por el tenant del `TenantExportContext`.
- Proyecta un DTO seguro:
  - membership: user id, rol, estado, fecha, datos mínimos del usuario (email, display name, MFA habilitado);
  - invitation: email, rol, expiración, aceptación, fecha.
- **Nunca** proyecta `password_hash`, `mfa_secret`, recovery codes, `token_hash` de sesión/invitación ni sesiones.
- La prueba serializa el envelope completo y busca tanto valores centinela como nombres de propiedades prohibidas.

## 3. Administración y auditoría

- Administración exporta empresas, sucursales y terceros con el tenant de la UoW.
- Auditoría exporta `platform.audit_events` del tenant, con actor, entidad, before/after y correlación.
- El registry ordena secciones y registros; los contribuyentes no controlan el orden final.

## 4. Aislamiento A/B

La prueba crea datos homólogos en A y B (empresa, tercero, usuario, invitación, sesión, auditoría) y verifica:

- el JSON de A contiene solo A;
- el JSON de B contiene solo B;
- ningún valor centinela de credenciales aparece;
- el hash de un estado idéntico es estable.

## 5. Límites

- No hay endpoint HTTP todavía (REM-07).
- No se exportan sesiones, MFA challenges, recovery codes ni secretos.
- El email es PII legítima de exportación; la entrega debe ser autorizada, TLS y auditada.
- No se implementa streaming ni autorización de endpoint.
