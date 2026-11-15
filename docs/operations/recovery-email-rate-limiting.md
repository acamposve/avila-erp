# Password recovery, email verification and auth rate limiting

**Estado:** R1-REM-09 · **Fecha:** 2026-11-15 · **Dueño:** @acamposve

## 1. Tokens

| Flujo | Vida | Almacenamiento | Uso |
|---|---|---|---|
| Password reset | 30 min | SHA-256 del token | Un solo uso |
| Email verification | 24 h | SHA-256 del token | Un solo uso |

- Tokens de 256 bits; nunca se guardan en claro.
- Reset consume el token y revoca todas las sesiones del usuario.
- Verificación marca `email_verified`.
- Invitación aceptada marca el correo como verificado (el token de invitación ya acredita el correo).

## 2. Enumeration safety

`POST /api/v1/auth/password/forgot` y `/email/verification/request` devuelven **202 Accepted** siempre.

- Usuario existente: crea token y envía email.
- Usuario inexistente: no crea token, no envía email, misma respuesta.
- No se revela existencia por body/status.

> La medición de tiempos no es una garantía criptográfica; el test cubre status/body y ausencia de email para inexistentes.

## 3. Email

- `IEmailSender` con `SmtpEmailSender` (SMTP estándar, credenciales por configuración/secret manager) y `SinkEmailSender` local/Testcontainers.
- Configuración: `Email:Smtp:Host`, `Port`, `From`, `UserName`, `Password`, `EnableSsl`.
- Sin host configurado, el API usa el sink local; no envía correo real.
- No se loguean tokens ni cuerpos.

## 4. Rate limiting

Middleware para `/api/v1/auth`:

| Ruta | Límite |
|---|---|
| login / forgot / MFA | 10 por minuto |
| resto auth | 30 por minuto |

- Clave: IP + path (in-memory, por instancia).
- Respuesta `429` con `Retry-After` y body `RATE_LIMITED`.
- Se registra warning con correlación, sin PII.
- No hay límite distribuido; R2+ requiere Redis/gateway.

## 5. Auditoría de brute force

Un login fallido de un usuario existente registra `auth.login.failed` en `platform.audit_events` con:

- tenant y actor;
- acción y motivo;
- correlación.

Se escribe en una transacción independiente. No se registra password, token ni email.

## 6. Endpoints

```text
POST /api/v1/auth/password/forgot
POST /api/v1/auth/password/reset
POST /api/v1/auth/email/verification/request
POST /api/v1/auth/email/verification/confirm
```

## 7. Límites

- El envío SMTP es síncrono dentro de la petición; R2 debe usar outbox.
- No hay rate limit distribuido ni CAPTCHA.
- La auditoría de 429 no está en `audit_events` (no hay tenant resuelto); sólo log.
- No se prueba entrega SMTP real en CI.
