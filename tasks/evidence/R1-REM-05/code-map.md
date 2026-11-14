# R1-REM-05 — Code map

## 1. Contract and registry

| Artifact | Path | Purpose |
|---|---|---|
| Export types/registry | `src/Avila.Platform/Exporting/TenantExportRegistry.cs` | Contributor contract, canonicalization, hashes |
| Tests | `tests/Avila.ProductionTests/TenantExportRegistryTests.cs` | Determinism/duplicates/no module refs |
| Architecture doc | `docs/architecture/tenant-export-registry.md` | Contract/format/limits |

## 2. Evidence

| Artifact | Path |
|---|---|
| Execution log | `tasks/evidence/R1-REM-05/execution-log.md` |
| Verification | `tasks/evidence/R1-REM-05/verification.md` |
| Full tests | `tasks/evidence/R1-REM-05/raw/test-output.txt` |
| Link check | `tasks/evidence/R1-REM-05/raw/link-check.txt` |

## 3. Boundaries

- No domain contributors.
- No HTTP endpoint/auth/audit/streaming.
- No module references in orchestrator.
- No G1 opening.
