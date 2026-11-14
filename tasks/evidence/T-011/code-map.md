# T-011 — Code map

## 1. Lifecycle

| Artefacto | Ruta | Propósito |
|---|---|---|
| Lifecycle store | `src/Avila.Platform/Lifecycle/TenantLifecycleStore.cs` | Suspend/reactivate via platform role |
| Lifecycle table | `platform.tenant_lifecycle` | Status, plan, suspension metadata |
| Provisioning | `src/Avila.Platform/Persistence/PlatformTenantProvisioner.cs` | Creates active lifecycle row |

## 2. Guard, limits and flags

| Artefacto | Ruta | Propósito |
|---|---|---|
| Write guard | `src/Avila.Platform/Lifecycle/TenantLifecycleServices.cs` | Blocks writes when suspended |
| Usage limits | `src/Avila.Platform/Lifecycle/TenantLifecycleServices.cs` | Configure/consume counters |
| Feature flags | `src/Avila.Platform/Lifecycle/TenantLifecycleServices.cs` | Per-tenant flags |

## 3. Export

| Artefacto | Ruta | Propósito |
|---|---|---|
| Export service | `src/Avila.Application/TenantExportService.cs` | Tenant-scoped export wrapper |
| Admin export | `src/Modules/Administration/Avila.Modules.Administration/AdministrationModule.cs` | Companies/branches/parties |
| Export DTOs | `src/Modules/Administration/Avila.Modules.Administration.Contracts/AdministrationContracts.cs` | Public contract |

## 4. Tests

| Test | File |
|---|---|
| Lifecycle, limits, flags, export | `tests/Avila.ProductionTests/LifecycleExportTests.cs` |
| Provisioning lifecycle | `tests/Avila.ProductionTests/ProvisioningTests.cs` |
| Fixture grants/RLS | `tests/Avila.ProductionTests/ProductionFixture.cs` |

## 5. Documentation

| Documento | Change |
|---|---|
| `docs/architecture/security.md` | T-011 lifecycle/guard/export note |
| `docs/tasks-validation.md` | T-010 verified, T-011 implemented |
| `docs/project-execution-plan.md` | R1 status |

## 6. Boundaries

- No operator console/UI.
- No full database export.
- No commercial plan pricing.
- No module-wide guard integration.
- No automatic DB read-only suspension.
- No shared migration or deployment.
