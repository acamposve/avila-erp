# R1-CLOSURE-B — Code map

| Artifact | Path | Purpose |
|---|---|---|
| Fiscal entities | `src/Avila.Platform/Persistence/PlatformDbContext.cs` | Rule versions/snapshots |
| Fiscal service | `src/Avila.Platform/Fiscal/FiscalVersionService.cs` | Publish/select/snapshot/audit |
| Migration | `src/Avila.Platform/Migrations/20261115115048_AddFiscalVersionFoundation.cs` | Tables/RLS/triggers/registry |
| Job telemetry | `src/Avila.Platform/Jobs/JobTelemetry.cs` | `ActivitySource("Avila.Jobs")` |
| Job runner | `src/Avila.Platform/Jobs/TenantJobRunner.cs` | Parent propagation/status |
| OTel wiring | `src/Avila.Api/Program.cs` | AddSource |
| Grants | `ops/postgres/roles.sql` | App role fiscal grants |
| Tests | `tests/Avila.ProductionTests/FiscalTraceTests.cs` | Fiscal/trace |
| Final evidence | `tasks/evidence/R1-CLOSURE-B/` | Delta/sign-off |

## Boundaries

- No tax values or document emission.
- No R2/deployment/production restore.
- G1 remains closed.
