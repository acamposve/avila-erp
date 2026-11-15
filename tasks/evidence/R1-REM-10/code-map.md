# R1-REM-10 — Code map

## 1. Audit integrity

| Artifact | Path | Purpose |
|---|---|---|
| Hash | `src/Avila.Platform/Authorization/AuditHash.cs` | Canonical SHA-256 |
| Writer | `src/Avila.Platform/Authorization/AuditService.cs` | Advisory lock + chain |
| Verify/query/export | `src/Avila.Platform/Authorization/AuditIntegrityService.cs` | Verification, pagination, NDJSON |
| Migration | `src/Avila.Platform/Migrations/20261115103211_AddAuditHashChain.cs` | Columns + legacy v0 |

## 2. API

| Artifact | Path | Purpose |
|---|---|---|
| Endpoints | `src/Avila.Api/Audit/AuditEndpoints.cs` | Query/export/verify + audit-of-audit |
| Composition | `src/Avila.Api/Program.cs` | Routes/services |
| Permission | `src/Avila.Platform/Authorization/AuthorizationService.cs` | `administration.audit.read` |

## 3. Tests/docs

| Artifact | Path |
|---|---|
| Chain/pagination tests | `tests/Avila.ProductionTests/AuditIntegrityTests.cs` |
| Endpoint audit-of-audit | `tests/Avila.ProductionTests/AuditEndpointTests.cs` |
| Operations | `docs/operations/audit-integrity.md` |

## 4. Evidence

- `tasks/evidence/R1-REM-10/execution-log.md`
- `tasks/evidence/R1-REM-10/verification.md`
- `tasks/evidence/R1-REM-10/raw/test-output.txt`

## 5. Boundaries

- No legacy backfill/asymmetric anchor.
- No full HTTP login E2E for audit.
- No purge/legal hold.
- No G1 opening.
