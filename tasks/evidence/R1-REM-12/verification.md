# R1-REM-12 — Verification

**Task:** R1-REM-12 — Metrics auth, OpenTelemetry and alert rules
**Status:** `implemented` — awaiting human review. Not `verified`.
**Date:** 2026-11-15

## 1. Acceptance criteria

| Criterio | Evidencia | Resultado |
|---|---|---|
| Metrics strict auth/allowlist | Loopback/token/IP tests; remote 401 | **Pass** |
| OpenTelemetry traces | OTel packages + ASP.NET instrumentation; health/metrics filtered | **Pass** |
| Zero PII/sensitive tenant parameters | No query/body instrumentation; no tenant labels; filtered health | **Pass (design)** |
| Prometheus alert rules | 4 rules in `alerts.yml` | **Pass** |
| promtool unit tests | `promtool test rules` PASS | **Pass** |
| Alertmanager drills | Disposable Alertmanager + amtool add/query | **Pass** |
| No regression | 113/113 | **Pass** |

## 2. Test coverage

| Test | What it proves |
|---|---|
| `Metrics_allow_loopback_by_default` | Loopback |
| `Metrics_reject_remote_without_token_or_allowlist` | 401 |
| `Metrics_allow_remote_with_token` | Bearer auth |
| `Metrics_allow_exact_network_allowlist` | IP allowlist |
| `promtool test rules` | 4 alert expressions/annotations |
| `amtool check-config` | Alertmanager config |
| Alertmanager drill | Add/query synthetic alert |

Production suite: **94/94**. Full suite: **113/113**.

## 3. Configuration

| Key | Purpose |
|---|---|
| `Observability:MetricsToken` | Bearer token; secret manager |
| `Observability:MetricsAllowedNetworks` | Exact IP list |
| `OTEL_EXPORTER_OTLP_ENDPOINT` | Collector endpoint |

No token or collector secret is committed.

## 4. Open gaps

- Exact-IP allowlist only; no CIDR/gateway policy.
- No real OTLP collector/dashboards.
- Alertmanager receiver is a placeholder webhook; no paging.
- Trace sampling/export not load-tested.
- Metrics endpoint still returns aggregate only; no auth database.
- Pending review.

## 5. Evidence

- [Metrics endpoint](../../../src/Avila.Api/Observability/MetricsEndpoint.cs)
- [OpenTelemetry wiring](../../../src/Avila.Api/Program.cs)
- [Alert rules](../../../ops/monitoring/prometheus/alerts.yml)
- [Promtool tests](../../../ops/monitoring/prometheus/alerts_test.yml)
- [Alertmanager config](../../../ops/monitoring/alertmanager/alertmanager.yml)
- [Metrics tests](../../../tests/Avila.ProductionTests/MetricsAuthTests.cs)
- [Operations doc](../../../docs/operations/telemetry-and-alerts.md)
- [execution-log.md](execution-log.md)

## 6. Conclusion

REM-12 meets its acceptance criteria: metrics are authenticated/allowlisted, OTel is wired without PII labels/query capture, Prometheus rules are unit-tested with promtool, and Alertmanager config/drill pass. G1 remains closed.
