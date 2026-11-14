# T-011 — Verification

**Task:** [T-011 — Planes, ciclo de vida y exportación](../../T-011-platform-lifecycle.md)
**Status:** `implemented` — awaiting human review. Not `verified`.
**Date:** 2026-11-14

## 1. Acceptance criteria

| Criterio de T-011 | Evidencia | Resultado |
|---|---|---|
| Límites por plan/tenant | `users` limit=2; third consume rejected | **Pass** |
| Bloqueo de escrituras suspendido | `TenantSuspendedException` after suspend | **Pass** |
| Lectura/exportación permitida suspendido | Export succeeds while suspended | **Pass** |
| Reactivación | `active` restores write guard | **Pass** |
| Exportación sin cruce de tenants | A exports only A; B exports only B | **Pass** |
| Feature flags por tenant | A flag true; B flag false | **Pass** |

## 2. Test coverage

| Test | What it proves |
|---|---|
| `Suspended_tenant_blocks_writes_but_allows_export_and_reactivation` | Lifecycle, guard, export, reactivation |
| `Limits_and_feature_flags_are_tenant_scoped` | Limit enforcement and tenant isolation |
| `Export_is_tenant_scoped_and_cannot_include_another_tenant` | RLS-scoped export |

Production suite: **50/50**. Full suite: **69/69**.

## 3. Lifecycle

| Property | Implementation |
|---|---|
| Table | `platform.tenant_lifecycle` |
| Status | `active` / `suspended` |
| Plan | `plan_code` (no commercial price) |
| Suspension | Timestamp and reason |
| Write guard | `TenantAccessGuard` throws on suspended |
| Export | Allowed while suspended |
| Provisioning | Creates active lifecycle row |

## 4. Limits, flags and export

| Feature | Implementation |
|---|---|
| Limits | `platform.usage_counters`; `TenantUsageService` |
| Flags | `platform.feature_flags`; `TenantFeatureService` |
| Export | `TenantExportService` + `IAdministrationModule.ExportAsync` |
| Isolation | RLS; export has no target-tenant parameter |

## 5. Open gaps

- The write guard is not yet called by every module command.
- Suspension is service-level, not automatic database `READ ONLY`.
- Export only covers companies, branches and parties.
- No operator console/HTTP endpoint.
- No commercial plan pricing; limits are synthetic.
- Usage counter increments are not row-locked under concurrency.
- No P1 closed.

## 6. Evidence

- [Lifecycle store](../../../src/Avila.Platform/Lifecycle/TenantLifecycleStore.cs)
- [Lifecycle/guard/limits/flags](../../../src/Avila.Platform/Lifecycle/TenantLifecycleServices.cs)
- [Export service](../../../src/Avila.Application/TenantExportService.cs)
- [Migration](../../../src/Avila.Platform/Migrations/20261114203657_AddTenantLifecycleLimitsFlags.cs)
- [Tests](../../../tests/Avila.ProductionTests/LifecycleExportTests.cs)
- [execution-log.md](execution-log.md)

## 7. Conclusion

T-011 meets its acceptance criteria at the service/database level: lifecycle suspension/reactivation, write blocking, read/export during suspension, tenant-scoped limits/flags, and RLS-limited export are tested. It is ready for human review; full export, module-wide guard integration and operator UI remain open.
