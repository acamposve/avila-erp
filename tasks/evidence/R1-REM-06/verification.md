# R1-REM-06 — Verification

**Task:** R1-REM-06 — R1 export contributors
**Status:** `implemented` — awaiting human review. Not `verified`.
**Date:** 2026-11-15

## 1. Acceptance criteria

| Criterio | Evidencia | Resultado |
|---|---|---|
| Administration exporta companies/branches/parties | Contributor + A/B test | **Pass** |
| Audit exporta audit_events del tenant | Contributor + A/B test | **Pass** |
| Identity usa `avila_identity` | Role check + test | **Pass** |
| Identity filtra por membresía | Membership/invitation tenant filter | **Pass** |
| DTO seguro sin credenciales | Serialized JSON sentinel/property assertions | **Pass** |
| A/B no se mezclan | A/B JSON assertions | **Pass** |
| Orden determinista del registry | Existing REM-05 tests + stable hash | **Pass** |
| Sin regresión | 89/89 tests | **Pass** |

## 2. Test coverage

| Test | What it proves |
|---|---|
| `R1_contributors_export_only_the_target_tenant_and_never_credentials` | A/B isolation + safe identity + audit |
| `Identity_contributor_rejects_non_identity_role` | Least-privilege role enforcement |
| `Identical_export_state_has_same_manifest_hash` | Deterministic hash |

Production suite: **70/70**. Full suite: **89/89**.

## 3. Safe identity projection

Included:

- membership id, user id, role, status, created date;
- user email, display name, MFA enabled, created date;
- invitation email, role, expiry, acceptance, created date.

Excluded:

- `password_hash`;
- `mfa_secret`;
- recovery codes;
- session tokens/hashes;
- invitation token hash;
- MFA challenge data.

The test serializes the complete envelope and asserts both sentinel values and forbidden property names are absent.

## 4. Cross-tenant

Tenant A/B have homologous synthetic data:

- company, branch, party;
- user, membership, invitation, session;
- audit event.

A's envelope contains only A; B's only B.

## 5. Open gaps

- No HTTP endpoint/auth/streaming yet (REM-07).
- Audit before/after may contain business PII; authorization is REM-07.
- No export size limits/compression.
- Pending review.

## 6. Evidence

- [Administration contributor](../../../src/Modules/Administration/Avila.Modules.Administration/AdministrationExportContributor.cs)
- [Audit contributor](../../../src/Avila.Platform/Exporting/AuditExportContributor.cs)
- [Identity contributor](../../../src/Avila.Platform/Exporting/IdentityExportContributor.cs)
- [Tests](../../../tests/Avila.ProductionTests/ExportContributorTests.cs)
- [Operations doc](../../../docs/operations/export-contributors.md)
- [execution-log.md](execution-log.md)

## 7. Conclusion

REM-06 meets its acceptance criteria: R1 contributors are tenant-scoped, identity is safe and role-restricted, and cross-tenant leakage is tested. Ready for review; no endpoint yet, G1 remains closed.
