# T-010 — Execution log

- **Fecha:** 2026-11-14
- **Tarea:** [T-010 — Parámetros versionados y aprobaciones](../../T-010-configuration.md)
- **Preparado por:** agente de IA
- **Responsable/aprobador:** @acamposve
- **Rama:** `feature/t-010-configuration-approvals`
- **Commit base:** `14ee40b` (`Merge pull request #12`)
- **Commit de evidencia:** `3a45710`
- **PR:** [#13](https://github.com/acamposve/avila-erp/pull/13) — fusionado
- **Aprobación:** @acamposve, 2026-11-14
- **Merge commit:** `2f6353c`
- **Entorno:** local Windows + PostgreSQL 18 Testcontainers
- **Estado:** `verified` — aprobado y fusionado

## Comandos ejecutados

| # | Comando | Resultado | Evidencia |
|---|---|---|---|
| 1 | `dotnet test Avila.slnx --logger "console;verbosity=minimal"` | **66/66 passed** (19 spike + 47 producción) | [`raw/test-output.txt`](raw/test-output.txt) |
| 2 | `GET http://127.0.0.1:5099/health` | HTTP 200 `Healthy` | [`raw/api-health.txt`](raw/api-health.txt) |
| 3 | Migración `AddSettingVersionsAndApprovals` | `setting_versions`, `approval_rules`, `approval_requests` con RLS y trigger append-only | [`src/Avila.Platform/Migrations/20261114202006_AddSettingVersionsAndApprovals.cs`](../../../src/Avila.Platform/Migrations/20261114202006_AddSettingVersionsAndApprovals.cs) |
| 4 | Pruebas de configuración/aprobaciones | 5/5 | [`raw/test-output.txt`](raw/test-output.txt) |
| 5 | Comprobación de enlaces Markdown | 0 enlaces roto | Reporte del PR |

## Resultado

- Parámetros con `effective_from`/`effective_to`, fuente e historial inmutable.
- Selección de la versión vigente por fecha.
- Trigger y grants bloquean UPDATE/DELETE de `setting_versions`.
- Preferencias de organización actualizables y auditadas.
- Reglas de aprobación por tipo y monto.
- Una operación sobre el umbral sin aprobación lanza `ApprovalRequiredException`.
- El solicitante no puede aprobar su propia solicitud; un segundo owner sí.
- Cambios de parámetros y decisiones de aprobación quedan auditados.
- T-010 queda **`verified`**: aprobado y fusionado en PR #13 (`2f6353c`).

## Bloqueos registrados

| Bloqueo | Dueño | Estado |
|---|---|---|
| Endpoint HTTP de configuración/aprobaciones | T-012/T-010 | Servicios listos; sin endpoint autorizado |
| Catálogo de tipos/montos y UI de aprobaciones | Producto/T-010 | Configuración programática; sin UI |
| Valores fiscales/laborales definitivos | Especialista externo | `PENDIENTE DE VALIDACIÓN LEGAL / CONTABLE` |
| Notificación al aprobador | T-012 | No implementada |
| Reglas de aprobación versionadas | T-010+ | Regla actual por tipo; sin historial de reglas |
| Aprobación fuera de `EnsureApprovedAsync` en módulos | T-016/T-017 | Integración pendiente |
| Aprobación de este PR | @acamposve | **Fusionado** |

## Nota de alcance

No se implementan valores regulatorios, endpoints HTTP, UI de aprobaciones ni notificaciones. Las pruebas usan fuentes y montos sintéticos; el historial de parámetros es inmutable, pero la regla de aprobación se actualiza in-place y no tiene versionado histórico.
