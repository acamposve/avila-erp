# Simulacro de respaldo y restauración — T-012

**Fecha:** 2026-11-14
**Entorno:** PostgreSQL 18 desechable (Docker), sin datos reales.
**Estado:** PASS local; **no** ejecutado en producción ni en un entorno compartido.

## 1. Objetivo

Probar de forma reproducible que un dump lógico y su restauración conservan:

- esquema y tabla;
- política RLS forzada;
- grants al rol de aplicación;
- datos separados por tenant;
- lectura aislada por tenant después del restore.

## 2. Script

[`scripts/backup-restore-rehearsal.ps1`](../../scripts/backup-restore-rehearsal.ps1)

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/backup-restore-rehearsal.ps1
```

El script genera una contraseña efímera, no la imprime y elimina el contenedor al final.

## 3. Resultado

```text
container=avila-rto-<efímero>
dump=pg_dump -Fc
restore=pg_restore
restored_rows_total=2
restored_rows_tenant_a=1
restored_rows_tenant_b=1
result=PASS
```

Salida completa: [`tasks/evidence/T-012/raw/restore-rehearsal.txt`](../../tasks/evidence/T-012/raw/restore-rehearsal.txt).

## 4. Qué prueba

- `pg_dump`/`pg_restore` funcionan con RLS y políticas.
- Los datos de dos tenants se conservan.
- El rol de aplicación restaurado sigue viendo **solo** su tenant.
- El procedimiento es reproducible y desechable.

## 5. Qué **no** prueba

- No es un backup de la base completa de ÁVILA.
- No restaura los roles/cluster completos (solo la base).
- No mide RPO/RTO reales ni volumen de producción.
- No prueba PITR/WAL ni cifrado en reposo.
- No prueba migraciones de aplicación sobre la copia.
- No sustituye un simulacro autorizado en un entorno compartido.

## 6. RPO/RTO

| Objetivo | Valor | Estado |
|---|---|---|
| RPO | 24 h | Provisional; requiere aprobación |
| RTO | 4 h | Provisional; requiere aprobación |
| Retención | Pendiente `DEC-008` | No definida |

## 7. Próximos pasos autorizados

1. Aprobar RPO/RTO y retención.
2. Configurar backups automáticos y verificación.
3. Ejecutar restore en entorno aislado autorizado con datos sintéticos de tamaño representativo.
4. Medir tiempos y documentar desviaciones.
5. Repetir al menos cada 90 días.
