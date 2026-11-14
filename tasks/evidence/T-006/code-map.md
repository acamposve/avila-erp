# T-006 — Code map

## 1. Identity persistence

| Artefacto | Ruta | Propósito |
|---|---|---|
| Identity context | `src/Avila.Platform/Identity/IdentityDbContext.cs` | Users, memberships, invitations, sessions, MFA challenges |
| Migration | `src/Avila.Platform/Migrations/Identity/20261108182317_InitialIdentity.cs` | `identity` schema; no tenant RLS; identity-role gate |
| Design factory | `src/Avila.Platform/Identity/IdentityDbContextFactory.cs` | Migrations |
| Identity service | `src/Avila.Platform/Identity/IdentityService.cs` | Invitations, login, MFA, switch, revoke |
| Security primitives | `src/Avila.Platform/Identity/SecurityPrimitives.cs` | PBKDF2, tokens, TOTP |
| Secret protector | `src/Avila.Platform/Identity/SecretProtector.cs` | AES-GCM for TOTP seeds |

## 2. HTTP surface

| Artefacto | Ruta | Propósito |
|---|---|---|
| Session middleware | `src/Avila.Api/Identity/SessionAuthenticationMiddleware.cs` | Bearer token → `ITenantContext` |
| Auth endpoints | `src/Avila.Api/Identity/IdentityEndpoints.cs` | Login, MFA, switch, logout, invitation accept |
| DI wiring | `src/Avila.Api/Identity/IdentityServiceCollectionExtensions.cs` | Register identity service/config |
| Host wiring | `src/Avila.Api/Program.cs` | Session + UoW hook for `/api` |

## 3. Database

| Schema/table | Change |
|---|---|
| `identity.users` | Global account; password hash + encrypted TOTP secret |
| `identity.memberships` | User/tenant membership; unique pair |
| `identity.invitations` | Token hash, expiry, single use |
| `identity.sessions` | Token hash, active tenant, expiry, revocation |
| `identity.mfa_challenges` | Short-lived MFA challenge |

## 4. Tests

| Test | File |
|---|---|
| Invitations, multi-org, MFA, lockout, expiry, revocation, middleware | `tests/Avila.ProductionTests/IdentityTests.cs` |
| Fixture identity migration/roles | `tests/Avila.ProductionTests/ProductionFixture.cs` |

## 5. Documentation

| Documento | Change |
|---|---|
| `docs/architecture/security.md` | T-006 opaque sessions, encrypted TOTP, pending gaps |
| `docs/tasks-validation.md` | T-005 verified, T-006 implemented |

## 6. Boundaries

- No T-007 provisioning.
- No T-008 RBAC/audit.
- No business endpoint.
- No JWT/refresh; opaque sessions only.
- No email, password recovery or rate limiting.
- No production key/role provisioning.
- No shared migration or deployment.
