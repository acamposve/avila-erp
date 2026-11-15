# R1-REM-06 — Code map

## 1. Contributors

| Artifact | Path | Purpose |
|---|---|---|
| Administration | `src/Modules/Administration/Avila.Modules.Administration/AdministrationExportContributor.cs` | Companies/branches/parties |
| Audit | `src/Avila.Platform/Exporting/AuditExportContributor.cs` | Tenant audit events |
| Identity | `src/Avila.Platform/Exporting/IdentityExportContributor.cs` | Safe memberships/invitations via `avila_identity` |
| Composition | `src/Avila.Api/Program.cs` | Registers contributors/registry |

## 2. Tests

| Test | Path |
|---|---|
| A/B + credential absence | `tests/Avila.ProductionTests/ExportContributorTests.cs` |
| Role enforcement | same |
| Stable hash | same |

## 3. Evidence

| Artifact | Path |
|---|---|
| Execution log | `tasks/evidence/R1-REM-06/execution-log.md` |
| Verification | `tasks/evidence/R1-REM-06/verification.md` |
| Full tests | `tasks/evidence/R1-REM-06/raw/test-output.txt` |
| Link check | `tasks/evidence/R1-REM-06/raw/link-check.txt` |

## 4. Boundaries

- No endpoint/auth/streaming (REM-07).
- No credentials/tokens/session export.
- No G1 opening.
