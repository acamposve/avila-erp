# R1-REM-02 — Verification

**Task:** R1-REM-02 — Production PostgreSQL roles and grants
**Status:** `implemented` — awaiting human review. Not `verified`.
**Date:** 2026-11-14

## 1. Acceptance criteria

| Criterio | Evidencia | Resultado |
|---|---|---|
| `roles.sql` idempotente | `RoleBootstrapTests` executes it twice; no error | **Pass** |
| `avila_app` least privilege | Attributes false; no `platform.tenants`/`identity.*`; audit INSERT/SELECT only | **Pass** |
| `avila_identity` isolated identity export grants | `identity.users` SELECT true; `admin.companies` false | **Pass** |
| `avila_platform` minimal | `platform.tenants` UPDATE true; `admin.companies` false | **Pass** |
| Testcontainers uses exact script | Fixture reads `AppContext.BaseDirectory/ops/postgres/roles.sql`; test asserts file exists | **Pass** |
| No secrets in script | Script has no password; test credentials are generated per run and applied after bootstrap | **Pass** |
| No RLS bypass | All three `rolsuper=false`, `rolbypassrls=false` | **Pass** |
| Existing behavior preserved | Full suite 73/73 | **Pass** |

## 2. Test coverage

| Test | What it proves |
|---|---|
| `Roles_bootstrap_is_idempotent_and_least_privilege` | Attributes, grants, audit append-only, app/identity/platform separation |
| Existing 72 tests | No regression after replacing fixture bootstrap |

Production suite: **54/54**. Full suite: **73/73**.

## 3. Role model

| Role | Access |
|---|---|
| `avila_app` | Tenant/business tables + tenant-scoped platform; no global tenants/identity |
| `avila_identity` | Identity schema only; enough for safe identity export |
| `avila_platform` | `platform.tenants` + `platform.tenant_lifecycle` only |

## 4. Operational notes

- Roles are created `NOLOGIN`; deployment sets credentials with the secret manager.
- Bootstrap order: migrations → `roles.sql` → login credentials.
- No `GRANT ALL`, no superuser, no `BYPASSRLS`.
- Future tables need explicit grants; this is a deliberate least-privilege constraint.

## 5. Open gaps

- No production application of roles (requires operations authorization).
- No automated grant manifest for future R2 tables yet; each migration must include it.
- RLS policy/audit registry is REM-03.
- Pending @acamposve review.

## 6. Evidence

- [roles.sql](../../../ops/postgres/roles.sql)
- [Roles/grant doc](../../../docs/operations/roles-and-grants.md)
- [Fixture](../../../tests/Avila.ProductionTests/ProductionFixture.cs)
- [Role test](../../../tests/Avila.ProductionTests/RoleBootstrapTests.cs)
- [execution-log.md](execution-log.md)

## 7. Conclusion

REM-02 meets its acceptance criteria: one idempotent password-free bootstrap script defines least-privilege app/identity/platform roles, and Testcontainers executes that exact file. Ready for review; no production role application or G1 opening.
