# R1-REM-12 — Code map

## 1. Application

| Artifact | Path | Purpose |
|---|---|---|
| Metrics auth | `src/Avila.Api/Observability/MetricsEndpoint.cs` | Loopback/token/IP gate |
| OTel wiring | `src/Avila.Api/Program.cs` | Traces/metrics OTLP |
| Metrics tests | `tests/Avila.ProductionTests/MetricsAuthTests.cs` | 4 auth scenarios |

## 2. Monitoring

| Artifact | Path | Purpose |
|---|---|---|
| Alert rules | `ops/monitoring/prometheus/alerts.yml` | 5xx/latency/readiness/backup |
| Promtool tests | `ops/monitoring/prometheus/alerts_test.yml` | 4 alert tests |
| Alertmanager | `ops/monitoring/alertmanager/alertmanager.yml` | Route/receiver |
| Checks | `scripts/ci/check-monitoring.ps1` | promtool + amtool |
| Drill | `scripts/ci/alertmanager-drill.ps1` | Add/query alert |

## 3. Docs/evidence

| Artifact | Path |
|---|---|
| Operations | `docs/operations/telemetry-and-alerts.md` |
| Execution | `tasks/evidence/R1-REM-12/execution-log.md` |
| Verification | `tasks/evidence/R1-REM-12/verification.md` |
| Raw tests | `tasks/evidence/R1-REM-12/raw/test-output.txt` |

## 4. Boundaries

- No collector/dashboards/paging.
- Exact-IP allowlist only.
- No production deployment.
- No G1 opening.
