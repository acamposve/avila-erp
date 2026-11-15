# R1-REM-09 — Execution log

- **Fecha:** 2026-11-15
- **Tarea:** R1-REM-09 — Password recovery, email verification and rate limiting
- **Preparado por:** agente de IA
- **Responsable/aprobador:** @acamposve
- **Rama:** `feat/r1-rem-09-recovery-rate-limit`
- **Commit base:** `90ea414` (REM-08)
- **Commit de evidencia:** pendiente al redactar; ver PR
- **PR:** pendiente al redactar; se registra al abrir el PR
- **Estado:** `implemented` — pendiente de revisión humana; **no** `verified`

## Comandos ejecutados

| # | Comando | Resultado | Evidencia |
|---|---|---|---|
| 1 | `dotnet test Avila.slnx` | **105/105 passed** (19 spike + 86 producción) | [`raw/test-output.txt`](raw/test-output.txt) |
| 2 | `RecoveryRateLimitTests` | 4/4 | [`raw/test-output.txt`](raw/test-output.txt) |
| 3 | Comprobación de enlaces Markdown | 0 enlaces roto / 179 archivos | [`raw/link-check.txt`](raw/link-check.txt) |

## Resultado

- Password reset y verificación de email con tokens 256-bit hasheados, un solo uso y expiración.
- Reset revoca todas las sesiones del usuario.
- Respuestas 202 idénticas para emails existentes/inexistentes; sin email para inexistentes.
- `IEmailSender` con sink local y adaptador SMTP; sin secretos en repo.
- Rate limiting auth: 429 con `Retry-After`.
- Login fallido auditado como `auth.login.failed` en tenant, con transacción independiente.
- E2E HTTP de REM-08 sigue cubierto; REM-09 añade pruebas de servicio/middleware.

## Bloqueos / pendientes

| Pendiente | Dueño | Estado |
|---|---|---|
| Envío SMTP real/outbox | R2+ | Sink en CI; SMTP no probado externamente |
| Rate limit distribuido/CAPTCHA | R2+ | In-memory por instancia |
| Auditoría de 429 en `audit_events` | R1-REM-09+ | No hay tenant resuelto; solo warning log |
| Medición de timing anti-enumeración | Seguridad | Test funcional, no criptográfico |
| Aprobación de este PR | @acamposve | Pendiente |

## Nota de alcance

No se implementa entrega SMTP real ni cola de correo. El adaptador SMTP existe y se configura fuera del repositorio; CI usa el sink.
