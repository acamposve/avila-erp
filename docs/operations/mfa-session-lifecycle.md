# MFA and session lifecycle

**Estado:** R1-REM-08 · **Fecha:** 2026-11-15 · **Dueño:** @acamposve

## 1. MFA obligatorio

- `owner`, `admin` y operadores de plataforma sin MFA reciben `MfaEnrollmentRequired` al iniciar sesión.
- Flujo:
  1. `POST /api/v1/auth/login` → challenge.
  2. `POST /api/v1/auth/mfa/enroll/begin` → secreto TOTP.
  3. `POST /api/v1/auth/mfa/enroll/complete` → sesión + recovery codes.
- Usuarios con MFA reciben `MfaRequired` y usan `/mfa/verify`.
- El secreto TOTP se cifra con AES-GCM.

## 2. Recovery codes

- 10 códigos aleatorios por enrolamiento.
- Se guardan **solo hasheados** (SHA-256), nunca en claro.
- Un solo uso; el uso se marca con `used_at`.
- Regenerar reemplaza los códigos no usados.
- Sirven para MFA login y step-up.

## 3. Sesiones opacas

| Control | Valor |
|---|---|
| Token | 256 bits, hash SHA-256 en reposo |
| Expiración absoluta | 8 h |
| Inactividad | 30 min desde `last_seen_at` |
| Revocación | logout, switch-tenant, cambio de rol |
| Replay | token revocado/expirado se rechaza |
| Cookie | `avila_session`, HttpOnly, Secure, SameSite=Strict |
| Bearer | soportado para API |

`AuthenticateAsync` rechaza y revoca una sesión inactiva. `ChangeRoleAsync` revoca todas las sesiones del usuario en el tenant.

## 4. Step-up

`session.stepped_up_at` se actualiza al verificar MFA (login o `/api/v1/auth/step-up`). Es válido por **5 minutos**.

Operaciones sensibles:

- export completo del tenant;
- cambio de rol;
- desactivación de MFA;
- revocación de todas las sesiones.

La exportación responde `403 STEP_UP_REQUIRED` sin step-up reciente.

## 5. Flujo HTTP E2E

`HttpMfaExportE2eTests` usa `WebApplicationFactory` + Testcontainers:

1. login owner → `MfaEnrollmentRequired`;
2. begin/complete enrollment;
3. bearer token;
4. `GET /api/v1/tenant/export` → 200 NDJSON.

## 6. Límites

- No hay WebAuthn; solo TOTP.
- La cookie se emite `Secure`; en HTTP local no se enviará (los tests usan bearer).
- No hay notificación de nuevo dispositivo.
- Recovery codes no expiran por fecha; se consumen o regeneran.
- Password reset/email/rate limiting son REM-09.
