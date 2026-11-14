# R1-REM-04 — Code map

## 1. Runner

| Artifact | Path | Purpose |
|---|---|---|
| Job contracts/runner | `src/Avila.Platform/Jobs/TenantJobRunner.cs` | Explicit tenant, ambient cleanup, UoW |
| Guard | `src/Avila.Platform/Lifecycle/TenantLifecycleServices.cs` | Suspension check |
| Tests | `tests/Avila.ProductionTests/TenantJobRunnerTests.cs` | Boundary/parallel/rollback |

## 2. Documentation

| Document | Purpose |
|---|---|
| `docs/operations/tenant-job-runner.md` | Contract, lifecycle, limits |

## 3. Evidence

| Artifact | Path |
|---|---|
| Execution log | `tasks/evidence/R1-REM-04/execution-log.md` |
| Verification | `tasks/evidence/R1-REM-04/verification.md` |
| Full tests | `tasks/evidence/R1-REM-04/raw/test-output.txt` |
| Link check | `tasks/evidence/R1-REM-04/raw/link-check.txt` |

## 4. Boundaries

- No queue/hosted worker/retries.
- No read-only jobs.
- No production deploy.
- No G1 opening.
