# Telemetry and alerting

**Estado:** R1-REM-12 · **Fecha:** 2026-11-15 · **Dueño:** @acamposve

## 1. Metrics access

`GET /metrics` requires one of:

- request from loopback;
- `Authorization: Bearer <Observability:MetricsToken>`;
- remote IP in `Observability:MetricsAllowedNetworks` (exact IP list, comma separated).

Otherwise **401**. No token is committed; production provides it via secret manager. The allowlist is intentionally exact-string, not CIDR; R2 can add CIDR/gateway policy.

## 2. OpenTelemetry

- `OpenTelemetry.Extensions.Hosting`, ASP.NET Core instrumentation and OTLP exporter.
- Service resource `avila-api`.
- Traces exclude `/health` and `/metrics`.
- ASP.NET instrumentation records route/status, not query strings, bodies or tenant data.
- Custom metrics carry no tenant/user labels.
- OTLP endpoint configured by standard `OTEL_EXPORTER_OTLP_ENDPOINT`; no endpoint hardcoded.

## 3. Prometheus rules

`ops/monitoring/prometheus/alerts.yml`:

| Alert | Expression/for |
|---|---|
| `AvilaHigh5xx` | 5xx rate >1/s for 5m |
| `AvilaLatencyHigh` | average >2s for 10m |
| `AvilaReadinessDown` | `up{job="avila-api"}==0` for 3m |
| `AvilaBackupStale` | no backup timestamp update for 24h |

Unit tests: `alerts_test.yml`, executed by **promtool test rules**. The script also validates Alertmanager config with **amtool check-config**.

## 4. Drill

`scripts/ci/alertmanager-drill.ps1`:

1. Starts a disposable Alertmanager container.
2. Adds a synthetic `DrillTest` alert via `amtool`.
3. Queries it back.
4. Removes the container.

No production alertmanager or webhook is configured.

## 5. Límites

- Metrics allowlist is exact IP, not CIDR.
- OTLP collector is not provisioned in the repo.
- Alert rules assume metric names exported by `/metrics`.
- Alertmanager receiver is a localhost webhook placeholder; no paging.
- No PII in traces/logs/metrics; audit remains in `audit_events`.
