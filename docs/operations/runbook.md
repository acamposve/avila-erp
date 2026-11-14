# Runbook de operación — ÁVILA ERP

**Alcance:** R1, monolito modular; no cubre aún producción multi-región.
**Dueño:** @acamposve · **Ejecutor:** Operaciones/Plataforma.

## 1. Primeros 5 minutos

1. Confirmar alcance: ¿un tenant, una región o todos?
2. Consultar `GET /health/live` y `GET /health/ready`.
3. Revisar `GET /metrics` desde la red interna.
4. Buscar por `CorrelationId` en logs; no abrir bodies ni datos de clientes.
5. Si hay 5xx masivos, verificar base de datos y últimas migraciones/despliegues.
6. Abrir incidente con hora, tenant(s), impacto y correlación. **No** incluir PII ni secretos.

## 2. Roles y límites

| Rol | Responsabilidad | Límite |
|---|---|---|
| @acamposve | Arquitectura, seguridad y decisiones | Dueño de la decisión |
| Operaciones | Salud, backups, restauración | No cambia reglas de negocio |
| DBA/Plataforma | Migraciones, RLS, roles | Requiere ventana y backup |
| Soporte | Acceso con ticket/justificación | Solo lectura, expira |

- No se otorgan credenciales de producción a agentes de IA.
- No se ejecutan migraciones ni restores en compartido sin autorización explícita.
- El rol `avila_platform` es de plataforma; `avila_app` no puede leer tenants globales.

## 3. Diagnóstico por síntoma

| Síntoma | Revisar | Acción inicial |
|---|---|---|
| `/health/live` falla | Proceso/contenedor | Reiniciar instancia; revisar logs de arranque |
| `/health/ready` falla | Conexión PostgreSQL, pool, credenciales | Verificar DB y límites; no rotar secretos sin autorización |
| 5xx altos | Logs por correlación, última release | Rollback de la release si es regression |
| 401/403 inesperado | Sesión/tenant/permiso | Validar sesión y scope; no desactivar auth |
| Errores RLS | `app.tenant_id`, rol, política | Revisar contexto de tenant; no desactivar RLS |
| Suspensión | `platform.tenant_lifecycle` | Confirmar ticket; reactivar solo con aprobación |
| Migración bloqueada | Locks, backfill, RLS | Detener, medir, aplicar por tenant |

## 4. Migración y backfill bajo RLS

Reglas:

1. Backup verificado antes de migrar.
2. Migración en ventana; una transacción por tenant para backfill.
3. `set_config('app.tenant_id', ..., true)` dentro de la transacción.
4. No `UPDATE` global sobre tablas con RLS forzada.
5. Probar primero en copia/Testcontainers; medir locks y duración.
6. Plan de rollback: migración inversa o restaurar backup; nunca editar datos fiscales emitidos.
7. Si falla: detener, preservar evidencia, no reintentar a ciegas.

## 5. Suspensión y exportación

- Suspender: actualizar `platform.tenant_lifecycle` con estado `suspended`, motivo y timestamp.
- El guard de escritura debe invocarse por los comandos; **no** está integrado en todos los módulos todavía.
- Lectura/exportación permanece disponible.
- Reactivar solo con aprobación y ticket; registrar motivo.
- La exportación no acepta tenant objetivo: usa el tenant del UoW + RLS.

## 6. Restauración

Procedimiento reproducible en entorno **desechable**:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/backup-restore-rehearsal.ps1
```

El script:

1. inicia PostgreSQL desechable;
2. crea una tabla con RLS y datos sintéticos;
3. ejecuta `pg_dump -Fc`;
4. crea una base nueva y ejecuta `pg_restore`;
5. verifica conteos totales y por tenant con `SET ROLE`.

Producción:

1. Solicitar autorización a @acamposve.
2. Verificar backup y ventana.
3. Restaurar en instancia aislada, no sobre producción.
4. Validar migraciones, RLS, roles y conteos.
5. Documentar RPO/RTO reales y diferencias.

**No ejecutado en producción.** Ver [backup-restore.md](backup-restore.md).

## 7. Incidente y escalamiento

- Severidad 1: pérdida de datos, brecha, todos los tenants caídos.
- Severidad 2: un tenant o función crítica degradada.
- Severidad 3: cosmético o workaround.
- Escalar a @acamposve; no auto-aprobar cambios de arquitectura, seguridad o datos.
- Preservar logs con correlación; no copiar PII a tickets.

## 8. Pendientes

- Alertas reales y dashboards.
- OpenTelemetry/exportador de trazas.
- Autenticación/allowlist de `/metrics`.
- Backups automáticos, retención y prueba en entorno autorizado.
- Guard de suspensión en todos los comandos.
- Runbook de outbox (R2+).
