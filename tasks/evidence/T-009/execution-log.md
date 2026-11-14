# T-009 — Execution log

- **Fecha:** 2026-11-14
- **Tarea:** [T-009 — Numeración e idempotencia transaccional](../../T-009-numbering-idempotency.md)
- **Preparado por:** agente de IA
- **Responsable/aprobador:** @acamposve
- **Rama:** `feature/t-009-numbering-idempotency`
- **Commit base:** `e0302c7` (`Merge pull request #11`)
- **Commit de evidencia:** `f6f1891`
- **PR:** [#12](https://github.com/acamposve/avila-erp/pull/12)
- **Entorno:** local Windows + PostgreSQL 18 Testcontainers
- **Estado:** `implemented` — pendiente de revisión humana; **no** `verified`

## Comandos ejecutados

| # | Comando | Resultado | Evidencia |
|---|---|---|---|
| 1 | `dotnet test Avila.slnx --logger "console;verbosity=minimal"` | **61/61 passed** (19 spike + 42 producción) | [`raw/test-output.txt`](raw/test-output.txt) |
| 2 | `GET http://127.0.0.1:5099/health` | HTTP 200 `Healthy` | [`raw/api-health.txt`](raw/api-health.txt) |
| 3 | Migración `AddDocumentNumberingAndIdempotency` | `document_sequences`, `idempotency_keys` con RLS | [`src/Avila.Platform/Migrations/20261114200618_AddDocumentNumberingAndIdempotency.cs`](../../../src/Avila.Platform/Migrations/20261114200618_AddDocumentNumberingAndIdempotency.cs) |
| 4 | Pruebas de numeración/idempotencia | 7/7 | [`raw/test-output.txt`](raw/test-output.txt) |
| 5 | Comprobación de enlaces Markdown | 0 enlaces roto | Reporte del PR |

## Resultado

- Secuencias por tenant + `scope_type`/`scope_id` + tipo de documento.
- Cotizaciones y pedidos usan `Q-00000001`/`O-00000001`; se eliminaron los marcadores GUID.
- 10.000 emisiones con concurrencia acotada (64 workers) son únicas y sin huecos.
- Rollback no consume número.
- Idempotency-Key persistida en la misma transacción; misma clave + mismo cuerpo replay; misma clave + cuerpo distinto → `IDEMPOTENCY_KEY_REUSED`.
- Dos solicitudes concurrentes con la misma clave producen un solo efecto.
- T-009 queda `implemented`; no se declara ningún P1 cerrado.

## Bloqueos registrados

| Bloqueo | Dueño | Estado |
|---|---|---|
| Numeración fiscal definitiva y series administrables | T-009/T-019 | Fuera de alcance; solo cotización/pedido |
| `Idempotency-Key` en endpoints HTTP | T-012/T-006 | Servicio existe; no endpoint de negocio |
| Limpieza/retención de claves de idempotencia | Operaciones | Pendiente |
| 10.000 simultáneos reales vs. 64 workers | @acamposve | Prueba de carga acotada; sin tuning de pool |
| Idempotencia fuera de `sales-flow` | T-016/T-019/T-020 | Pendiente |
| Aprobación de este PR | @acamposve | Pendiente |

## Nota de alcance

No se implementa emisión fiscal, series administrables ni idempotencia global de todos los comandos. La prueba de 10.000 usa concurrencia acotada para no saturar el pool de Testcontainers; mide unicidad y ausencia de huecos.
