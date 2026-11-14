# T-012 — Code map

## 1. Observability

| Artefacto | Ruta | Propósito |
|---|---|---|
| Metrics registry | `src/Avila.Api/Observability/Observability.cs` | Counters/status/duration |
| Request middleware | `src/Avila.Api/Observability/Observability.cs` | Correlation and access log |
| DB health check | `src/Avila.Api/Observability/DatabaseHealthCheck.cs` | Readiness probe |
| API wiring | `src/Avila.Api/Program.cs` | `/health/*`, `/metrics`, exception handler |

## 2. CI and restore

| Artefacto | Ruta | Propósito |
|---|---|---|
| CI | `.github/workflows/ci.yml` | Build, tests, clean migrations |
| Restore rehearsal | `scripts/backup-restore-rehearsal.ps1` | Disposable pg_dump/pg_restore + RLS check |
| Rehearsal evidence | `tasks/evidence/T-012/raw/restore-rehearsal.txt` | PASS output |

## 3. Tests

| Test | File |
|---|---|
| Metrics, correlation, health | `tests/Avila.ProductionTests/ObservabilityTests.cs` |
| API smoke | `tasks/evidence/T-012/raw/api-observability.txt` |

## 4. Documentation

| Documento | Change |
|---|---|
| `docs/operations/observability.md` | Endpoints, logs, metrics, alerts, RPO/RTO |
| `docs/operations/runbook.md` | Triage, roles, migration/backfill, restore |
| `docs/operations/backup-restore.md` | Rehearsal scope and limits |
| `docs/architecture/security.md` | T-012 logging/metrics note |
| `docs/tasks-validation.md` | T-011 verified, T-012 implemented |

## 5. Boundaries

- No deployment, shared infrastructure or production restore.
- No real alerts/dashboards.
- No OpenTelemetry exporter.
- No `/metrics` auth.
- No CI security scanner.
- No full-schema/PITR rehearsal.
