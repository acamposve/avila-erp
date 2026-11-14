# Observabilidad y salud — ÁVILA ERP

**Estado:** implementación R1/T-012 · **Responsable:** @acamposve (pendiente de ratificar umbrales)
**Fecha:** 2026-11-14

## 1. Endpoints

| Endpoint | Uso | Comportamiento |
|---|---|---|
| `GET /health` | Liveness legado | 200 si el proceso responde; no consulta la base |
| `GET /health/live` | Liveness | 200 si el proceso responde |
| `GET /health/ready` | Readiness | Consulta `SELECT 1`; 503 si la base no responde; 200/degradado si falta configuración |
| `GET /metrics` | Métricas internas | Texto tipo Prometheus; **restringir por red** |
| `X-Correlation-ID` | Correlación | Se propaga si es válido; si no, se genera uno nuevo |

> `/metrics` no tiene autenticación en R1. Debe exponerse solo en la red interna o detrás del proxy con allowlist. Añadir auth/allowlist es trabajo pendiente antes de producción.

## 2. Señales

### Logs

Campos mínimos por solicitud:

- `CorrelationId` (`X-Correlation-ID` / `TraceIdentifier`);
- método HTTP;
- path **sin query string**;
- status;
- duración en ms.

Reglas:

- No registrar headers, bodies, tokens, contraseñas, documentos, salarios, datos bancarios ni PII.
- Las excepciones no controladas registran solo el **tipo**; el detalle se devuelve como `ProblemDetails` genérico.
- El `tenant_id` no se usa como etiqueta de métrica ni se registra en logs de acceso.

### Métricas

| Métrica | Tipo | Uso |
|---|---|---|
| `avila_http_requests_total` | counter | Volumen total |
| `avila_http_errors_total` | counter | Respuestas 5xx |
| `avila_http_request_duration_ms` | gauge | Promedio desde el arranque |
| `avila_http_responses_total{status}` | counter | Distribución por status |

No incluyen tenant, usuario, ruta ni PII: evita cardinalidad y filtración.

## 3. Alertas propuestas (provisional)

| Alerta | Condición | Severidad | Dueño |
|---|---|---|---|
| Readiness caída | `/health/ready` falla 3 min | P1 | Operaciones |
| Tasa 5xx | >5% durante 5 min | P1 | Operaciones |
| Latencia | promedio >2 s durante 10 min | P2 | Operaciones |
| Migración fallida | job/PR de migración en rojo | P1 | Plataforma |
| Respaldo fallido | job de backup sin éxito 24 h | P1 | Operaciones |
| Restore no probado | >90 días sin simulacro | P2 | Operaciones |

Los umbrales son **provisionales** y requieren ratificación de @acamposve; no se configuró ninguna alerta real.

## 4. RPO/RTO provisionales

| Objetivo | Valor provisional | Estado |
|---|---|---|
| RPO | 24 h | **Pendiente de aprobación** |
| RTO | 4 h | **Pendiente de aprobación** |
| Retención de backups | Pendiente `DEC-008` | **No definida** |

No se ejecutó backup/restore sobre un entorno compartido ni producción.

## 5. Limitaciones

- Sin OpenTelemetry/exportador de trazas; solo correlación y logs estructurados.
- Sin dashboard ni alertmanager conectados.
- `/metrics` sin autenticación.
- Sin métricas de base de datos, colas/outbox ni negocio.
- Sin SLO formal ni presupuesto de error.

## 6. Referencias

- [Runbook](runbook.md)
- [Simulacro de restauración](backup-restore.md)
- [Seguridad](../architecture/security.md)
- [Testing](../architecture/testing-strategy.md)
