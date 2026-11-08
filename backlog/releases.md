# Releases — Alcance y criterios de salida

**Versión:** 1.0 · **Fecha:** 2026-11-08 · **Sin fechas comprometidas**

## 1. Modelo de release

Un release se declara terminado cuando:

1. Cumple sus **criterios de salida**.
2. Sus historias están en `Hecho` según la definición de terminado.
3. Los `AC` asociados tienen evidencia.
4. Las validaciones externas requeridas están documentadas.
5. No hay defectos abiertos de severidad crítica o alta sin mitigación aprobada.

El contenido puede ajustarse; lo que no se ajusta sin decisión es la **calidad de salida**.

## 2. R1 — ÁVILA Core

| Campo | Detalle |
|---|---|
| Objetivo | Plataforma multi-tenant segura, operable y auditable |
| Módulos | `PLT`, `ADM` |
| Dependencias | `DEC-001`, `DEC-003`, `DEC-008` |
| Riesgos | RSK-006, RSK-008, RSK-015, RSK-018 |
| Epics | `EP-PLT-01`…`03`, `EP-ADM-01`…`05`, `EP-INT-01` |
| Requisitos | `PLT-001`…`PLT-003`, `PLT-005`; `ADM-001`…`ADM-020` |

**Criterios de salida**

- [ ] Aprovisionamiento completo de organización, empresa, sucursal y administrador.
- [ ] Aislamiento verificado entre dos tenants en todos los recursos y jobs.
- [ ] RLS activa, probada y auditada; rol de aplicación sin privilegios.
- [ ] RBAC con alcance por empresa/sucursal/almacén y pruebas de autorización.
- [ ] MFA, sesiones y revocación operativas.
- [ ] Auditoría append-only consultable y exportable.
- [ ] Numeración concurrente sin duplicados ni huecos.
- [ ] Exportación completa de datos de un tenant.
- [ ] Respaldos y restauración ensayados.
- [ ] Observabilidad end-to-end y alertas básicas.
- [ ] CI/CD con pruebas, migraciones y escaneo de seguridad.
- [ ] ADR fundacionales aprobados.

**No vendible de forma independiente.**

## 3. R2 — ÁVILA Comercial

| Campo | Detalle |
|---|---|
| Objetivo | Operar el ciclo comercial y fiscal de una PYME |
| Módulos | `INV`, `VEN`, `COM`, `FAC`, `FIS` (base), `TES` (básico), `INT` (importación) |
| Dependencias | R1; `DEC-002`, `DEC-004`, `DEC-005`, `DEC-006`; validación fiscal |
| Riesgos | RSK-001, RSK-002, RSK-004, RSK-014, RSK-021 |
| Epics | `EP-INV-01`…`04`, `EP-VEN-01`…`03`, `EP-COM-01`…`02`, `EP-FAC-01`…`03`, `EP-FIS-01`…`02`, `EP-TES-01`…`02`, `EP-INT-02` |

**Criterios de salida**

- [ ] Un piloto emite facturas reales durante un mes sin correcciones manuales.
- [ ] Inventario cuadra contra conteo físico en un almacén.
- [ ] Factura, nota de crédito y cartera conciliadas.
- [ ] Cierre de caja con arqueo.
- [ ] Migración de datos del piloto con reporte de errores.
- [ ] Aislamiento y concurrencia verdes en CI.
- [ ] Validación externa de documentos fiscales.
- [ ] Manual de usuario y onboarding.
- [ ] Precios y planes definidos (`DEC-006`).

**Este es el primer release comercializable.**

## 4. R3 — ÁVILA Finance

| Campo | Detalle |
|---|---|
| Objetivo | Cerrar la contabilidad y profesionalizar tesorería |
| Módulos | `CON`, `TES` completo, `FIS` completo, `FAC` avanzado, `INT` |
| Dependencias | R2; `DEC-007`, `DEC-009`, `DEC-012`; validación contable |
| Riesgos | RSK-005, RSK-017, RSK-022 |
| Epics | `EP-CON-01`…`03`, `EP-TES-03`…`04`, `EP-FIS-03`, `EP-FAC-04`, `EP-INT-03` |

**Criterios de salida**

- [ ] Asientos automáticos de factura, nota, cobro, pago, compra y ajuste.
- [ ] Balanza cuadrada y conciliación subledger-GL sin diferencias.
- [ ] Cierre mensual realizado por un contador externo en el piloto.
- [ ] Libros y retenciones revisados por especialista.
- [ ] Conciliación bancaria con archivo real.
- [ ] Reversión de asientos y período bloqueado probados.

## 5. R4 — ÁVILA Retail

| Campo | Detalle |
|---|---|
| Objetivo | Caja rápida, hardware y operación offline |
| Módulos | `POS`, `INV` (seriales), `FAC`, `TES` |
| Dependencias | R2; `DEC-010`, `DEC-013`; validación fiscal de offline |
| Riesgos | RSK-003, RSK-016, RSK-023 |
| Epics | `EP-POS-01`…`02`, `EP-INV-05` |

**Criterios de salida**

- [ ] Venta sin internet y sincronización sin pérdida ni duplicados.
- [ ] Cierre Z cuadra con ventas y medios de pago.
- [ ] Factura fiscal emitida desde POS.
- [ ] Hardware de la matriz validado.
- [ ] Procedimiento de contingencia documentado y aprobado.

## 6. R5 — ÁVILA People

| Campo | Detalle |
|---|---|
| Objetivo | Nómina y gestión laboral auditables |
| Módulos | `RRH`, `NOM`, `TES`, `CON` |
| Dependencias | R1/R2/R3; `DEC-011`, `DEC-014`; validación laboral |
| Riesgos | RSK-005, RSK-024, RSK-025 |
| Epics | `EP-RRH-01`, `EP-NOM-01`…`03` |

**Criterios de salida**

- [ ] Nómina de piloto validada por especialista.
- [ ] Recálculo determinista y trazable.
- [ ] Prestaciones y utilidades conciliadas.
- [ ] Liquidación completa.
- [ ] Provisión y pago integrados con contabilidad y tesorería.
- [ ] Datos sensibles protegidos y auditados.

## 7. R6 — ÁVILA Enterprise

| Campo | Detalle |
|---|---|
| Objetivo | Producción, CRM, integraciones y clientes mayores |
| Módulos | `PRO`, `CRM`, `INT`, `PLT` (dedicado), `CON` (reportes multiempresa, sin consolidación) |
| Dependencias | R2/R3; `DEC-015`, `DEC-016` |
| Riesgos | RSK-007, RSK-015, RSK-026, RSK-027 |
| Epics | `EP-PRO-01`, `EP-CRM-01`, `EP-INT-04`…`05`, `EP-PLT-04` |

**Criterios de salida**

- [ ] API pública documentada, versionada y con sandbox.
- [ ] Partner externo integrado usando solo la API.
- [ ] Orden de producción costeada y conciliada.
- [ ] CRM convierte oportunidad en venta.
- [ ] Tenant dedicado aprovisionado con el mismo código.
- [ ] SSO operativo para un cliente enterprise.

## 8. Secuencia y dependencias

| Release | Depende de | Habilita |
|---|---|---|
| R1 | Decisiones base | R2…R6 |
| R2 | R1 | R3, R4, R6 |
| R3 | R2 | R5, R6 |
| R4 | R2 | R6 |
| R5 | R1, R2, R3 | — |
| R6 | R2, R3 | — |

## 9. Gestión de cambios

1. Un cambio de alcance actualiza este archivo, [epics.md](epics.md), [roadmap.md](../docs/product/roadmap.md) y [functional-scope.md](../docs/product/functional-scope.md).
2. Un release no se retrasa "en silencio": se recorta alcance o se registra el riesgo.
3. Las validaciones externas se planifican antes del cierre, no después.
4. Las fechas, si se comunican, son compromisos explícitos de dirección y no parte de este documento.

## 10. Documentos relacionados

- [Roadmap](../docs/product/roadmap.md)
- [Epics](epics.md)
- [Sprint backlog](sprint-backlog.md)
- [Criterios de aceptación](../docs/requirements/acceptance-criteria.md)
