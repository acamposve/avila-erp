# ADR-003 — Multi-tenancy compartido con `tenant_id` y RLS

- **Estado:** Aceptado
- **Fecha:** 2026-11-08
- **Decisores:** Arquitectura, Seguridad, Producto
- **Reemplaza a:** —

## 1. Contexto

ÁVILA ERP es SaaS para muchas organizaciones. Cada organización (tenant) debe estar aislada de las demás, incluso ante errores de programación, consultas mal filtradas o accesos de soporte. El costo de una fuga de datos entre tenants es crítico: reputacional, legal y comercial.

El producto debe operar cientos o miles de organizaciones con un costo razonable, permitir aprovisionamiento rápido, actualizaciones centralizadas y soporte operativo. Al mismo tiempo, algunos clientes enterprise podrían exigir aislamiento físico en el futuro.

## 2. Decisión

Se adopta un modelo **multi-tenant compartido con aislamiento por fila**, con estas reglas:

1. **Tenant = Organización.** Es la frontera de aislamiento, contratación y facturación SaaS. Una organización contiene empresas y sucursales ([jerarquía](../architecture/multi-tenancy.md)).
2. **Base de datos compartida**, un esquema por módulo. Todas las tablas de negocio incluyen `tenant_id uuid NOT NULL`.
3. **Defensa en profundidad** con cuatro capas:
   - **Capa de aplicación:** filtro obligatorio por tenant en consultas y comandos; el contexto se valida en servidor.
   - **Capa de datos:** claves foráneas compuestas `(tenant_id, id)`, constraints y unicidad que incluyen el tenant.
   - **Capa de base de datos:** RLS habilitada y forzada (`ENABLE` + `FORCE ROW LEVEL SECURITY`) con política por tabla.
   - **Capa de pruebas:** batería automatizada de acceso cruzado que debe fallar siempre.
4. **Contexto de tenant:** el token de acceso contiene la identidad (`sub`), la organización activa (`tid`) y la sesión (`sid`). El servidor **revalida la membresía** en cada petición (con caché corta invalidable) antes de establecer `app.tenant_id` en la transacción.
5. **Prohibido** aceptar `tenant_id` del cuerpo, query string o header sin validación. El cambio de organización emite un token nuevo.
6. **Jobs y eventos** llevan `tenant_id` explícito; no se usa estado ambiental compartido.
7. **Operadores de plataforma** usan un rol distinto, con acceso de solo lectura por defecto, justificación, ticket y auditoría. El acceso de escritura entre tenants es una excepción aprobada y auditada.
8. **Tenant dedicado (R6):** mismo código, base de datos o esquema exclusivo para clientes que lo requieran. No cambia el modelo de dominio.

## 3. Ejemplo de política RLS

```sql
ALTER TABLE sales.orders ENABLE ROW LEVEL SECURITY;
ALTER TABLE sales.orders FORCE ROW LEVEL SECURITY;

CREATE POLICY tenant_isolation ON sales.orders
  USING (tenant_id = nullif(current_setting('app.tenant_id', true), '')::uuid)
  WITH CHECK (tenant_id = nullif(current_setting('app.tenant_id', true), '')::uuid);
```

El rol `avila_app` no es propietario de las tablas y no tiene `BYPASSRLS`. `SET LOCAL app.tenant_id` se ejecuta al inicio de la transacción; si falta, la política no devuelve filas (fail closed).

## 4. Consecuencias

### Positivas
- Costo operativo bajo y aprovisionamiento inmediato.
- Aislamiento verificable en varias capas, no solo en código.
- Actualizaciones y migraciones centralizadas.
- Camino de evolución a tenant dedicado sin reescribir el dominio.

### Negativas
- RLS añade complejidad a consultas y requiere pruebas específicas.
- El pool de conexiones debe resetear el contexto por transacción.
- Un error de configuración de política puede afectar a todos los tenants.
- Consultas analíticas globales requieren rol y ruta separados.

### Riesgos mitigados
- Consulta sin filtro → RLS devuelve vacío.
- IDOR entre tenants → 404/403 y auditoría.
- Job sin contexto → falla al no existir `app.tenant_id`.
- Acceso de soporte → auditado y acotado.

## 5. Alternativas consideradas

| Alternativa | Motivo de descarte |
|---|---|
| Base de datos por tenant | Costo operativo y de migraciones alto para miles de PYMEs; se reserva para dedicado |
| Esquema por tenant | Multiplica objetos y migraciones; difícil de operar a escala |
| Solo filtro de aplicación | Un error de programación produce fuga; no es aceptable |
| Solo RLS | No reemplaza validación de contexto ni autorización; RLS no cubre jobs ni archivos |

## 6. Verificación

- Prueba automatizada que intenta acceder a recursos de otro tenant por ID, filtro, reporte, exportación, archivo y job.
- Prueba de token manipulado (`tid` cambiado) que debe fallar.
- Introspección de esquema: toda tabla de negocio con RLS y `tenant_id`.
- Prueba de fuga en logs y trazas (sin datos de otro tenant).
- Revisión trimestral de políticas y roles.

## 7. Referencias

- [multi-tenancy.md](../architecture/multi-tenancy.md)
- [security.md](../architecture/security.md)
- [database-design.md](../architecture/database-design.md)
- [ADR-002](ADR-002-postgresql.md)
