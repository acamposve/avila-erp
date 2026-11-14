# ADR-013 — Topología, operación y telemetría

- **Estado:** **Aceptado** — 2026-11-14.
- **Decisores:** @acamposve (Arquitectura, Operaciones, Seguridad).
- **Complementa:** ADR-002, DEC-001, DEC-008.
- **Alcance:** R1 operativo; no autoriza despliegue.

## 1. Topología

- Proveedor: **DigitalOcean**, aprovisionado con **Terraform**.
- Región primaria baseline: **`nyc3`**, una sola región activa.
- Sin multi-región activo-activo en R1. La aplicación y PostgreSQL primario residen en la misma región.
- **Read-replica readiness:** el esquema y la configuración deben permitir una réplica de lectura futura; no se aprovisiona ni se usa para escritura en R1.
- Prueba de latencia desde Venezuela antes de producción; si no cumple el objetivo, se requiere una nueva decisión de región, no un cambio silencioso.
- Backups y retención siguen ADR-011 y `DEC-008`.

## 2. Telemetría

- **OpenTelemetry** para trazas, métricas y logs; exportador OTLP configurable.
- **Prometheus** para métricas y **Alertmanager** para alertas; `/metrics` solo en red interna/allowlist.
- Correlación obligatoria: `X-Correlation-ID`/trace ID en logs, métricas y auditoría.
- Prohibido usar `tenant_id`, usuario, email o ruta como etiqueta de métrica.
- Logs sin bodies, tokens, PII ni secretos; excepciones solo tipo/correlación.
- Retención de telemetría separada de auditoría; no se usa telemetría como registro legal.

## 3. Alertas mínimas

| Señal | Condición |
|---|---|
| Readiness | falla 3 minutos |
| 5xx | >5 % durante 5 minutos |
| Latencia | promedio >2 s durante 10 minutos |
| Backup | sin éxito en 24 horas |
| Restore | sin simulacro en 90 días |

Los umbrales son baseline operativo; cambios requieren revisión de Operaciones.

## 4. Operación y recuperación

- RPO/RTO se miden en el simulacro autorizado; baseline provisional: RPO 24 h, RTO 4 h.
- Restore siempre en entorno aislado; nunca sobre producción sin autorización.
- Migraciones y backfills bajo RLS siguen el runbook.
- Ningún agente de IA despliega, rota credenciales ni toca infraestructura compartida.

## 5. Consecuencias

- `DEC-001` queda resuelto en topología/región baseline; la prueba de latencia es un gate de despliegue.
- `DEC-008` queda con retención baseline en ADR-011.
- La telemetría añade una dependencia OpenTelemetry aprobada por este ADR.
- Sin réplica activa en R1; el diseño no promete alta disponibilidad multi-región.

## 6. Referencias

- [Operación](../operations/observability.md)
- [Runbook](../operations/runbook.md)
- [ADR-002](ADR-002-postgresql.md)
- [ADR-011](ADR-011-immutable-audit.md)
