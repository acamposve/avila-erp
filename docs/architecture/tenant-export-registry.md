# Tenant export contract and module/entity registry

**Estado:** R1-REM-05 · **Fecha:** 2026-11-14 · **Dueño:** @acamposve

## 1. Objetivo

Exportar un tenant sin que el orquestador conozca tablas de módulos. Cada módulo aporta una sección mediante `ITenantExportContributor`; el registry las ordena, canonicaliza y firma.

## 2. Contrato

```csharp
public interface ITenantExportContributor
{
    string ModuleKey { get; }
    int Order { get; }
    int SchemaVersion { get; }
    Task<TenantExportSection> ExportAsync(IUnitOfWork uow, TenantExportContext context, CancellationToken ct);
}
```

- `ModuleKey` único y estable.
- `Order` define precedencia; desempate por `ModuleKey` ordinal.
- `SchemaVersion` ≥ 1.
- Cada sección contiene `TenantExportRecord(Entity, Id, JsonObject)`.
- La UoW ya está limitada al tenant; el contributor no recibe un tenant del cliente.

## 3. Determinismo

1. Secciones ordenadas por `Order`, luego `ModuleKey`.
2. Registros dentro de cada sección ordenados por `Entity`, luego `Id`.
3. JSON canónico:
   - claves de objeto ordenadas ordinalmente;
   - arrays preservan orden;
   - números preservan representación raw;
   - sin espacios accidentales.
4. `manifest.sections[].sha256` cubre módulo/schema/registros.
5. `manifest.sha256` cubre formato, tenant y secciones.
6. `GeneratedAt` **no** entra en el hash: el mismo estado produce el mismo hash.

## 4. Manifest

```json
{
  "format": "avila.tenant-export/v1",
  "tenant_id": "...",
  "generated_at": "...",
  "sections": [
    { "module": "administration", "order": 10, "schema_version": 1, "record_count": 3, "sha256": "..." }
  ],
  "sha256": "..."
}
```

El envelope devuelve también las secciones canónicas para streaming/serialización posterior.

## 5. Aislamiento de dominio

- El contrato y el registry viven en `Avila.Platform.Exporting`.
- `Avila.Platform` no referencia `Avila.Modules.*`.
- Los contribuyentes se registran en el composition root:
  - Administration (REM-06);
  - Audit (REM-06);
  - Identity (REM-06);
  - futuros R2: Sales, Inventory, Billing.
- El orquestador no contiene `switch` por módulo ni nombres de tablas.

## 6. Validaciones

- Al menos un contribuyente.
- `ModuleKey` no vacío y único.
- `Order` ≥ 0; schema ≥ 1.
- La sección devuelta debe coincidir con el `ModuleKey` del contribuyente.
- `Entity`/`Id` no vacíos.
- `Data` debe ser objeto JSON.
- Sin duplicados `(Entity, Id)` en una sección.

## 7. Límites

- No hay endpoint HTTP ni autorización todavía (REM-07).
- No hay contribuyentes de dominio todavía (REM-06).
- No hay cifrado de exportación; el transporte es TLS y el acceso se autoriza en REM-07.
- No se exportan secretos, hashes de contraseña, tokens ni claves MFA.
- No reemplaza backups; es exportación lógica autorizada.
