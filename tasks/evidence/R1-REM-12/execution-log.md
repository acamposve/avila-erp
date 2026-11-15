# R1-REM-12 — Execution log

- **Fecha:** 2026-11-15
- **Tarea:** R1-REM-12 — Metrics auth, OpenTelemetry and alert rules
- **Preparado por:** agente de IA
- **Responsable/aprobador:** @acamposve
- **Rama:** `feat/r1-rem-12-observability`
- **Commit base:** `6a13bdc`
- **Commit de evidencia:** `e426cd7`
- **PR:** [#29](https://github.com/acamposve/avila-erp/pull/29)
- **Estado:** `implemented` — pendiente de revisión humana; **no** `verified`

## Comandos ejecutados

| # | Comando | Resultado | Evidencia |
|---|---|---|---|
| 1 | `dotnet test Avila.slnx` | **113/113 passed** (19 spike + 94 producción) | [`raw/test-output.txt`](raw/test-output.txt) |
| 2 | `check-monitoring.ps1` | promtool rules PASS + amtool config SUCCESS | [`raw/monitoring-check.txt`](raw/monitoring-check.txt) |
| 3 | `alertmanager-drill.ps1` | Alertmanager drill **PASS** | [`raw/alertmanager-drill.txt`](raw/alertmanager-drill.txt) |
| 4 | Metric auth tests | 4/4 | [`raw/test-output.txt`](raw/test-output.txt) |
| 5 | Comprobación de enlaces Markdown | 0 enlaces roto | Reporte del PR |

## Resultado

- `/metrics`:
  - loopback permitido;
  - token Bearer permitido;
  - allowlist exacta permitida;
  - remoto sin credencial → 401.
- OpenTelemetry ASP.NET Core traces/metrics con OTLP; health/metrics excluidos.
- Reglas Prometheus de 5xx, latencia, readiness y backup.
- `promtool test rules` valida las 4 alertas.
- `amtool check-config` valida Alertmanager.
- Drill real: alta/consulta de alerta en Alertmanager desechable.
- T-013 no se re-ejecuta; G1 sigue cerrado.

## Bloqueos / pendientes

| Pendiente | Dueño | Estado |
|---|---|---|
| OTLP collector real | Operaciones | No aprovisionado |
| Allowlist CIDR/gateway | R2+ | IP exacta hoy |
| Paging/on-call real | Operaciones | Webhook placeholder |
| Dashboard Grafana | R2+ | No incluido |
| Aprobación de este PR | @acamposve | Pendiente |

## Nota de alcance

No se despliega Prometheus/Alertmanager ni un collector OTLP. Las pruebas de reglas y el drill son locales/desechables.
