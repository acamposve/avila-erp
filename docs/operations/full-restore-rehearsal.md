# Full-schema restore rehearsal

**Estado:** R1-REM-13 · **Fecha:** 2026-11-15 · **Dueño:** @acamposve

## 1. Test

`FullRestoreRehearsalTests` corre en CI con Testcontainers PostgreSQL 18:

1. crea dos tenants sintéticos con empresa, sucursal, tercero y auditoría;
2. ejecuta `pg_dump -Fc` de la base completa;
3. crea `avila_restore`;
4. ejecuta `pg_restore`;
5. verifica:
   - empresas de ambos tenants;
   - dos eventos `restore.test`;
   - RLS/force RLS y políticas restauradas;
   - lectura RLS de A sin B;
   - cadena SHA-256 de auditoría válida.

El dump incluye todos los esquemas, tablas, políticas y grants; los roles son cluster-wide y ya existen en el mismo contenedor.

## 2. Evidencia

```text
dotnet test Avila.slnx
19 spike + 91 production = 110/110 passed
```

Test específico: `Full_schema_dump_restore_preserves_rls_counts_and_audit_chain`.

## 3. RPO/RTO

| Objetivo | Baseline | Estado |
|---|---|---|
| RPO | 24 h | Provisional |
| RTO | 4 h | Provisional |

No se mide un volumen de producción. El simulacro prueba el mecanismo, no el SLA.

## 4. Límites

- Es una base Testcontainers, no producción.
- No prueba PITR/WAL, cifrado en reposo ni restore desde object storage.
- No prueba migraciones sobre la copia.
- No restaura roles del cluster (existen por el mismo contenedor).
- La autorización para un drill de producción sigue pendiente.
- No sustituye un backup automatizado.

## 5. Próximos pasos

1. Ejecutar en entorno aislado autorizado con volumen representativo.
2. Medir RPO/RTO reales.
3. Repetir al menos cada 90 días.
4. Documentar desviaciones y restore de identidad/roles.
