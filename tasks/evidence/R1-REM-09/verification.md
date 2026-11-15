# R1-REM-09 — Verification

**Task:** R1-REM-09 — Password recovery, email verification and rate limiting
**Status:** `implemented` — awaiting human review. Not `verified`.
**Date:** 2026-11-15

## 1. Acceptance criteria

| Criterio | Evidencia | Resultado |
|---|---|---|
| Hashed single-use reset token + expiry | 30 min; DB hash ≠ plaintext; reuse fails | **Pass** |
| Hashed single-use verification token + expiry | 24 h; DB hash; reuse fails | **Pass** |
| Email via `IEmailSender` | Sink in tests; SMTP adapter | **Pass** |
| Enumeration-safe responses | Missing email no token/no mail; same 202 | **Pass** |
| Reset revokes sessions | Old token null after reset | **Pass** |
| 429 rate limit | 11th login request blocked + Retry-After | **Pass** |
| Brute-force audit | `auth.login.failed` in tenant audit | **Pass** |
| No regression | 105/105 | **Pass** |

## 2. Test coverage

| Test | What it proves |
|---|---|
| `Password_reset_is_single_use_enumeration_safe_and_revokes_sessions` | Reset, replay, revocation, no email for missing |
| `Email_verification_token_is_single_use_and_hashed` | Verification hash/consume |
| `Rate_limiter_returns_429_and_retry_after` | Middleware |
| `Failed_login_is_security_audited` | Brute-force event |

Production suite: **86/86**. Full suite: **105/105**.

## 3. Security properties

- Tokens: 256-bit, SHA-256 at rest, single-use.
- Reset revokes sessions and clears lockout.
- Verification sets `email_verified`.
- Responses do not reveal account existence.
- Login failure audit is independent and PII-free.
- SMTP credentials from config/secret manager only.

## 4. Open gaps

- SMTP delivery not exercised externally.
- Rate limit is per-instance/in-memory; no distributed limit.
- 429 audit only log, not `audit_events`.
- Timing side-channel not measured cryptographically.
- Email sending is synchronous (outbox in R2).
- Pending review.

## 5. Evidence

- [Email adapter](../../../src/Avila.Platform/Identity/EmailSender.cs)
- [Identity service](../../../src/Avila.Platform/Identity/IdentityService.cs)
- [Identity audit](../../../src/Avila.Platform/Identity/IdentityAuditWriter.cs)
- [Rate limiter](../../../src/Avila.Api/Identity/AuthRateLimiter.cs)
- [Tests](../../../tests/Avila.ProductionTests/RecoveryRateLimitTests.cs)
- [Operations doc](../../../docs/operations/recovery-email-rate-limiting.md)
- [execution-log.md](execution-log.md)

## 6. Conclusion

REM-09 meets its acceptance criteria: hashed single-use reset/verification tokens, enumeration-safe requests, session revocation, SMTP/sink abstraction, 429 rate limiting and brute-force audit are tested. G1 remains closed.
