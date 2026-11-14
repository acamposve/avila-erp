# Tenant job runner

**Estado:** R1-REM-04 · **Fecha:** 2026-11-14 · **Dueño:** @acamposve

## 1. Contrato

```csharp
Task RunAsync(Guid tenantId, ITenantJob job, CancellationToken cancellationToken);
```

- `tenantId` vacío (`Guid.Empty`) se rechaza con `ArgumentException`.
- `job` nulo se rechaza con `ArgumentNullException`.
- El runner limpia el contexto ambiental **antes** de crear la UoW.
- Cada job recibe una UoW con `SET LOCAL app.tenant_id` explícito.
- El job no puede tomar tenant de `AsyncLocal`, headers ni estado global.

## 2. Contexto ambiental

`TenantExecutionContext` existe solo como Surface de diagnóstico/limpieza. No es fuente de tenant.

- `Set` rechaza `Guid.Empty`.
- El runner ejecuta `Clear()` de forma síncrona en el límite público.
- El `finally` vuelve a limpiar.
- Un job nunca hereda el tenant del llamador.

## 3. Ciclo de ejecución

1. Validar tenant/job.
2. Limpiar contexto ambiental.
3. Abrir UoW por tenant.
4. Verificar que el tenant no está suspendido (`TenantAccessGuard`).
5. Ejecutar el job.
6. Commit único.
7. En excepción: rollback por dispose y limpieza de contexto.

## 4. Pruebas

| Test | Verifica |
|---|---|
| `Empty_tenant_and_null_job_are_rejected_at_boundary` | Contrato de entrada |
| `Ambient_context_is_cleared_before_job_and_after_completion` | El job observa `null` |
| `Parallel_interleaved_jobs_keep_tenant_scoped_context` | 24 jobs A/B intercalados; sin cruces |
| `Failed_job_rolls_back_and_clears_ambient_context` | Rollback y contexto limpio antes del job |

## 5. Límites

- Es un runner in-process; no implementa una cola, reintentos, visibilidad ni dead-letter.
- El outbox/worker real se integrará en R2.
- No se registra un hosted service todavía.
- La suspensión se verifica con `TenantAccessGuard`; jobs de solo lectura deben usar otro contrato.
