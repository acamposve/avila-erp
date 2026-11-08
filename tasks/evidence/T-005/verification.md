# T-005 — Verification

**Task:** [T-005 — Contexto tenant, UoW y RLS](../../T-005-tenancy-uow.md)
**Status:** `implemented` — awaiting human review. Not `verified`.
**Date:** 2026-11-08

## 1. Acceptance criteria

| Criterio de T-005 | Evidencia | Resultado |
|---|---|---|
| Contexto validado de tenant y UoW compartida por petición/mensaje | `ITenantContext`, `TenantUnitOfWork`, `TenantUnitOfWorkMiddleware`; middleware tests | **Pass** |
| RLS en negocio y `platform` | Four business schemas plus `platform.tenant_settings` with forced RLS; `platform.tenants` is global and grant-protected | **Pass** |
| Roles de mínimo privilegio | `avila_app` no superuser/no bypass; no grant on `platform.tenants` | **Pass** |
| Backfill bajo RLS | `TenantSettingsBackfill` per tenant; two-tenant test | **Pass** |
| Límites entre módulos | `ModuleBoundaryTests` (8 assertions) | **Pass** |
| PostgreSQL real: acceso cruzado, pool, introspección, rollback inyectado | `TenancyHardeningTests` and middleware tests | **Pass** |

## 2. Hardening traceability

| Hardening test | Scenario | Production evidence | Result |
|---|---|---|---|
| T-04 | Injected failure / cross-module rollback | `Injected_failure_rolls_back_all_module_rows`; middleware rollback | **Pass** |
| T-07 | Pool reuse without residual tenant | `Pool_reuse_does_not_leave_tenant_context` | **Pass** |
| T-07b | PgBouncer transaction pooling | Not adopted (`D-07`); N/A | **N/A** |
| T-08 | Read without/with tenant context | `App_role_without_tenant_context_sees_no_rows`; cross-tenant tests | **Pass** |
| T-09 | Platform tenant isolation | `Platform_tenant_settings_are_isolated_and_backfill_is_per_tenant` | **Pass** |
| T-09b | RLS introspection | `Rls_introspection_covers_all_tenant_tables` | **Pass** |
| T-16 | Backfill with two tenants | Backfill test creates one setting per tenant; counts match | **Pass** |

## 3. Test coverage

| Test | What it proves |
|---|---|
| `Platform_tenant_settings_are_isolated_and_backfill_is_per_tenant` | Two tenants, per-tenant UoW, one row each, no cross-read |
| `App_role_cannot_read_global_platform_tenants_table` | Least privilege: `42501` on global table |
| `Pool_reuse_does_not_leave_tenant_context` | `MaxPoolSize=1`; GUC empty after commit; next tenant correct |
| `Injected_failure_rolls_back_all_module_rows` | Module write + exception → zero rows |
| `App_role_without_tenant_context_sees_no_rows` | RLS default-deny for companies and settings |
| `Rls_introspection_covers_all_tenant_tables` | RLS + force + policy on every `tenant_id` table; no bypass |
| `Middleware_commits_module_writes_for_resolved_tenant` | Request-scoped UoW commit |
| `Middleware_rolls_back_when_downstream_throws` | Request-scoped rollback |
| `Middleware_rejects_unresolved_tenant` | 401 before any DB access |
| `ModuleBoundaryTests` | No module implementation references another module implementation |

Production suite: **19/19**. Full suite: **38/38**.

## 4. Platform classification

| Table | Classification | Protection |
|---|---|---|
| `platform.tenants` | Global | No `tenant_id`; not granted to `avila_app`; unique name |
| `platform.tenant_settings` | Tenant-scoped | Forced RLS by `app.tenant_id`; granted to `avila_app` |

## 5. P1 status

T-005 implements and tests mechanisms for `P1-03`, `P1-05` and `P1-06`, but **does not close them**:

- `P1-03`: production UoW and rollback pass; hardening closure review pending.
- `P1-05`: pool and RLS pass; `T-07b` is N/A because PgBouncer is not adopted.
- `P1-06`: `platform.tenant_settings` is covered; the full platform inventory will grow with T-006/T-008/T-010.

No P1 is marked closed by this task.

## 6. What was not done

- No identity, login, MFA or membership (T-006).
- No business endpoint under `/api`; middleware is wired but not exposed.
- No PgBouncer.
- No production role provisioning beyond the Testcontainers fixture.
- No additional platform tables beyond `tenants` and `tenant_settings`.
- No data migration in a shared environment.

## 7. Evidence

- [Platform migration](../../../src/Avila.Platform/Migrations/20261108180733_Initial.cs)
- [TenantUnitOfWork](../../../src/Avila.Platform/Tenancy/TenantUnitOfWork.cs)
- [Tenant context](../../../src/Avila.Platform/Tenancy/ITenantContext.cs)
- [Middleware](../../../src/Avila.Api/Tenancy/TenantUnitOfWorkMiddleware.cs)
- [Platform DbContext](../../../src/Avila.Platform/Persistence/PlatformDbContext.cs)
- [Backfill](../../../src/Avila.Platform/Persistence/TenantSettingsBackfill.cs)
- [Tenancy tests](../../../tests/Avila.ProductionTests/TenancyHardeningTests.cs)
- [Middleware tests](../../../tests/Avila.ProductionTests/TenantUnitOfWorkMiddlewareTests.cs)
- [Execution log](execution-log.md)

## 8. Conclusion

T-005 meets its acceptance criteria for production tenancy, RLS, least privilege, per-tenant backfill, rollback and module boundaries. It is ready for human review. T-006 remains blocked until this task is verified and the identity-stack ADR is approved.
