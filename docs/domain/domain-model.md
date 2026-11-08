# Modelo de dominio

**Versión:** 1.0 · **Fecha:** 2026-11-08 · **Estado:** línea base

## 1. Contextos delimitados

| Contexto | Módulo | Responsabilidad | No es responsable de |
|---|---|---|---|
| Administración | `ADM` | Organización, empresas, sucursales, usuarios, roles, terceros, aprobaciones | Inventario, dinero, fiscalidad |
| Inventario | `INV` | Artículos, almacenes, existencias, movimientos, costos, lotes y seriales | Precios de venta, facturación |
| Ventas | `VEN` | Cotización, pedido, entrega, devolución, precios, comisiones | Emisión fiscal, contabilidad |
| Compras | `COM` | Proveedor, orden, recepción, factura de compra, devolución, costos de compra | Pago, contabilidad |
| Facturación | `FAC` | Comprobantes fiscales, numeración, impuestos, libros, retenciones | Precios comerciales, cobranza |
| Tesorería | `TES` | Caja, bancos, cobros, pagos, cartera, conciliación, multimoneda | Emisión de comprobantes, asientos |
| Contabilidad | `CON` | Plan de cuentas, asientos, períodos, estados financieros | Captura de documentos operativos |
| POS | `POS` | Terminal, turno, venta rápida, offline, hardware | Reglas fiscales o de inventario |
| RRHH/Nómina | `RRH`/`NOM` | Empleados, contratos, conceptos, corridas, prestaciones | Pago bancario (usa TES) |
| Producción | `PRO` | Recetas, órdenes, consumo, costos de producción | Compra o venta |
| CRM | `CRM` | Prospectos, oportunidades, actividades | Facturación |
| Integraciones | `INT` | API, webhooks, importación, adjuntos, mapeos | Reglas de negocio |

## 2. Propiedad de datos

| Dato | Dueño | Otros módulos lo usan como |
|---|---|---|
| Organización / empresa / sucursal | `ADM` | Referencia (`company_id`, `branch_id`) |
| Tercero (cliente/proveedor) | `ADM` | Referencia (`party_id`) + snapshot en el documento |
| Artículo / UoM | `INV` | Referencia (`item_id`) + snapshot |
| Almacén / stock | `INV` | Consulta por contrato |
| Precio de venta | `VEN` | Snapshot en la línea |
| Costo | `INV` | Consulta y snapshot contable |
| Número de comprobante | `FAC` (secuencia en `platform`) | Solo lectura |
| Saldo de cartera | `TES` | Consulta |
| Asiento | `CON` | Solo lectura |
| Empleado / nómina | `NOM` | Referencia en tesorería |
| Configuración fiscal | `ADM`/`FIS` | Lectura versionada |
| Archivo/adjunto | `INT` | Referencia por ID |

**Regla:** ningún módulo escribe datos de otro. Las referencias son IDs más snapshots de los datos que el documento necesita para ser reconstruible.

## 3. Conceptos compartidos (shared kernel)

| Concepto | Tipo | Regla |
|---|---|---|
| `TenantId` | Value object | Obligatorio en todo agregado de negocio |
| `CompanyId` / `BranchId` | Value object | Definen alcance y numeración |
| `Money` | Value object | Importe + moneda; nunca operar entre monedas sin tasa |
| `CurrencyCode` | Value object | ISO 4217 + escala de la versión vigente |
| `ExchangeRate` | Value object | Valor, fuente, vigencia, moneda origen/destino |
| `Quantity` | Value object | Valor + UoM |
| `DocumentNumber` | Value object | Serie + número + tipo; inmutable |
| `PartyRef` | Value object | ID + snapshot fiscal mínimo |
| `ItemRef` | Value object | ID + snapshot de descripción, UoM e impuesto |
| `Period` | Value object | Año/mes y estado |
| `AuditInfo` | Value object | Creado/modificado por y cuándo |
| `CorrelationId` | Value object | Trazabilidad end-to-end |
| `Result` / `DomainError` | Tipo | Errores de negocio explícitos |

## 4. Agregados principales

| Agregado | Raíz | Entidades internas | Invariante central |
|---|---|---|---|
| Organización | `Organization` | Empresas, sucursales | Aislamiento y jerarquía |
| Membresía | `Membership` | Roles y alcance | Permisos derivados de membresía activa |
| Tercero | `Party` | Direcciones, contactos, roles | Identidad fiscal única por tenant |
| Artículo | `Item` | Variantes, códigos, UoM | SKU único por tenant |
| Existencia | `StockBalance` | — | Saldo = suma de movimientos |
| Movimiento | `StockMovement` | — | Inmutable; afecta saldo y costo |
| Cotización | `Quote` | Líneas | Vigencia y conversión controlada |
| Pedido | `Order` | Líneas, reservas | Stock y crédito al confirmar |
| Factura | `Invoice` | Líneas, impuestos, snapshot | Inmutable al emitir; número fiscal |
| Nota de crédito | `CreditNote` | Líneas | Referencia a factura; reversión |
| Orden de compra | `PurchaseOrder` | Líneas | Aprobación por umbral |
| Recepción | `GoodsReceipt` | Líneas | No excede orden pendiente |
| Partida abierta | `Receivable` / `Payable` | Aplicaciones | Saldo = original − aplicado |
| Cobro / pago | `Receipt` / `Payment` | Líneas, aplicaciones | Suma aplicada ≤ importe |
| Asiento | `JournalEntry` | Líneas | Debe = haber; período abierto |
| Corrida de nómina | `PayrollRun` | Líneas, conceptos | Inmutable al aprobar |
| Orden de producción | `WorkOrder` | Componentes, consumos | Consumo ≤ disponible según regla |

## 5. Eventos de dominio e integración

| Evento | Publica | Consumidores | Tipo |
|---|---|---|---|
| `sales.quote.accepted.v1` | VEN | VEN, CRM | Integración |
| `sales.order.confirmed.v1` | VEN | INV, FAC, INT | Dominio + integración |
| `sales.order.cancelled.v1` | VEN | INV, INT | Integración |
| `billing.invoice.issued.v1` | FAC | CON, TES, INT | Dominio + integración |
| `billing.invoice.voided.v1` | FAC | CON, TES, INT | Dominio + integración |
| `billing.credit-note.issued.v1` | FAC | CON, TES, VEN | Dominio + integración |
| `treasury.receipt.registered.v1` | TES | CON, INT | Dominio + integración |
| `treasury.payment.registered.v1` | TES | CON, INT | Dominio + integración |
| `treasury.payment.applied.v1` | TES | CON, INT | Integración |
| `inventory.stock.moved.v1` | INV | Reportes, INT | Integración |
| `inventory.stock.below-reorder.v1` | INV | COM, INT | Integración |
| `purchasing.goods-receipt.posted.v1` | COM | INV, CON | Dominio + integración |
| `accounting.entry.posted.v1` | CON | Reportes, INT | Integración |
| `payroll.run.approved.v1` | NOM | TES, CON | Integración |
| `tenant.plan.changed.v1` | PLT | ADM, INT | Integración |

Reglas: eventos en pasado, versionados, inmutables y con `tenant_id`. Los eventos de dominio que participan de invariantes se manejan en la transacción; los de integración van al outbox.

## 6. Consistencia entre contextos

| Operación | Consistencia | Mecanismo |
|---|---|---|
| Confirmar pedido y reservar stock | Fuerte | Contrato síncrono en la transacción |
| Emitir factura y numerar | Fuerte | Secuencia transaccional |
| Emitir factura y crear partida por cobrar | Fuerte | Contrato síncrono |
| Emitir factura y contabilizar | Fuerte (si la empresa exige contabilidad) | Evento de dominio en proceso |
| Registrar cobro y contabilizar | Fuerte | Evento de dominio en proceso |
| Enviar PDF/correo | Eventual | Outbox |
| Sincronizar POS offline | Eventual e idempotente | Outbox + inbox |
| Proyectar reportes | Eventual | Proyección o vista |

Cuando la contabilidad no está activa (R2 sin `CON`), el evento de contabilización no se publica y se registra la razón; al activarse, se genera el asiento de apertura y se concilia el histórico.

## 7. Reglas transversales

Las reglas globales `BR-001`…`BR-040` están en [PRD §6](../requirements/PRD.md#6-reglas-de-negocio-globales). Cada módulo define sus invariantes específicas `INV-*`, `VEN-*`, etc. en su documento. **Una regla se implementa una sola vez, en el módulo dueño.**

## 8. Nomenclatura de estados

Se usa un ciclo común:

```text
Borrador → Confirmado → Emitido → (Anulado | Revertido)
```

Cada módulo define sus estados y transiciones. Los estados se almacenan como `text` con `CHECK`, nunca como enteros opacos.

## 9. Documentos de módulo

- [Administración](administration.md)
- [Inventario](inventory.md)
- [Ventas](sales.md)
- [Compras](purchasing.md)
- [Facturación](billing.md)
- [Tesorería](treasury.md)
- [Contabilidad](accounting.md)
- [Nómina y RRHH](payroll.md)
- [POS](point-of-sale.md)
- [Producción](production.md)
