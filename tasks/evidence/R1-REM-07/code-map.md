# R1-REM-07 — Code map

## 1. Endpoint

| Artifact | Path | Purpose |
|---|---|---|
| Handler | `src/Avila.Api/Exporting/TenantExportEndpoint.cs` | Auth, tenant derivation, NDJSON, audit |
| Route | `src/Avila.Api/Program.cs` | `/api/v1/tenant/export` |
| Export audit | `src/Avila.Platform/Exporting/ExportAuditWriter.cs` | Independent requested/completed/failed |
| Streaming registry | `src/Avila.Platform/Exporting/TenantExportRegistry.cs` | NDJSON + hashes |

## 2. Contributors (REM-06)

| Artifact | Path |
|---|---|
| Administration | `src/Modules/Administration/Avila.Modules.Administration/AdministrationExportContributor.cs` |
| Audit | `src/Avila.Platform/Exporting/AuditExportContributor.cs` |
| Identity | `src/Avila.Platform/Exporting/IdentityExportContributor.cs` |

## 3. Tests

| Test | Path |
|---|---|
| Endpoint auth/stream/audit/suspension | `tests/Avila.ProductionTests/ExportEndpointTests.cs` |
| Deterministic streaming | `tests/Avila.ProductionTests/TenantExportStreamingTests.cs` |

## 4. Evidence

| Artifact | Path |
|---|---|
| Execution log | `tasks/evidence/R1-REM-07/execution-log.md` |
| Verification | `tasks/evidence/R1-REM-07/verification.md` |
| Full tests | `tasks/evidence/R1-REM-07/raw/test-output.txt` |
| Link check | `tasks/evidence/R1-REM-07/raw/link-check.txt` |

## 5. Boundaries

- No full HTTP/login E2E; handler tested with real DB and HttpContext.
- No quota/cursor/zip/detached signature/async download.
- Stacked on REM-06.
- No G1 opening.
