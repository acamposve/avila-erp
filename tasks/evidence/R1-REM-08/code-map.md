# R1-REM-08 — Code map

## 1. Identity

| Artifact | Path | Purpose |
|---|---|---|
| Service | `src/Avila.Platform/Identity/IdentityService.cs` | MFA, recovery, sessions, step-up, role revoke |
| Schema | `src/Avila.Platform/Identity/IdentityDbContext.cs` | Recovery codes, step-up, email verified |
| Migration | `src/Avila.Platform/Migrations/Identity/20261115101659_AddMfaRecoveryAndSessionStepUp.cs` | Persistence changes |

## 2. API

| Artifact | Path | Purpose |
|---|---|---|
| Auth endpoints | `src/Avila.Api/Identity/IdentityEndpoints.cs` | Login, enrollment begin/complete, step-up, cookie |
| Middleware | `src/Avila.Api/Identity/SessionAuthenticationMiddleware.cs` | Bearer/cookie session |
| Export gate | `src/Avila.Api/Exporting/TenantExportEndpoint.cs` | Step-up required |

## 3. Tests/docs

| Artifact | Path |
|---|---|
| MFA/session tests | `tests/Avila.ProductionTests/MfaSessionTests.cs` |
| HTTP E2E | `tests/Avila.ProductionTests/HttpMfaExportE2eTests.cs` |
| Export step-up | `tests/Avila.ProductionTests/ExportEndpointTests.cs` |
| Operations | `docs/operations/mfa-session-lifecycle.md` |

## 4. Evidence

- `tasks/evidence/R1-REM-08/execution-log.md`
- `tasks/evidence/R1-REM-08/verification.md`
- `tasks/evidence/R1-REM-08/raw/test-output.txt`

## 5. Boundaries

- No WebAuthn/push/device notification.
- No password reset/email/rate limit (REM-09).
- No G1 opening.
