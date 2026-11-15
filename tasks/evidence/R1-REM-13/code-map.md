# R1-REM-13 — Code map

| Artifact | Path | Purpose |
|---|---|---|
| Restore test | `tests/Avila.ProductionTests/FullRestoreRehearsalTests.cs` | pg_dump/pg_restore + RLS/count/chain |
| Fixture container | `tests/Avila.ProductionTests/ProductionFixture.cs` | Exposes Testcontainers container |
| Operations | `docs/operations/full-restore-rehearsal.md` | Scope/RPO/RTO/limits |

## Evidence

- `tasks/evidence/R1-REM-13/execution-log.md`
- `tasks/evidence/R1-REM-13/verification.md`
- `tasks/evidence/R1-REM-13/raw/test-output.txt`

## Boundaries

- No production restore.
- No PITR/WAL.
- No role/cluster restore.
- No G1 opening.
