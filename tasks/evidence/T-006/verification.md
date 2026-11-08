# T-006 — Verification

**Task:** [T-006 — Identidad, membresías y seguridad de sesión](../../T-006-identity.md)
**Status:** `implemented` — awaiting human review. Not `verified`.
**Date:** 2026-11-08

## 1. Acceptance criteria

| Criterio de T-006 | Evidencia | Resultado |
|---|---|---|
| Invitación y cambio de organización sin mezcla de datos | Invitation test; multi-org login/switch test; identity role separated from app role | **Pass** |
| Rechazo de token/tenant manipulado | Random token rejected; switch to non-member tenant rejected; old token revoked | **Pass** |
| MFA | TOTP enrollment, confirm, login challenge, wrong code rejected, valid code accepted | **Pass** |
| Revocación de sesión | Logout/revoke test; expired session test | **Pass** |
| Política de cuenta | Minimum 12 chars; lockout after 5 failures | **Pass** |
| No business endpoint without auth | No business endpoint exposed; `/api` requires session + tenant; auth endpoints excluded | **Pass** |

## 2. Test coverage

| Test | What it proves |
|---|---|
| `Invitation_acceptance_creates_user_and_membership_and_is_single_use` | Invitation token, user/membership creation, single use |
| `Multi_org_login_requires_tenant_selection_and_switch_revokes_old_token` | Multi-org selection, switch validation, old token revocation |
| `Manipulated_token_and_revoked_session_are_rejected` | Token tampering and logout |
| `Mfa_enrollment_and_login_require_a_valid_code` | TOTP enrollment, challenge, wrong/valid code |
| `Password_policy_rejects_short_password` | Account policy |
| `Account_is_locked_after_five_failed_attempts` | Lockout |
| `Expired_session_is_rejected` | Expiration |
| `Session_middleware_sets_tenant_and_rejects_invalid_token` | HTTP session-to-tenant bridge |

Production suite: **27/27**. Full suite: **46/46**.

## 3. Security properties

| Property | Implementation |
|---|---|
| Session token | 256-bit random; only SHA-256 hash stored |
| Session lifetime | 8 hours; revocation timestamp |
| Tenant switch | Membership checked; old session revoked; new token issued |
| Password | PBKDF2-HMAC-SHA256, 120,000 iterations, 16-byte salt |
| MFA | TOTP SHA-1, 6 digits, 30 s, ±1 window; secret AES-GCM encrypted |
| Invitation | 256-bit token; hash stored; 7-day expiry; single use |
| Lockout | 5 failures → 15-minute lock |
| Identity DB | `identity` schema; only `avila_identity` role; no tenant RLS |
| Tenant bridge | Session middleware sets `ITenantContext`; T-005 UoW applies RLS |

## 4. Identity data classification

| Table | Scope | Protection |
|---|---|---|
| `identity.users` | Global | `avila_identity` only; password hash + encrypted MFA secret |
| `identity.memberships` | User/tenant | Identity role only; no app grant |
| `identity.invitations` | Tenant invite | Hash only; identity role |
| `identity.sessions` | User/session | Hash only; identity role |
| `identity.mfa_challenges` | User/tenant challenge | Identity role |

Identity tables are intentionally **not tenant-RLS**: login must resolve memberships before tenant selection. The gate is the dedicated DB role, not an ambient tenant.

## 5. Open security gaps

- No password recovery or email verification flow.
- No email delivery, lockout notification or HTTP rate limiting.
- MFA is available but not enforced for owner/admin roles.
- Opaque sessions diverge from the JWT/refresh target in `security.md`; an ADR is required before production.
- `Identity:MfaSecretKey` must be supplied by a secret manager; not in the repository.
- Auth endpoints lack HTTP-level E2E tests; service and middleware are covered.
- No invitation endpoint with administrative authorization (T-007/T-008).
- Production roles and key rotation are deployment concerns.

## 6. What was not done

- No T-007 provisioning, T-008 authorization/audit.
- No email sending.
- No business endpoint.
- No JWT/refresh token implementation.
- No production secret manager or role provisioning.
- No shared migration or deployment.

## 7. Evidence

- [IdentityService](../../../src/Avila.Platform/Identity/IdentityService.cs)
- [Security primitives](../../../src/Avila.Platform/Identity/SecurityPrimitives.cs)
- [Identity migration](../../../src/Avila.Platform/Migrations/Identity/20261108182317_InitialIdentity.cs)
- [Session middleware](../../../src/Avila.Api/Identity/SessionAuthenticationMiddleware.cs)
- [Auth endpoints](../../../src/Avila.Api/Identity/IdentityEndpoints.cs)
- [Identity tests](../../../tests/Avila.ProductionTests/IdentityTests.cs)
- [execution-log.md](execution-log.md)

## 8. Conclusion

T-006 meets its acceptance criteria at the service/middleware level: invitation, multi-org selection and switch, token/tenant rejection, MFA, lockout, expiration and revocation are tested. It is **not verified** until human review and the security gaps in §5 are accepted or resolved. The identity-stack ADR remains required before production.
