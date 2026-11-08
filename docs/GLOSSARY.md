# Glosario y nomenclatura canónica

Este documento fija el vocabulario que **todos** los documentos, el código y la interfaz deben usar. Si un documento introduce un término nuevo, debe agregarse aquí y enlazarse.

## 1. Términos de plataforma

| Término | Definición | Equivalente técnico |
|---|---|---|
| **Plataforma** | Operación SaaS de ÁVILA: aprovisionamiento, planes, soporte, métricas globales. | `Platform` |
| **Organización** | Cliente que contrata ÁVILA. Es la **frontera de aislamiento** y de facturación SaaS. | `Tenant` / `tenant_id` |
| **Empresa** | Persona jurídica dentro de una organización, con RIF, domicilio fiscal y contabilidad propia. | `Company` |
| **Sucursal** | Establecimiento de una empresa (tienda, depósito, oficina) con operación y numeración propias. | `Branch` |
| **Almacén** | Ubicación física o lógica donde se custodia inventario; pertenece a una empresa y puede vincularse a una sucursal. | `Warehouse` |
| **Caja** | Punto de cobro/pago con sesión, medios de pago y arqueo. | `CashBox` |
| **Membresía** | Vínculo entre un usuario y una organización, con roles y alcance. Un usuario puede pertenecer a varias organizaciones. | `TenantMembership` |
| **Contexto de tenant** | `tenant_id` activo de la petición, validado en el servidor contra la membresía del usuario. | `ITenantContext` |
| **Operador de plataforma** | Usuario interno de ÁVILA con acceso de soporte, siempre auditado y con mínimo privilegio. | `PlatformOperator` |
| **Plan** | Conjunto comercial de módulos y límites contratados. | `Plan` |
| **Límite de plan** | Cuota verificable (empresas, sucursales, usuarios, comprobantes, almacenamiento). | `PlanLimit` |

## 2. Términos de negocio

| Término | Definición |
|---|---|
| **Tercero** | Persona natural o jurídica con la que se opera; puede ser cliente, proveedor o ambos. Los datos maestros son únicos; los roles comerciales son específicos. |
| **Cliente** | Rol de un tercero en ventas (crédito, lista de precios, vendedor asignado). |
| **Proveedor** | Rol de un tercero en compras (condiciones, retenciones, moneda). |
| **Artículo** | Bien o servicio vendible o comprable. Puede ser inventariable o no inventariable. |
| **Variante** | Combinación de atributos de un artículo (talla, color, presentación) con SKU propio. |
| **SKU** | Identificador único de artículo o variante dentro de una organización. |
| **Unidad de medida (UoM)** | Unidad de compra, almacenamiento o venta; las conversiones son configurables. |
| **Documento comercial** | Cotización, pedido, nota de entrega, factura, nota de crédito/débito, devolución. |
| **Comprobante fiscal** | Documento con efectos tributarios (factura, nota de crédito/débito, comprobante de retención). |
| **Partida abierta** | Saldo pendiente de una cuenta por cobrar o pagar, asociado a un documento y a un tercero. |
| **Aplicación de pago** | Imputación de un cobro/pago (total o parcial) a una o varias partidas abiertas. |
| **Asiento** | Registro contable de partida doble, con fecha, período y líneas debe/haber. |
| **Contabilización** | Acto de convertir un documento en asientos inmutables. |
| **Reversión** | Contrapartida que anula el efecto de un documento o asiento sin alterar el original. |
| **Kardex** | Historial valorado de movimientos de un artículo por almacén. |
| **Costo promedio ponderado** | Método de valoración por defecto: el costo unitario se recalcula con cada entrada. |
| **Lote** | Conjunto de unidades con el mismo vencimiento/origen, trazable. |
| **Serial** | Unidad física única identificada individualmente. |
| **Tasa de cambio** | Valor de conversión entre dos monedas, con fuente, vigencia y responsable. |
| **Moneda funcional** | Moneda de registro contable de una empresa. |
| **Moneda de transacción** | Moneda en que se documenta una operación distinta de la funcional. |
| **Redondeo** | Regla determinista para ajustar importes a la escala de la moneda; ver ADR-004. |

## 3. Términos técnicos

| Término | Definición |
|---|---|
| **Módulo** | Unidad de negocio desplegable dentro del monolito, con contratos públicos y datos propios. |
| **Contrato de módulo** | Interfaz, DTO o evento público en `Avila.Modules.<Módulo>.Contracts`. |
| **Shared kernel** | Tipos primitivos compartidos (TenantId, Money, DocumentId, Clock, Result). |
| **Outbox** | Tabla transaccional donde se registran eventos de integración para publicación confiable. |
| **Inbox** | Registro de eventos procesados que garantiza idempotencia del consumidor. |
| **RLS** | Row Level Security de PostgreSQL: política que restringe filas por `tenant_id`. |
| **Idempotencia** | Propiedad por la cual repetir una operación con la misma clave produce el mismo resultado. |
| **Documento inmutable** | Documento emitido/contabilizado que no admite modificación ni borrado; solo reversión. |
| **Numeración fiscal** | Secuencia de comprobantes por empresa/sucursal/tipo/serie, sin huecos no justificados. |
| **Snapshot fiscal** | Copia inmutable de datos del emisor, receptor, ítems, impuestos y tasa usados al emitir. |
| **Feature flag** | Interruptor de funcionalidad por tenant/plan, auditable. |
| **Tenant dedicado** | Modalidad empresarial con base de datos o esquema exclusivo (R6). |
| **MoSCoW** | Priorización: Must, Should, Could, Won't (en este release). |

## 4. Códigos de módulo

| Código | Módulo | Documento | Release de introducción |
|---|---|---|---|
| `PLT` | Plataforma SaaS (requisitos transversales) | [administration.md](domain/administration.md) | R1 |
| `ADM` | Administración y seguridad | [administration.md](domain/administration.md) | R1 |
| `INV` | Inventario | [inventory.md](domain/inventory.md) | R2 |
| `VEN` | Ventas | [sales.md](domain/sales.md) | R2 |
| `COM` | Compras | [purchasing.md](domain/purchasing.md) | R2 |
| `FAC` | Facturación | [billing.md](domain/billing.md) | R2 |
| `FIS` | Cumplimiento fiscal Venezuela | [compliance/venezuela-tax.md](compliance/venezuela-tax.md) | R2 (base), R3 (completo) |
| `TES` | Tesorería y finanzas operativas | [treasury.md](domain/treasury.md) | R2 (básico), R3 (completo) |
| `CON` | Contabilidad | [accounting.md](domain/accounting.md) | R3 |
| `POS` | Punto de venta | [point-of-sale.md](domain/point-of-sale.md) | R4 |
| `RRH` | Recursos Humanos | [payroll.md](domain/payroll.md) | R5 |
| `NOM` | Nómina | [payroll.md](domain/payroll.md) | R5 |
| `PRO` | Producción | [production.md](domain/production.md) | R6 |
| `CRM` | CRM | [domain-model.md](domain/domain-model.md) (módulo futuro) | R6 |
| `INT` | Integraciones, API y e-commerce | [architecture/integration-strategy.md](architecture/integration-strategy.md) | R6 |

## 5. Nomenclatura de datos

- Esquemas PostgreSQL en minúsculas y singular por módulo: `platform`, `admin`, `inventory`, `sales`, `purchasing`, `billing`, `treasury`, `accounting`, `payroll`, `pos`, `production`, `crm`, `integration`, `shared`.
- Tablas en `snake_case` y plural: `sales.orders`, `billing.invoices`, `inventory.stock_movements`.
- Columnas en `snake_case`: `tenant_id`, `company_id`, `branch_id`, `created_at`, `row_version`.
- Claves primarias: `id` tipo `uuid` (UUIDv7 generado en aplicación). Claves foráneas compuestas `(tenant_id, id)` cuando aplique.
- Eventos: `<módulo>.<entidad>.<acción>.v<n>` en pasado, p. ej. `sales.order.confirmed.v1`, `billing.invoice.issued.v1`.
- Permisos: `<módulo>.<recurso>.<acción>`, p. ej. `sales.invoice.issue`, `inventory.stock.adjust`.
- Endpoints REST: `/api/v1/<recurso-en-plural>`; recursos anidados solo cuando el hijo no existe sin el padre.
- Ramas: `feature/<id>-<slug>`, `fix/<id>-<slug>`, `docs/<slug>`.
- Commits: Conventional Commits (`feat(sales): ...`).

## 6. Estados de afirmaciones

| Etiqueta | Significado | Acción permitida |
|---|---|---|
| `CONFIRMADO` | Verificado con fuente primaria o decisión aprobada. | Implementar. |
| `HIPÓTESIS` | Supuesto razonable pendiente de validación. | Diseñar, no prometer. |
| `PENDIENTE` | Falta información o decisión. | No implementar como definitivo. |
| `PENDIENTE DE VALIDACIÓN LEGAL / CONTABLE` | Materia regulada sin fuente suficiente. | Solo dejar puntos de extensión. |
| `DECISIÓN PENDIENTE` (`DEC-NNN`) | Requiere aprobación del comité. | No avanzar en lo afectado. |

## 7. Términos que se deben evitar

- **"Sistema"** como sinónimo de producto: usar "plataforma", "suite" o "módulo".
- **"Cliente"** para referirse a la organización que contrata: usar "organización" (el cliente es un tercero de ventas).
- **"Factura"** para documentos internos: usar "documento comercial" o "nota de entrega".
- **"Contraseña"** en logs o URLs: nunca.
- **"Temporal"** para datos sin fecha de expiración: especificar retención.
