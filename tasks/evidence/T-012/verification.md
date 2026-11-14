# T-012 — Verification

**Task:** [T-012 — Operación, CI y recuperación](../../T-012-operations.md)
**Status:** `implemented` — awaiting human review. Not `verified`.
**Date:** 2026-11-14

## 1. Acceptance criteria

| Criterio de T-012 | Evidencia | Resultado |
|---|---|---|
| Observabilidad sin secretos/PII | Correlation/log fields; no query/body/PII; exception type only | **Pass** |
| Health checks | `/health`, `/health/live`, `/health/ready`; DB check | **Pass** |
| Métricas | `/metrics` counters/status/duration | **Pass** |
| Alertas/runbooks | Alert table + runbook documented | **Pass (documented, not configured)** |
| CI build/pruebas/seguridad/migraciones | Existing CI: build + tests + clean migrations; security scan not present | **Partial** |
| Restauración reproducible | Disposable PostgreSQL dump/restore + RLS verification | **Pass (local synthetic)** |
| RPO/RTO/roles/límites | Provisional table + roles/limits in runbook | **Pass (provisional)** |
| Migración/backfill bajo RLS | Procedure documented | **Pass (documented, not executed)** |

## 2. Test coverage

| Test | What it proves |
|---|---|
| `Metrics_count_requests_errors_and_status_codes` | Counters and rendered format |
| `Correlation_id_is_propagated_and_invalid_values_are_replaced` | Header propagation and sanitization |
| `Database_health_check_reports_reachable_and_missing_configuration` | Healthy/DB and Degraded/no config |

Production suite: **53/53**. Full suite: **72/72**.

## 3. Evidence

| Evidence | Result |
|---|---|
| API smoke `/health`, `/health/live`, `/health/ready`, `/metrics` | 200; correlation header |
| Restore rehearsal | PASS; total=2, tenant A=1, tenant B=1 |
| CI baseline | main `4042d9c` success; PR #14 success |
| Docs | observability, runbook, backup/restore |

## 4. RPO/RTO

| Objective | Value | Status |
|---|---|---|
| RPO | 24 h | Provisional, requires approval |
| RTO | 4 h | Provisional, requires approval |
| Retention | `DEC-008` | Not defined |

## 5. Open gaps

- No real alerts/dashboards or alertmanager.
- No OpenTelemetry exporter/distributed tracing.
- `/metrics` has no auth/allowlist; must be network-restricted.
- No CI security scan (SAST/dependency audit beyond restore/build).
- No production/shared-environment backup or restore; local synthetic only.
- No full-schema or PITR/WAL rehearsal.
- No real RPO/RTO approval or retention.
- No migration/backfill execution.
- No P1 closed.

## 6. Conclusion

T-012 meets the R1 operations scope at the local/documentation level: health, metrics, correlation logging, CI baseline, runbook, and a reproducible disposable restore rehearsal are in place. Alerts, tracing, security scanning and authorized production restore remain open; it is ready for human review.
