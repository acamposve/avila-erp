# T-005 — Code map

## 1. Production code

| Artefacto | Ruta | Propósito |
|---|---|---|
| Tenant context | `src/Avila.Platform/Tenancy/ITenantContext.cs` | Scoped, validated-once tenant |
| UoW | `src/Avila.Platform/Tenancy/TenantUnitOfWork.cs` | Connection + transaction + `set_config` |
| UoW factory | `src/Avila.Platform/Tenancy/TenantUnitOfWorkFactory.cs` | Begin per request/message |
| Platform persistence | `src/Avila.Platform/Persistence/PlatformDbContext.cs` | Global tenants + tenant settings |
| Platform migration | `src/Avila.Platform/Migrations/20261108180733_Initial.cs` | RLS for `platform.tenant_settings` |
| Backfill | `src/Avila.Platform/Persistence/TenantSettingsBackfill.cs` | One UoW per tenant |
| Request middleware | `src/Avila.Api/Tenancy/TenantUnitOfWorkMiddleware.cs` | Commit/rollback per `/api` request |
| DI/application hook | `src/Avila.Api/Tenancy/TenancyServiceCollectionExtensions.cs` | Register UoW, context, backfill |
| Host wiring | `src/Avila.Api/Program.cs` | `AddAvilaTenancy`, `/api` hook |

## 2. Database

| Schema/table | Change |
|---|---|
| `platform.tenants` | Global; no `tenant_id`; unique name; not granted to `avila_app` |
| `platform.tenant_settings` | Tenant-scoped; forced RLS; unique `(tenant_id, key)` |
| `admin`, `inventory`, `sales`, `billing` | Existing forced RLS retained |

## 3. Tests

| Test | File |
|---|---|
| Platform isolation, pool, rollback, introspection, backfill | `tests/Avila.ProductionTests/TenancyHardeningTests.cs` |
| Request middleware commit/rollback/401 | `tests/Avila.ProductionTests/TenantUnitOfWorkMiddlewareTests.cs` |
| Module boundaries | `tests/Avila.ProductionTests/ModuleBoundaryTests.cs` |
| Fixture roles/platform migration | `tests/Avila.ProductionTests/ProductionFixture.cs` |

## 4. Documentation

| Documento | Change |
|---|---|
| `docs/architecture/multi-tenancy.md` | §4.4 platform/UoW/backfill implementation |
| `docs/tasks-validation.md` | T-005 implemented; T-001…T-004 verified |

## 5. Boundaries

- No T-006 identity.
- No business endpoint.
- No PgBouncer.
- No production role provisioning.
- No shared migration or deployment.
- No secret or PII.
