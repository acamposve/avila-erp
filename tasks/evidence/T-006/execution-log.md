# T-006 — Execution log

- **Fecha:** 2026-11-08
- **Tarea:** [T-006 — Identidad, membresías y seguridad de sesión](../../T-006-identity.md)
- **Preparado por:** agente de IA
- **Responsable/aprobador:** @acamposve
- **Rama:** `feature/t-006-identity`
- **Commit base:** `0a5c4db` (`Merge pull request #8`)
- **Commit de evidencia:** `1c113c2`
- **PR:** [#9](https://github.com/acamposve/avila-erp/pull/9)
- **Entorno:** local Windows + PostgreSQL 18 Testcontainers
- **Estado:** `implemented` — pendiente de revisión humana; **no** `verified`

## Comandos ejecutados

| # | Comando | Resultado | Evidencia |
|---|---|---|---|
| 1 | `dotnet test Avila.slnx --logger "console;verbosity=minimal"` | **46/46 passed** (19 spike + 27 producción) | [`raw/test-output.txt`](raw/test-output.txt) |
| 2 | `GET http://127.0.0.1:5099/health` | HTTP 200 `Healthy` | [`raw/api-health.txt`](raw/api-health.txt) |
| 3 | Migración `IdentityDbContext` | `identity` schema; usuarios, membresías, invitaciones, sesiones, retos MFA | [`src/Avila.Platform/Migrations/Identity/20261108182317_InitialIdentity.cs`](../../../src/Avila.Platform/Migrations/Identity/20261108182317_InitialIdentity.cs) |
| 4 | Pruebas de identidad | 8/8 | [`raw/test-output.txt`](raw/test-output.txt) |
| 5 | Comprobación de enlaces Markdown | 0 enlaces roto | Reporte del PR |

## Resultado

- Invitación de un solo uso; aceptación crea usuario y membresía.
- Login multi-organización: selección de tenant obligatoria cuando hay más de una membresía.
- Cambio de organización valida membresía; revoca el token anterior.
- Token manipulado y sesión revocada son rechazados.
- MFA TOTP: enrolamiento, confirmación, reto en login y verificación.
- Política de contraseña: mínimo 12 caracteres; bloqueo tras 5 fallos.
- Expiración de sesión y revocación probadas.
- Sesión opaca: 256 bits; solo se persiste SHA-256; expiración 8 h.
- Secreto TOTP cifrado con AES-GCM; clave fuera del repositorio.
- Identidad aislada en `identity`; solo `avila_identity` tiene acceso; `avila_app` no.
- T-006 queda `implemented`; no se declara ningún P1 cerrado.

## Bloqueos registrados

| Bloqueo | Dueño | Estado |
|---|---|---|
| ADR del stack HTTP/JWT vs. sesión opaca | @acamposve / Arquitectura | Pendiente antes de producción |
| Endpoint de invitación administrativa con autorización | T-007/T-008 | Pendiente; el servicio existe, no el endpoint |
| Recuperación de contraseña y verificación de correo | T-006/T-007 | Pendiente |
| Notificación de bloqueo y rate limiting HTTP | T-006/T-012 | Pendiente |
| MFA obligatorio para owner/admin | T-006/T-008 | Pendiente de política y flujo de enrolamiento |
| Gestión de la clave `Identity:MfaSecretKey` | Operaciones | Pendiente; no está en el repositorio |
| Roles de producción | Operaciones | Solo Testcontainers |
| Aprobación de este PR | @acamposve | Pendiente |

## Nota de alcance

Este log no implementa T-007 (aprovisionamiento) ni T-008 (autorización/auditoría). No se expone ningún endpoint de negocio. Los endpoints de autenticación existen, pero no se ejecutó una prueba HTTP E2E; la lógica se cubre a nivel de servicio y middleware.
