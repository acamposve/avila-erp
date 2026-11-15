# R1-REM-08 — Verification

**Task:** R1-REM-08 — Mandatory MFA and session lifecycle
**Status:** `implemented` — awaiting human review. Not `verified`.
**Date:** 2026-11-15

## 1. Acceptance criteria

| Criterio | Evidencia | Resultado |
|---|---|---|
| MFA obligatorio owner/admin/platform | `MfaEnrollmentRequired` test; no token | **Pass** |
| TOTP enrollment | Begin/complete challenge test | **Pass** |
| Recovery codes single-use, hashed | 10 codes; DB hash ≠ plaintext; replay fails | **Pass** |
| Step-up sensitive export | `STEP_UP_REQUIRED` + step-up test | **Pass** |
| Role change revokes sessions | ChangeRole + authenticate null | **Pass** |
| 8 h absolute expiry | Expired session null | **Pass** |
| 30 min idle | Idle session null | **Pass** |
| Replay rejected | Revoked/old token null; recovery replay | **Pass** |
| HTTP E2E | login → MFA → bearer → export 200 | **Pass** |

## 2. Test coverage

| Test | What it proves |
|---|---|
| `Owner_requires_mfa_enrollment_and_recovery_codes_are_hashed_and_single_use` | Mandatory MFA, hashed/single-use recovery |
| `Idle_absolute_expiry_role_change_and_replay_invalidate_sessions` | Session lifecycle |
| `Step_up_marks_session_and_expires_after_five_minutes` | Step-up |
| `Export_requires_recent_step_up` | Export gate |
| `Login_enroll_mfa_then_bearer_export_returns_ndjson` | Full HTTP E2E |

Production suite: **82/82**. Full suite: **101/101**.

## 3. Security properties

| Property | Value |
|---|---|
| Session token | 256 bits; SHA-256 stored |
| Absolute expiry | 8 h |
| Idle timeout | 30 min |
| Step-up window | 5 min |
| Recovery codes | 10, SHA-256 at rest, single-use |
| TOTP secret | AES-GCM encrypted |
| Role change | revokes tenant sessions |
| Cookie | HttpOnly, Secure, SameSite=Strict |

## 4. Open gaps

- No WebAuthn/device notification.
- No password reset yet (REM-09).
- No custom roles to enumerate beyond owner/admin.
- E2E uses bearer; secure cookie is not exercised over plain HTTP.
- Pending review.

## 5. Evidence

- [Identity service](../../../src/Avila.Platform/Identity/IdentityService.cs)
- [Identity schema](../../../src/Avila.Platform/Identity/IdentityDbContext.cs)
- [Identity migration](../../../src/Avila.Platform/Migrations/Identity/20261115101659_AddMfaRecoveryAndSessionStepUp.cs)
- [MFA tests](../../../tests/Avila.ProductionTests/MfaSessionTests.cs)
- [HTTP E2E](../../../tests/Avila.ProductionTests/HttpMfaExportE2eTests.cs)
- [Operations doc](../../../docs/operations/mfa-session-lifecycle.md)
- [execution-log.md](execution-log.md)

## 6. Conclusion

REM-08 meets its acceptance criteria: mandatory MFA, hashed single-use recovery codes, session idle/absolute expiry, role-change revocation, step-up, and full HTTP MFA→export E2E are implemented and tested. G1 remains closed.
