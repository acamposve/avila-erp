# Requisitos funcionales — ÁVILA ERP

**Versión:** 1.0 · **Fecha:** 2026-11-08 · **Estado:** línea base

## 1. Convenciones

- **ID:** `MÓD-NNN`. Estable, único y no reutilizable. Un requisito deprecado se marca `DEPRECADO` y no se renumera.
- **Release:** release en que se entrega por primera vez (`R1`…`R6`).
- **Prioridad (MoSCoW):** `M` = Must, `S` = Should, `C` = Could.
- **AC:** criterio de aceptación asociado en [acceptance-criteria.md](acceptance-criteria.md).
- **Estado:** `Confirmado` (decisión de producto), `Hipótesis` (a validar), `Pendiente validación` (materia regulada).
- Los requisitos describen **capacidad verificable**, no implementación. El detalle de reglas e invariantes está en [docs/domain](../domain/domain-model.md).

## 2. Resumen

| Módulo | Prefijo | Requisitos | Release principal |
|---|---|---|---|
| Plataforma SaaS | `PLT` | 6 | R1 |
| Administración | `ADM` | 21 | R1 |
| Inventario | `INV` | 14 | R2 |
| Ventas | `VEN` | 12 | R2 |
| Compras | `COM` | 8 | R2 |
| Facturación | `FAC` | 14 | R2 |
| Fiscal Venezuela | `FIS` | 10 | R2/R3 |
| Tesorería | `TES` | 12 | R2/R3 |
| Contabilidad | `CON` | 12 | R3 |
| RRHH | `RRH` | 4 | R5 |
| Nómina | `NOM` | 10 | R5 |
| POS | `POS` | 10 | R4 |
| Producción | `PRO` | 6 | R6 |
| CRM | `CRM` | 6 | R6 |
| Integraciones | `INT` | 8 | R2/R3/R6 |
| **Total** | | **153** | |

## 3. Plataforma SaaS (`PLT`)

| ID | Requisito | Rel. | Prio. | AC | Estado |
|---|---|---|---|---|---|
| PLT-001 | Catálogo de planes con módulos, límites y precios configurables | R1 | M | AC-PLT-01 | Hipótesis |
| PLT-002 | Medición de uso por organización (usuarios, empresas, documentos, almacenamiento) | R1 | M | AC-PLT-02 | Confirmado |
| PLT-003 | Registro autoservicio y período de prueba con aprovisionamiento automático | R1 | S | AC-PLT-02 | Hipótesis |
| PLT-004 | Suscripción, cobro recurrente, prorrateo y gestión de impago | R2 | M | AC-PLT-01 | Hipótesis |
| PLT-005 | Consola de operador con búsqueda de tenants, soporte e incidencias | R1 | M | AC-PLT-03 | Confirmado |
| PLT-006 | Aprovisionamiento dedicado y borrado seguro al término del contrato | R6 | S | AC-PLT-03 | Pendiente decisión |

## 4. Administración (`ADM`)

| ID | Requisito | Rel. | Prio. | AC | Estado |
|---|---|---|---|---|---|
| ADM-001 | Aprovisionar organización con plan, límites y datos iniciales | R1 | M | AC-ADM-01 | Confirmado |
| ADM-002 | Crear empresas y sucursales bajo una organización | R1 | M | AC-ADM-01 | Confirmado |
| ADM-003 | Usuario global con múltiples membresías y cambio de organización | R1 | M | AC-ADM-02 | Confirmado |
| ADM-004 | Invitaciones, aceptación, suspensión y baja de membresía | R1 | M | AC-ADM-02 | Confirmado |
| ADM-005 | Roles y permisos granulares por módulo, recurso y acción | R1 | M | AC-ADM-03 | Confirmado |
| ADM-006 | Alcance de permisos por empresa, sucursal y almacén | R1 | M | AC-ADM-03 | Confirmado |
| ADM-007 | MFA y política de contraseñas configurable | R1 | M | AC-SEC-02 | Confirmado |
| ADM-008 | Sesiones, expiración, revocación y cierre remoto | R1 | M | AC-SEC-02 | Confirmado |
| ADM-009 | Auditoría append-only de operaciones sensibles con antes/después | R1 | M | AC-ADM-04 | Confirmado |
| ADM-010 | Catálogo único de terceros con roles de cliente y proveedor | R1 | M | AC-ADM-05 | Confirmado |
| ADM-011 | Perfil fiscal y comercial de la empresa | R1 | M | AC-ADM-05 | Confirmado |
| ADM-012 | Series y configuración de numeración documental | R1 | M | AC-ADM-06, AC-FAC-01 | Confirmado |
| ADM-013 | Parámetros versionados con vigencia y fuente | R1 | M | AC-FIS-01 | Confirmado |
| ADM-014 | Acceso de soporte auditado con justificación y alcance temporal | R1 | M | AC-ADM-04 | Confirmado |
| ADM-015 | Suspensión y reactivación de organización | R1 | S | AC-ADM-07 | Confirmado |
| ADM-016 | Exportación completa de datos del tenant en formato abierto | R1 | S | AC-ADM-07 | Confirmado |
| ADM-017 | Planes, límites y bloqueo controlado al exceder cuota | R1 | M | AC-PLT-01 | Confirmado |
| ADM-018 | Feature flags por plan y organización | R1 | S | AC-PLT-01 | Confirmado |
| ADM-019 | Preferencias de idioma, zona horaria, moneda y formato | R1 | S | AC-ADM-01 | Confirmado |
| ADM-020 | Flujos de aprobación configurables por tipo y monto | R1 | M | AC-VEN-02 | Confirmado |
| ADM-021 | SSO y aprovisionamiento SCIM para clientes enterprise | R6 | C | AC-SEC-02 | Pendiente decisión |

## 5. Inventario (`INV`)

| ID | Requisito | Rel. | Prio. | AC | Estado |
|---|---|---|---|---|---|
| INV-001 | Artículos, servicios, variantes y SKU únicos por organización | R2 | M | AC-INV-01 | Confirmado |
| INV-002 | Unidades de medida, presentaciones y conversiones | R2 | M | AC-INV-01 | Confirmado |
| INV-003 | Almacenes por empresa, con vínculo opcional a sucursal | R2 | M | AC-INV-02 | Confirmado |
| INV-004 | Movimientos de inventario con kardex auditable | R2 | M | AC-INV-02 | Confirmado |
| INV-005 | Consulta de existencias por almacén, lote y estado | R2 | M | AC-INV-02 | Confirmado |
| INV-006 | Transferencias entre almacenes con aprobación y tránsito | R2 | M | AC-INV-03 | Confirmado |
| INV-007 | Ajustes y conteos físicos con aprobación y motivo | R2 | M | AC-INV-03 | Confirmado |
| INV-008 | Lotes, vencimientos y control de salida | R2 | S | AC-INV-04 | Confirmado |
| INV-009 | Seriales por unidad con trazabilidad de venta y compra | R4 | M | AC-INV-04 | Confirmado |
| INV-010 | Valoración por costo promedio ponderado por empresa/almacén | R2 | M | AC-INV-05 | Confirmado |
| INV-011 | Reservas de stock por documento y liberación automática | R2 | M | AC-INV-06 | Confirmado |
| INV-012 | Política de stock negativo configurable con permiso | R2 | M | AC-INV-06 | Confirmado |
| INV-013 | Puntos de reposición y sugerencia de compra | R2 | S | AC-INV-06 | Confirmado |
| INV-014 | Impuestos, costo y datos logísticos por defecto del artículo | R2 | M | AC-INV-01 | Confirmado |

## 6. Ventas (`VEN`)

| ID | Requisito | Rel. | Prio. | AC | Estado |
|---|---|---|---|---|---|
| VEN-001 | Cotizaciones con vigencia, versiones y conversión a pedido | R2 | M | AC-VEN-01 | Confirmado |
| VEN-002 | Pedidos con reserva de stock y seguimiento de entrega | R2 | M | AC-VEN-01 | Confirmado |
| VEN-003 | Listas de precios y resolución de precio en servidor | R2 | M | AC-VEN-02 | Confirmado |
| VEN-004 | Descuentos con umbral y aprobación registrada | R2 | M | AC-VEN-02 | Confirmado |
| VEN-005 | Vendedores, metas y comisiones | R2 | S | AC-VEN-03 | Confirmado |
| VEN-006 | Facturación desde pedido o nota de entrega | R2 | M | AC-VEN-04 | Confirmado |
| VEN-007 | Notas de crédito y débito comerciales | R2 | M | AC-VEN-04 | Confirmado |
| VEN-008 | Devoluciones con reingreso a inventario | R2 | M | AC-VEN-04 | Confirmado |
| VEN-009 | Crédito del cliente, límite y bloqueo por morosidad | R2 | M | AC-VEN-03 | Confirmado |
| VEN-010 | Ventas de contado y a crédito con condiciones | R2 | M | AC-VEN-04 | Confirmado |
| VEN-011 | Anticipos de clientes aplicables a facturas | R2 | S | AC-TES-03 | Confirmado |
| VEN-012 | Impresión y envío de documentos comerciales | R2 | S | AC-FAC-04 | Confirmado |

## 7. Compras (`COM`)

| ID | Requisito | Rel. | Prio. | AC | Estado |
|---|---|---|---|---|---|
| COM-001 | Proveedores con condiciones comerciales y datos de retención | R2 | M | AC-COM-01 | Confirmado |
| COM-002 | Órdenes de compra con aprobación por umbral | R2 | M | AC-COM-01 | Confirmado |
| COM-003 | Recepciones parciales y totales contra orden | R2 | M | AC-COM-02 | Confirmado |
| COM-004 | Factura de compra con validación de tres vías | R2 | M | AC-COM-02 | Confirmado |
| COM-005 | Devoluciones a proveedor con salida de inventario | R2 | S | AC-COM-03 | Confirmado |
| COM-006 | Compras en divisas con tasa y moneda documentadas | R2 | M | AC-COM-03 | Confirmado |
| COM-007 | Costos de flete y gastos asignables al costo de compra | R2 | S | AC-INV-05 | Confirmado |
| COM-008 | Compras de servicios y gastos sin movimiento de inventario | R2 | S | AC-COM-02 | Confirmado |

## 8. Facturación (`FAC`)

| ID | Requisito | Rel. | Prio. | AC | Estado |
|---|---|---|---|---|---|
| FAC-001 | Emitir factura con datos fiscales del emisor, receptor e ítems | R2 | M | AC-FAC-01 | Pendiente validación (formato) |
| FAC-002 | Calcular impuestos por línea, descuentos y totales | R2 | M | AC-FAC-02 | Pendiente validación (alícuotas) |
| FAC-003 | Emitir notas de crédito y débito fiscales | R2 | M | AC-FAC-03 | Pendiente validación |
| FAC-004 | Generar libros de ventas y compras | R3 | M | AC-FIS-03 | Pendiente validación |
| FAC-005 | Emitir comprobantes de retención de IVA e ISLR | R3 | M | AC-FIS-03 | Pendiente validación |
| FAC-006 | Registrar moneda, tasa, fuente y vigencia del comprobante | R2 | M | AC-FAC-02 | Confirmado |
| FAC-007 | Validar datos fiscales obligatorios antes de emitir | R2 | M | AC-FAC-01 | Pendiente validación (campos) |
| FAC-008 | Generar representación gráfica e imprimir/exportar | R2 | M | AC-FAC-04 | Pendiente validación (formato) |
| FAC-009 | Gestionar contingencia ante falla del medio de emisión | R3 | C | AC-FAC-05 | Pendiente validación legal |
| FAC-010 | Anular por reversión sin borrar el comprobante | R2 | M | AC-FAC-04 | Confirmado |
| FAC-011 | Integrarse con proveedor autorizado de emisión digital | R2 | M | AC-FAC-05 | Pendiente decisión |
| FAC-012 | Adaptador para máquina fiscal legada | R3 | C | AC-FAC-05 | Pendiente decisión |
| FAC-013 | Numerar por empresa, sucursal, tipo y serie sin duplicados | R2 | M | AC-FAC-01 | Confirmado |
| FAC-014 | Exportar comprobantes y libros en formato abierto | R3 | S | AC-FAC-06 | Confirmado |

## 9. Fiscal Venezuela (`FIS`)

| ID | Requisito | Rel. | Prio. | AC | Estado |
|---|---|---|---|---|---|
| FIS-001 | Motor de reglas fiscales versionado por vigencia | R2 | M | AC-FIS-01 | Confirmado |
| FIS-002 | Gestionar alícuotas y tipos de impuesto configurables | R2 | M | AC-FIS-02 | Pendiente validación (valores) |
| FIS-003 | Calcular y documentar retenciones de IVA e ISLR | R3 | M | AC-FIS-03 | Pendiente validación |
| FIS-004 | Calendario de obligaciones y recordatorios | R3 | S | AC-FIS-03 | Pendiente validación |
| FIS-005 | Generar reportes requeridos por el SENIAT | R3 | M | AC-FIS-03 | Pendiente validación (formatos) |
| FIS-006 | Calcular IGTF según medio de pago y moneda | R2 | M | AC-FIS-02 | Pendiente validación |
| FIS-007 | Registrar exenciones y exoneraciones con soporte documental | R2 | S | AC-FIS-04 | Pendiente validación |
| FIS-008 | Mantener trazabilidad de versiones y cambios normativos | R2 | M | AC-FIS-01 | Confirmado |
| FIS-009 | Exportar libros y reportes fiscales | R3 | S | AC-FIS-03 | Pendiente validación |
| FIS-010 | Auditar emisiones, anulaciones y cambios de parámetros | R2 | M | AC-FIS-04 | Confirmado |

## 10. Tesorería (`TES`)

| ID | Requisito | Rel. | Prio. | AC | Estado |
|---|---|---|---|---|---|
| TES-001 | Cajas, sesiones, apertura y arqueo | R2 | M | AC-TES-01 | Confirmado |
| TES-002 | Cuentas bancarias y movimientos | R3 | M | AC-TES-04 | Confirmado |
| TES-003 | Cobros con múltiples medios de pago | R2 | M | AC-TES-02 | Confirmado |
| TES-004 | Pagos a proveedores y obligaciones | R2 | M | AC-TES-02 | Confirmado |
| TES-005 | Aplicación total/parcial y control de saldos | R2 | M | AC-TES-03 | Confirmado |
| TES-006 | Cuentas por cobrar con antigüedad de saldos | R3 | M | AC-TES-05 | Confirmado |
| TES-007 | Cuentas por pagar con antigüedad de saldos | R3 | M | AC-TES-05 | Confirmado |
| TES-008 | Conciliación bancaria asistida por importación | R3 | M | AC-TES-04 | Confirmado |
| TES-009 | Transferencias entre cuentas y cajas | R3 | S | AC-TES-04 | Confirmado |
| TES-010 | Multimoneda, diferencias cambiarias y revaluación | R3 | M | AC-TES-05 | Pendiente validación contable |
| TES-011 | Anticipos de clientes y prepagos a proveedores | R2 | S | AC-TES-03 | Confirmado |
| TES-012 | Cierre de caja y reportes de operación | R2 | M | AC-TES-01 | Confirmado |

## 11. Contabilidad (`CON`)

| ID | Requisito | Rel. | Prio. | AC | Estado |
|---|---|---|---|---|---|
| CON-001 | Plan de cuentas jerárquico con cuentas auxiliares | R3 | M | AC-CON-01 | Confirmado |
| CON-002 | Asientos con partida doble y validaciones de período | R3 | M | AC-CON-01 | Confirmado |
| CON-003 | Libro diario, mayor y balanza de comprobación | R3 | M | AC-CON-02 | Confirmado |
| CON-004 | Estados financieros básicos (situación y resultados) | R3 | M | AC-CON-02 | Confirmado |
| CON-005 | Períodos contables, cierre y bloqueo | R3 | M | AC-CON-03 | Confirmado |
| CON-006 | Asientos automáticos por reglas de contabilización | R3 | M | AC-CON-04 | Confirmado |
| CON-007 | Auxiliares por tercero y documento | R3 | M | AC-CON-04 | Confirmado |
| CON-008 | Centros de costo y dimensiones analíticas | R3 | S | AC-CON-04 | Confirmado |
| CON-009 | Reversión por contraasiento con referencia | R3 | M | AC-CON-05 | Confirmado |
| CON-010 | Apertura de saldos iniciales | R3 | M | AC-CON-03 | Confirmado |
| CON-011 | Bloqueo y reapertura auditada de período | R3 | M | AC-CON-03 | Confirmado |
| CON-012 | Conciliación entre subledgers y contabilidad | R3 | M | AC-CON-05 | Confirmado |

## 12. RRHH y Nómina (`RRH`, `NOM`)

| ID | Requisito | Rel. | Prio. | AC | Estado |
|---|---|---|---|---|---|
| RRH-001 | Expediente de empleado con datos sensibles protegidos | R5 | M | AC-RRH-01 | Pendiente validación (datos) |
| RRH-002 | Contratos, cargos y estructura organizativa | R5 | M | AC-RRH-01 | Confirmado |
| RRH-003 | Vacaciones, permisos y ausencias | R5 | M | AC-RRH-02 | Pendiente validación |
| RRH-004 | Historial laboral y gestión documental del empleado | R5 | S | AC-RRH-02 | Confirmado |
| NOM-001 | Conceptos salariales y reglas de cálculo configurables | R5 | M | AC-NOM-01 | Pendiente validación |
| NOM-002 | Nómina periódica con novedades y recálculo | R5 | M | AC-NOM-01 | Pendiente validación |
| NOM-003 | Deducciones, aportes y retenciones laborales | R5 | M | AC-NOM-01 | Pendiente validación |
| NOM-004 | Utilidades y bonificaciones | R5 | M | AC-NOM-02 | Pendiente validación |
| NOM-005 | Prestaciones sociales e intereses | R5 | M | AC-NOM-02 | Pendiente validación |
| NOM-006 | Liquidación de empleados | R5 | M | AC-NOM-03 | Pendiente validación |
| NOM-007 | Recibos de pago y comunicación al empleado | R5 | M | AC-NOM-03 | Confirmado |
| NOM-008 | Provisión contable de nómina | R5 | M | AC-NOM-04 | Pendiente validación |
| NOM-009 | Parámetros legales versionados por vigencia | R5 | M | AC-NOM-01 | Pendiente validación |
| NOM-010 | Pago de nómina a través de tesorería | R5 | M | AC-NOM-04 | Confirmado |

## 13. POS (`POS`)

| ID | Requisito | Rel. | Prio. | AC | Estado |
|---|---|---|---|---|---|
| POS-001 | Terminales y sesiones de caja por sucursal | R4 | M | AC-POS-01 | Confirmado |
| POS-002 | Venta rápida con código de barras y búsqueda | R4 | M | AC-POS-01 | Confirmado |
| POS-003 | Medios de pago mixtos y vueltos | R4 | M | AC-POS-02 | Confirmado |
| POS-004 | Impresión ESC/POS y apertura de gaveta | R4 | M | AC-POS-02 | Confirmado |
| POS-005 | Operación offline con sincronización posterior | R4 | M | AC-POS-03 | Pendiente validación fiscal |
| POS-006 | Cierres X/Z y arqueo de caja | R4 | M | AC-POS-04 | Confirmado |
| POS-007 | Anulaciones y devoluciones desde caja | R4 | M | AC-POS-04 | Confirmado |
| POS-008 | Emisión de factura fiscal desde POS | R4 | M | AC-POS-04 | Pendiente validación |
| POS-009 | Control de descuentos y autorizaciones | R4 | S | AC-POS-02 | Confirmado |
| POS-010 | Múltiples cajas operando en una sucursal | R4 | S | AC-POS-01 | Confirmado |

## 14. Producción (`PRO`)

| ID | Requisito | Rel. | Prio. | AC | Estado |
|---|---|---|---|---|---|
| PRO-001 | Recetas y listas de materiales versionadas | R6 | M | AC-PRO-01 | Confirmado |
| PRO-002 | Órdenes de producción con estados y aprobación | R6 | M | AC-PRO-01 | Confirmado |
| PRO-003 | Consumo de componentes, mermas y desperdicio | R6 | M | AC-PRO-02 | Confirmado |
| PRO-004 | Costos de producción, mano de obra y conversión | R6 | M | AC-PRO-02 | Hipótesis |
| PRO-005 | Overhead y costeo por orden | R6 | S | AC-PRO-02 | Hipótesis |
| PRO-006 | Trazabilidad del lote producido | R6 | S | AC-PRO-01 | Confirmado |

## 15. CRM (`CRM`)

| ID | Requisito | Rel. | Prio. | AC | Estado |
|---|---|---|---|---|---|
| CRM-001 | Prospectos con origen, datos y responsable | R6 | M | AC-CRM-01 | Confirmado |
| CRM-002 | Oportunidades con etapas y valor estimado | R6 | M | AC-CRM-01 | Confirmado |
| CRM-003 | Actividades y seguimiento con recordatorios | R6 | S | AC-CRM-01 | Confirmado |
| CRM-004 | Conversión de prospecto a cliente | R6 | M | AC-CRM-02 | Confirmado |
| CRM-005 | Cotización desde oportunidad | R6 | M | AC-CRM-02 | Confirmado |
| CRM-006 | Métricas de embudo y conversión | R6 | S | AC-CRM-02 | Confirmado |

## 16. Integraciones (`INT`)

| ID | Requisito | Rel. | Prio. | AC | Estado |
|---|---|---|---|---|---|
| INT-001 | API pública versionada, documentada y con sandbox | R6 | M | AC-INT-01 | Confirmado |
| INT-002 | Credenciales de integración con scopes y rotación | R6 | M | AC-INT-01 | Confirmado |
| INT-003 | Webhooks firmados con reintentos e idempotencia | R6 | M | AC-INT-02 | Confirmado |
| INT-004 | Importación CSV con validación, errores y lotes | R2 | M | AC-MIG-01 | Confirmado |
| INT-005 | Integración de catálogo y pedidos con e-commerce | R6 | S | AC-INT-03 | Hipótesis |
| INT-006 | Gestión documental con almacenamiento y retención | R6 | S | AC-INT-03 | Confirmado |
| INT-007 | Exportación para BI y reportes externos | R6 | S | AC-INT-03 | Confirmado |
| INT-008 | Conciliación bancaria por archivo de estado de cuenta | R3 | S | AC-TES-04 | Confirmado |

## 17. Trazabilidad por release

| Release | Requisitos incluidos |
|---|---|
| R1 | PLT-001…PLT-003, PLT-005; ADM-001…ADM-020 |
| R2 | INV-001…INV-008, INV-010…INV-014; VEN-001…VEN-012; COM-001…COM-008; FAC-001…FAC-003, FAC-006…FAC-008, FAC-010, FAC-011, FAC-013; FIS-001, FIS-002, FIS-006…FIS-008, FIS-010; TES-001, TES-003…TES-005, TES-011, TES-012; PLT-004; INT-004 |
| R3 | FAC-004, FAC-005, FAC-009, FAC-012, FAC-014; FIS-003…FIS-005, FIS-009; TES-002, TES-006…TES-010; CON-001…CON-012; INT-008 |
| R4 | POS-001…POS-010; INV-009 |
| R5 | RRH-001…RRH-004; NOM-001…NOM-010 |
| R6 | PRO-001…PRO-006; CRM-001…CRM-006; INT-001…INT-003, INT-005…INT-007; ADM-021; PLT-006 |

> Los requisitos se agrupan por release para facilitar la verificación. Un requisito puede aparecer en un release posterior si su validación externa lo bloquea; el cambio se registra en [releases.md](../../backlog/releases.md).

## 18. Reglas de mantenimiento

1. Un requisito nuevo recibe el siguiente número libre del módulo.
2. Cambiar el release de un requisito es un cambio de alcance: se actualiza [releases.md](../../backlog/releases.md) y [roadmap.md](../product/roadmap.md).
3. Cada requisito debe tener al menos un `AC`; ningún `AC` puede quedar sin requisitos.
4. Los requisitos `Pendiente validación` no se implementan como definitivos hasta que la decisión (`DEC`) esté resuelta.
5. Verificación automatizada de IDs y enlaces en [documentation-review.md](../documentation-review.md).
