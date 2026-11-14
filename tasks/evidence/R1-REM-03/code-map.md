# R1-REM-03 — Code map

## 1. RLS registry

| Artifact | Path | Purpose |
|---|---|---|
| Entity | `src/Avila.Platform/Persistence/PlatformDbContext.cs` | `RlsPolicyRegistry` mapping |
| Migration | `src/Avila.Platform/Migrations/20261114214054_AddRlsPolicyRegistry.cs` | Table + live seed |
| Test | `tests/Avila.ProductionTests/RlsPolicyTests.cs` | `pg_class`/`pg_policy`/registry cross-check |

## 2. Denied-attempt audit

| Artifact | Path | Purpose |
|---|---|---|
| Writer | `src/Avila.Platform/Authorization/SecurityAuditWriter.cs` | Independent transaction |
| Authorization | `src/Avila.Platform/Authorization/AuthorizationService.cs` | Audits permission/scope/cross-tenant denials |
| DI | `src/Avila.Api/Tenancy/TenancyServiceCollectionExtensions.cs` | Registers writer/authorization |
| Tests | `tests/Avila.ProductionTests/SecurityAuditTests.cs` | Rollback persistence |

## 3. Documentation

| Document | Purpose |
|---|---|
| `docs/operations/rls-audit-and-registry.md` | Registry, denied-audit, new-table checklist, limits |

## 4. Evidence

| Artifact | Path |
|---|---|
| Execution log | `tasks/evidence/R1-REM-03/execution-log.md` |
| Verification | `tasks/evidence/R1-REM-03/verification.md` |
| Full tests | `tasks/evidence/R1-REM-03/raw/test-output.txt` |
| Link check | `tasks/evidence/R1-REM-03/raw/link-check.txt` |

## 5. Boundaries

- No hash chain/query/export (REM-10).
- No direct-psql RLS audit.
- No production migration.
- No G1 opening.
