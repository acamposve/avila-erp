# T-007 — Code map

## 1. Provisioning

| Artefacto | Ruta | Propósito |
|---|---|---|
| Tenant provisioning | `src/Avila.Application/TenantProvisioningService.cs` | Organization → company → branch → preference → admin invite |
| Platform tenant provisioner | `src/Avila.Platform/Persistence/PlatformTenantProvisioner.cs` | Create global tenant with `avila_platform` |
| Administration module | `src/Modules/Administration/Avila.Modules.Administration/AdministrationModule.cs` | Company, branch, party |
| Administration contracts | `src/Modules/Administration/Avila.Modules.Administration.Contracts/AdministrationContracts.cs` | Commands/DTOs |
| Administration DbContext | `src/Modules/Administration/Avila.Modules.Administration/AdministrationDbContext.cs` | Company fiscal fields + branch |
| Migration | `src/Modules/Administration/Avila.Modules.Administration/Migrations/20261114193205_AddBranchesAndFiscalProfile.cs` | Branch table/RLS + fiscal columns |

## 2. Fiscal gate

| Artefacto | Ruta | Propósito |
|---|---|---|
| Sales flow | `src/Avila.Application/SalesFlowService.cs` | Passes company fiscal state |
| Billing contract | `src/Modules/Billing/Avila.Modules.Billing.Contracts/BillingContracts.cs` | `CompanyFiscalProfileComplete` |
| Billing module | `src/Modules/Billing/Avila.Modules.Billing/BillingModule.cs` | Rejects incomplete fiscal profile |

## 3. Database

| Schema/table | Change |
|---|---|
| `admin.companies` | `fiscal_address`, `fiscal_profile_complete` |
| `admin.branches` | New tenant table; forced RLS; unique `(tenant, company, code)` |
| `platform.tenants` | Created by `avila_platform`; app role remains denied |
| `platform.tenant_settings` | Initial `locale=es-VE` preference |

## 4. Tests

| Test | File |
|---|---|
| Two-org provisioning, admin invite, fiscal gate | `tests/Avila.ProductionTests/ProvisioningTests.cs` |
| Fixture roles/migrations | `tests/Avila.ProductionTests/ProductionFixture.cs` |

## 5. Documentation

| Documento | Change |
|---|---|
| `docs/domain/administration.md` | T-007 implementation note |
| `docs/tasks-validation.md` | T-006 verified, T-007 implemented |
| `docs/project-execution-plan.md` | Phase 0 status |

## 6. Boundaries

- No T-008 RBAC/audit.
- No HTTP provisioning endpoint.
- No email delivery.
- No branch/fiscal update.
- No production role/key provisioning.
- No shared migration or deployment.
