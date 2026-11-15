# R1-CLOSURE-A — Code map

| Artifact | Path | Purpose |
|---|---|---|
| Contracts | `src/Modules/Administration/Avila.Modules.Administration.Contracts/AdministrationContracts.cs` | Update commands, Version, get |
| Module | `src/Modules/Administration/Avila.Modules.Administration/AdministrationModule.cs` | Update/get/validation/concurrency |
| Entities | `src/Modules/Administration/Avila.Modules.Administration/AdministrationDbContext.cs` | Version columns/tokens |
| Migration | `src/Modules/Administration/Avila.Modules.Administration/Migrations/*AddAdministrationVersion.cs` | version columns |
| Endpoints | `src/Avila.Api/Administration/AdministrationEndpoints.cs` | Company/branch/party writes |
| Guard | `src/Avila.Api/Tenancy/TenantWriteGuardMiddleware.cs` | 423 before handlers |
| Composition | `src/Avila.Api/Program.cs` | Guard + routes + DI |
| Permissions | `src/Avila.Platform/Authorization/AuthorizationService.cs` | Manage permissions/audit |
| Tests | `tests/Avila.ProductionTests/AdministrationHttpE2eTests.cs` | Full HTTP E2E |
| Decision | `docs/decisions/DEC-021-r1-scope-exceptions.md` | Deferred stock-adjust/pre-cloud |

## Evidence

- `tasks/evidence/R1-CLOSURE-A/execution-log.md`
- `tasks/evidence/R1-CLOSURE-A/verification.md`
- `tasks/evidence/R1-CLOSURE-A/raw/test-output.txt`

## Boundaries

- No R2/inventory work.
- No deployment/shared migrations.
- G1 remains closed.
