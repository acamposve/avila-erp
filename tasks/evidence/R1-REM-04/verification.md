# R1-REM-04 — Verification

**Task:** R1-REM-04 — Tenant context for background jobs/workers
**Status:** `implemented` — awaiting human review. Not `verified`.
**Date:** 2026-11-14

## 1. Acceptance criteria

| Criterio | Evidencia | Resultado |
|---|---|---|
| Rechaza tenant nulo/vacío en el boundary | `Guid.Empty` test | **Pass** |
| Rechaza job nulo | `ArgumentNullException` test | **Pass** |
| Limpia contexto ambiental antes de ejecutar | Job observa `null`; wrapper limpia síncronamente | **Pass** |
| Tenant explícito por job | UoW `TenantId` + `SET LOCAL` | **Pass** |
| A/B paralelos intercalados sin fuga | 24 jobs; 12 empresas por tenant | **Pass** |
| Fallo revierte todo | 0 empresas tras excepción | **Pass** |
| Suspensión | `TenantAccessGuard` aplicado | **Pass** |

## 2. Test coverage

| Test | What it proves |
|---|---|
| `Empty_tenant_and_null_job_are_rejected_at_boundary` | Boundary validation |
| `Ambient_context_is_cleared_before_job_and_after_completion` | No ambient inheritance |
| `Parallel_interleaved_jobs_keep_tenant_scoped_context` | Interleaved A/B isolation |
| `Failed_job_rolls_back_and_clears_ambient_context` | Rollback + clean context |

Production suite: **58/58** on this branch (base main 54 + 4). Full suite: **77/77**.
When REM-03 is also merged, the combined suite is expected to be **61 production / 80 total**.

## 3. Job contract

```text
RunAsync(tenantId, job)
  validate tenant/job
  TenantExecutionContext.Clear()
  UoW.Begin(tenantId) -> set_config
  guard.EnsureWritesAllowed
  job.Execute
  commit
  finally clear ambient
```

## 4. Open gaps

- No queue/worker host/retry/dead-letter.
- No read-only job contract.
- No DI registration for a background service.
- Pending review.

## 5. Evidence

- [TenantJobRunner](../../../src/Avila.Platform/Jobs/TenantJobRunner.cs)
- [Tests](../../../tests/Avila.ProductionTests/TenantJobRunnerTests.cs)
- [Operations doc](../../../docs/operations/tenant-job-runner.md)
- [execution-log.md](execution-log.md)

## 6. Conclusion

REM-04 meets its acceptance criteria: explicit tenant boundary, ambient cleanup, interleaved A/B isolation and rollback are tested. Ready for review; G1 remains closed.
