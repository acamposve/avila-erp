# Epics — ÁVILA ERP

**Versión:** 1.0 · **Fecha:** 2026-11-08

Un epic agrupa trabajo que entrega un resultado verificable. Cada epic pertenece a un release y referencia requisitos (`RF`) y criterios (`AC`). Las historias iniciales están en [sprint-backlog.md](sprint-backlog.md).

## 1. Resumen

| Release | Epics | Foco |
|---|---|---|
| R1 | `EP-PLT-01`…`EP-PLT-03`, `EP-ADM-01`…`EP-ADM-05`, `EP-INT-01` | Plataforma, identidad, permisos, auditoría |
| R2 | `EP-INV-01`…`EP-INV-04`, `EP-VEN-01`…`EP-VEN-03`, `EP-COM-01`…`EP-COM-02`, `EP-FAC-01`…`EP-FAC-03`, `EP-FIS-01`…`EP-FIS-02`, `EP-TES-01`…`EP-TES-02`, `EP-INT-02` | Primer producto vendible |
| R3 | `EP-CON-01`…`EP-CON-03`, `EP-TES-03`…`EP-TES-04`, `EP-FIS-03`, `EP-FAC-04`, `EP-INT-03` | Finanzas y cumplimiento |
| R4 | `EP-POS-01`…`EP-POS-02`, `EP-INV-05` | Retail y POS |
| R5 | `EP-RRH-01`, `EP-NOM-01`…`EP-NOM-03` | Personas y nómina |
| R6 | `EP-PRO-01`, `EP-CRM-01`, `EP-INT-04`…`EP-INT-05`, `EP-PLT-04` | Enterprise e integraciones |

## 2. R1 — ÁVILA Core

| Epic | Objetivo | Requisitos | AC |
|---|---|---|---|
| `EP-PLT-01` Aprovisionamiento y ciclo de vida del tenant | Crear una organización operable y aislada en minutos | `PLT-001`, `ADM-001`, `ADM-002`, `ADM-015` | AC-ADM-01, AC-ADM-07 |
| `EP-PLT-02` Planes, límites y medición | Controlar el consumo y bloquear con claridad | `PLT-002`, `PLT-003`, `PLT-004`, `ADM-017`, `ADM-018` | AC-PLT-01, AC-PLT-02 |
| `EP-PLT-03` Consola de operador y soporte | Operar el SaaS con trazabilidad | `PLT-005`, `ADM-014` | AC-PLT-03, AC-ADM-04 |
| `EP-ADM-01` Identidad, sesiones y MFA | Autenticación segura y multi-organización | `ADM-003`, `ADM-004`, `ADM-007`, `ADM-008` | AC-ADM-02, AC-SEC-02 |
| `EP-ADM-02` Roles, permisos y alcance | Autorización granular y verificable | `ADM-005`, `ADM-006` | AC-ADM-03, AC-SEC-03 |
| `EP-ADM-03` Empresas, sucursales y maestros | Base organizacional y terceros | `ADM-002`, `ADM-010`, `ADM-011`, `ADM-019` | AC-ADM-01, AC-ADM-05 |
| `EP-ADM-04` Auditoría, numeración y configuración | Evidencia y parámetros versionados | `ADM-009`, `ADM-012`, `ADM-013`, `ADM-020` | AC-ADM-04, AC-FAC-01, AC-FIS-01 |
| `EP-ADM-05` Exportación y suspensión | Ciclo de vida y portabilidad | `ADM-015`, `ADM-016` | AC-ADM-07 |
| `EP-INT-01` Entrega, observabilidad y respaldo | Fundación operativa | NFR-OBS-001…006, NFR-AVA-001…007 | AC-OBS-01 |

## 3. R2 — ÁVILA Comercial

| Epic | Objetivo | Requisitos | AC |
|---|---|---|---|
| `EP-INV-01` Catálogo, variantes y UoM | Base de artículos confiable | `INV-001`, `INV-002`, `INV-014` | AC-INV-01 |
| `EP-INV-02` Almacenes, existencias y kardex | Saber qué hay y cómo se movió | `INV-003`, `INV-004`, `INV-005` | AC-INV-02 |
| `EP-INV-03` Transferencias, ajustes y valoración | Costo y movimientos controlados | `INV-006`, `INV-007`, `INV-010` | AC-INV-03, AC-INV-05 |
| `EP-INV-04` Lotes, reservas y reposición | Disponibilidad y trazabilidad | `INV-008`, `INV-011`, `INV-012`, `INV-013` | AC-INV-04, AC-INV-06 |
| `EP-VEN-01` Cotización, pedido y entrega | Ciclo comercial completo | `VEN-001`, `VEN-002`, `VEN-010` | AC-VEN-01 |
| `EP-VEN-02` Precios, descuentos y crédito | Precio correcto y riesgo controlado | `VEN-003`, `VEN-004`, `VEN-009` | AC-VEN-02, AC-VEN-03 |
| `EP-VEN-03` Comisiones y devoluciones | Compensación y reversión | `VEN-005`, `VEN-007`, `VEN-008`, `VEN-011`, `VEN-012` | AC-VEN-03, AC-VEN-04 |
| `EP-COM-01` Proveedores y órdenes | Comprar con control | `COM-001`, `COM-002`, `COM-008` | AC-COM-01 |
| `EP-COM-02` Recepción, factura y devolución | Costo real y obligaciones | `COM-003`…`COM-007` | AC-COM-02, AC-COM-03 |
| `EP-FAC-01` Emisión fiscal y numeración | Comprobante válido y trazable | `FAC-001`, `FAC-002`, `FAC-006`, `FAC-007`, `FAC-013` | AC-FAC-01, AC-FAC-02 |
| `EP-FAC-02` Notas y representación | Corregir sin destruir | `FAC-003`, `FAC-008`, `FAC-010` | AC-FAC-03, AC-FAC-04 |
| `EP-FAC-03` Adaptador de emisión | Integrar el medio autorizado | `FAC-011` | AC-FAC-05 |
| `EP-FIS-01` Motor de reglas y parámetros | Fiscalidad configurable | `FIS-001`, `FIS-002`, `FIS-008` | AC-FIS-01, AC-FIS-04 |
| `EP-FIS-02` IGTF, exenciones y auditoría | Cálculo y evidencia | `FIS-006`, `FIS-007`, `FIS-010` | AC-FIS-02, AC-FIS-04 |
| `EP-TES-01` Caja y sesiones | Cobrar y cerrar bien | `TES-001`, `TES-012` | AC-TES-01 |
| `EP-TES-02` Cobros, pagos y cartera | Saldos y aplicación | `TES-003`, `TES-004`, `TES-005`, `TES-011` | AC-TES-02, AC-TES-03 |
| `EP-INT-02` Migración e importación | Adoptar sin fricción | `INT-004` | AC-MIG-01 |

## 4. R3 — ÁVILA Finance

| Epic | Objetivo | Requisitos | AC |
|---|---|---|---|
| `EP-CON-01` Plan de cuentas y asientos | Contabilidad confiable | `CON-001`, `CON-002`, `CON-009` | AC-CON-01 |
| `EP-CON-02` Reportes, períodos y cierres | Cerrar el mes | `CON-003`, `CON-004`, `CON-005`, `CON-010`, `CON-011` | AC-CON-02, AC-CON-03 |
| `EP-CON-03` Contabilización automática y conciliación | Integrar operación y libro | `CON-006`, `CON-007`, `CON-008`, `CON-012` | AC-CON-04, AC-CON-05 |
| `EP-TES-03` Bancos y conciliación | Controlar el efectivo | `TES-002`, `TES-008`, `TES-009` | AC-TES-04 |
| `EP-TES-04` Multimoneda y cartera | Saldos y diferencias | `TES-006`, `TES-007`, `TES-010` | AC-TES-05 |
| `EP-FIS-03` Retenciones, libros y obligaciones | Cumplimiento fiscal | `FIS-003`, `FIS-004`, `FIS-005`, `FIS-009`, `FAC-004`, `FAC-005`, `FAC-014` | AC-FIS-03, AC-FAC-06 |
| `EP-FAC-04` Contingencia y máquina fiscal | Continuidad operativa | `FAC-009`, `FAC-012` | AC-FAC-05 |
| `EP-INT-03` Conciliación por archivo | Importar banco | `INT-008` | AC-TES-04 |

## 5. R4 — ÁVILA Retail

| Epic | Objetivo | Requisitos | AC |
|---|---|---|---|
| `EP-POS-01` Venta, turnos y hardware | Caja rápida y confiable | `POS-001`…`POS-004`, `POS-006`, `POS-007`, `POS-009`, `POS-010` | AC-POS-01, AC-POS-02, AC-POS-04 |
| `EP-POS-02` Offline, sincronización y fiscal | Operar sin internet | `POS-005`, `POS-008` | AC-POS-03, AC-POS-04 |
| `EP-INV-05` Seriales y códigos de barras | Trazabilidad unitaria | `INV-009` | AC-INV-04 |

## 6. R5 — ÁVILA People

| Epic | Objetivo | Requisitos | AC |
|---|---|---|---|
| `EP-RRH-01` Expediente, contratos y vacaciones | Base laboral | `RRH-001`…`RRH-004` | AC-RRH-01, AC-RRH-02 |
| `EP-NOM-01` Motor de nómina y conceptos | Cálculo repetible | `NOM-001`…`NOM-003`, `NOM-009` | AC-NOM-01 |
| `EP-NOM-02` Utilidades, prestaciones y liquidación | Obligaciones laborales | `NOM-004`…`NOM-007` | AC-NOM-02, AC-NOM-03 |
| `EP-NOM-03` Provisión y pago | Integrar con finanzas | `NOM-008`, `NOM-010` | AC-NOM-04 |

## 7. R6 — ÁVILA Enterprise

| Epic | Objetivo | Requisitos | AC |
|---|---|---|---|
| `EP-PRO-01` Producción y costos | Costo real de fabricar | `PRO-001`…`PRO-006` | AC-PRO-01, AC-PRO-02 |
| `EP-CRM-01` CRM y conversión | Gestionar la oportunidad | `CRM-001`…`CRM-006` | AC-CRM-01, AC-CRM-02 |
| `EP-INT-04` API pública y webhooks | Integración de partners | `INT-001`, `INT-002`, `INT-003` | AC-INT-01, AC-INT-02 |
| `EP-INT-05` E-commerce, documental y BI | Ecosistema | `INT-005`, `INT-006`, `INT-007` | AC-INT-03 |
| `EP-PLT-04` Tenant dedicado y SSO | Segmento alto | `PLT-006`, `ADM-021` | AC-PLT-03, AC-SEC-02 |

## 8. Historias iniciales por epic

Las historias se detallan en [sprint-backlog.md](sprint-backlog.md). Cada historia:

- Tiene un ID `US-<MÓD>-NNN`.
- Referencia su epic, requisito y criterio de aceptación.
- Tiene un tamaño relativo (`S`, `M`, `L`), no una estimación de calendario.
- Se considera terminada según la [definición de terminado](../CONTRIBUTING.md#12-definición-de-terminado-definition-of-done).

## 9. Reglas de priorización

1. Primero lo que desbloquea un flujo completo de negocio, no lo que "falta" aisladamente.
2. Los requisitos `Pendiente validación` no se implementan como definitivos; se prepara la estructura.
3. Un epic no se cierra si deja deuda de aislamiento, auditoría o idempotencia.
4. Los epics de R3–R6 se refinan al abrir el release; no se detallan antes.

## 10. Documentos relacionados

- [Roadmap](../docs/product/roadmap.md)
- [Releases](releases.md)
- [Sprint backlog](sprint-backlog.md)
- [Requisitos funcionales](../docs/requirements/functional-requirements.md)
