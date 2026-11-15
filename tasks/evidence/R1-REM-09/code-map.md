# R1-REM-09 — Code map

## 1. Identity

| Artifact | Path | Purpose |
|---|---|---|
| Email sender | `src/Avila.Platform/Identity/EmailSender.cs` | SMTP/sink abstraction |
| Password/email tokens | `src/Avila.Platform/Identity/IdentityDbContext.cs` | Reset/verification tables |
| Identity service | `src/Avila.Platform/Identity/IdentityService.cs` | Reset/verification/audit |
| Audit writer | `src/Avila.Platform/Identity/IdentityAuditWriter.cs` | `auth.login.failed` |

## 2. API

| Artifact | Path | Purpose |
|---|---|---|
| Auth endpoints | `src/Avila.Api/Identity/IdentityEndpoints.cs` | Forgot/reset/verify |
| Rate limiter | `src/Avila.Api/Identity/AuthRateLimiter.cs` | 429 + Retry-After |
| Registration | `src/Avila.Api/Program.cs` | Rate limiter middleware |

## 3. Tests/docs

| Artifact | Path |
|---|---|
| Recovery/rate tests | `tests/Avila.ProductionTests/RecoveryRateLimitTests.cs` |
| Operations | `docs/operations/recovery-email-rate-limiting.md` |

## 4. Evidence

- `tasks/evidence/R1-REM-09/execution-log.md`
- `tasks/evidence/R1-REM-09/verification.md`
- `tasks/evidence/R1-REM-09/raw/test-output.txt`

## 5. Boundaries

- No real SMTP delivery/outbox.
- No distributed rate limit.
- No 429 audit table.
- No G1 opening.
