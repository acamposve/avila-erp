# Revisión de consistencia documental — ÁVILA ERP

**Versión:** 1.0 · **Fecha:** 2026-11-08 · **Alcance:** línea base documental completa (47 documentos)
**Estado:** revisión cerrada con hallazgos resueltos y decisiones abiertas registradas

## 1. Propósito

Verificar que la documentación maestra sea internamente consistente, trazable y accionable antes de iniciar el desarrollo. Este informe registra:

- Lo que se revisó y cómo.
- Las inconsistencias encontradas y su resolución.
- Las decisiones que requieren aprobación.
- Los documentos que requieren revisión de un especialista.
- El siguiente paso recomendado.

## 2. Método

1. **Inventario:** verificación de la estructura solicitada y de los documentos adicionales.
2. **Enlaces:** resolución de todos los enlaces relativos entre documentos.
3. **Identificadores:** extracción y validación de `RF` (`MÓD-NNN`), `AC`, `ADR`, `DEC`, `RSK`, `SRC`, `EP` y `US`.
4. **Trazabilidad:** cada requisito tiene criterio de aceptación; cada criterio está referenciado.
5. **Contradicciones:** revisión cruzada de alcance, releases, dependencias, dinero, tenancy, facturación, contabilidad y nómina.
6. **Cumplimiento:** verificación de que ninguna tasa, fórmula o formato regulatorio se afirme sin fuente y estado.
7. **Accionabilidad:** revisión de que cada release tenga objetivo, alcance, dependencias, riesgos y criterios de salida.

## 3. Inventario y métricas

| Métrica | Valor |
|---|---|
| Documentos Markdown | **47** |
| Líneas aproximadas | ~7.600 |
| Requisitos funcionales (`RF`) | **153** |
| Requisitos no funcionales (`NFR`) | **68** |
| Criterios de aceptación (`AC`) | **67** |
| ADR | **6** |
| Decisiones abiertas (`DEC`) | **20** |
| Riesgos (`RSK`) | **27** |
| Fuentes regulatorias (`SRC`) | **16** |
| Epics | **46** |
| Historias identificadas (`US`) | **60** (incluye esbozos R3–R6) |
| Módulos con documento de dominio | 10 + administración/plataforma + fiscal |
| Enlaces relativos rotos | **0** |
| RF referenciados sin definición | **0** |
| AC referenciados sin definición | **0** |
| AC definidos sin referencia | **0** |
| RF sin AC asociado | **0** |

## 4. Hallazgos y resoluciones

| ID | Hallazgo | Documento(s) | Resolución | Estado |
|---|---|---|---|---|
| F-01 | Dos enlaces del glosario apuntaban fuera de `docs/` | [GLOSSARY.md](GLOSSARY.md) | Rutas corregidas a `compliance/` y `architecture/` | Resuelto |
| F-02 | El diagrama de dependencias de alcance invertía la dirección Ventas/Compras→Facturación y POS | [functional-scope.md](product/functional-scope.md) | Diagrama corregido y semántica `A --> B = B depende de A` declarada | Resuelto |
| F-03 | R1 incluía `PLT-004` (R2) y `PLT-006` (R6) | [roadmap.md](product/roadmap.md) | Alcance de R1 ajustado a `PLT-001…003` y `PLT-005` | Resuelto |
| F-04 | R2 incluía requisitos de facturación de R3 y omitía `TES-012` | [roadmap.md](product/roadmap.md) | Alcance R2 corregido; cierre de caja incluido | Resuelto |
| F-05 | `FAC-003` figuraba en R2 en el catálogo y en R3 en la trazabilidad | [functional-requirements.md](requirements/functional-requirements.md) | Trazabilidad corregida a R2 | Resuelto |
| F-06 | `ADM-012` no referenciaba `AC-ADM-06` | [functional-requirements.md](requirements/functional-requirements.md) | AC agregado | Resuelto |
| F-07 | Los planes comerciales incluían CRM y API antes de R6 | [business-model.md](product/business-model.md) | Empaquetado alineado a releases | Resuelto |
| F-08 | El PRD fijaba un horizonte de 12–18 meses como compromiso | [PRD.md](requirements/PRD.md) | Reemplazado por "horizonte inicial, hipótesis" | Resuelto |
| F-09 | El documento de POS se enlazaba a sí mismo | [point-of-sale.md](domain/point-of-sale.md) | Enlace reemplazado por Ventas | Resuelto |
| F-10 | `INV-R-09` afirmaba una sola transacción y a la vez un estado "En tránsito" | [inventory.md](domain/inventory.md) | Regla aclarada: tránsito informativo, sin saldo físico | Resuelto |
| F-11 | El diseño de BD prohibía FK a `admin` pero el ejemplo usaba `admin.parties` | [database-design.md](architecture/database-design.md) | Excepción documentada para maestros compartidos | Resuelto |
| F-12 | El flujo de facturación llamaba al proveedor dentro de la transacción, contra la arquitectura | [billing.md](domain/billing.md), [architecture-overview.md](architecture/architecture-overview.md) | Emisión local transaccional + autorización asíncrona; modos `RangoAutorizado` y `AutorizacionPorDocumento` | Resuelto |
| F-13 | El sprint de R2 planificaba `FAC-014` (R3) | [sprint-backlog.md](../backlog/sprint-backlog.md) | Historia retirada; exportación fiscal se planifica en R3 | Resuelto |
| F-14 | `AC-PERF-01/02` no estaban referenciados por ningún requisito no funcional | [non-functional-requirements.md](requirements/non-functional-requirements.md) | NFR de rendimiento y numeración actualizados | Resuelto |
| F-15 | Enlace a `SRC-001` con ancla inexistente | [decisions/README.md](decisions/README.md) | Enlace corregido | Resuelto |
| F-16 | El encabezado de Administración sugería implementar `PLT-004`/`PLT-006` en R1 | [administration.md](domain/administration.md) | Nota de alcance agregada | Resuelto |
| F-17 | La matriz de alcance de R1 decía "webhooks internos", confundible con `INT-003` (R6) | [functional-scope.md](product/functional-scope.md) | Cambiado a "outbox interno" | Resuelto |
| F-18 | Facturación, fiscal y tesorería listaban requisitos de R3 bajo el alcance R2 | [functional-scope.md](product/functional-scope.md) | Alcance separado explícitamente por release | Resuelto |
| F-19 | R3 listaba `TES-012` (ya en R2) y omitía `FAC-009`, `FAC-012` y `FAC-014` | [roadmap.md](product/roadmap.md) | Alcance R3 corregido y completo | Resuelto |
| F-20 | R6 prometía consolidación fuera de alcance e incluía `INT-004`/`INT-008` de releases previos | [roadmap.md](product/roadmap.md), [epics.md](../backlog/epics.md), [releases.md](../backlog/releases.md) | Consolidación marcada fuera de alcance; integraciones acotadas | Resuelto |
| F-21 | `POS-R-12` prohibía persistir datos locales, incluida la cola offline pendiente | [point-of-sale.md](domain/point-of-sale.md) | Regla aclarada: solo se conserva la cola cifrada hasta reconciliar | Resuelto |
| F-22 | `FAC-R-04` no consideraba la suma de notas de crédito parciales | [billing.md](domain/billing.md) | Invariante ajustada al acumulado | Resuelto |
| F-23 | La tabla resumen del roadmap declara R5 depende de `R1, R2`, pero releases, diagrama, `NOM-008` y `AC-NOM-04` exigen R3 | [roadmap.md](product/roadmap.md) | Propuesta T-004: cambiar la tabla a `R1, R2, R3`; no se modificó el alcance | **Pendiente de aprobación** |

## 5. Estado de consistencia

| Verificación | Resultado |
|---|---|
| Estructura solicitada completa | Sí; se agregaron `docs/README.md`, `docs/GLOSSARY.md`, `docs/documentation-review.md`, ADR-004…006 y `testing-strategy.md` |
| Un solo modelo de tenancy en todos los documentos | Sí: organización → empresa → sucursal; `tenant_id` + RLS |
| Un solo modelo de dinero y redondeo | Sí: ADR-004 referenciado por ventas, facturación, tesorería, contabilidad e inventario |
| Un solo modelo de numeración | Sí: ADR-006 referenciado por facturación, administración y contabilidad |
| Dependencias entre módulos sin ciclos | Sí; Contabilidad consume eventos y no es dependida |
| Alcance por release coherente | Sí, tras F-03/F-04/F-05/F-13 |
| Cada requisito con AC | Sí (153/153) |
| Cada AC referenciado | Sí (67/67) |
| Cada decisión bloqueante registrada | Sí (20 `DEC`) |
| Ninguna tasa/fórmula afirmada sin fuente | Sí; materias reguladas marcadas `PENDIENTE DE VALIDACIÓN LEGAL / CONTABLE` |
| Sin código, migraciones ni despliegues | Sí; el repositorio es exclusivamente documental |

## 6. Decisiones que requieren aprobación

| Decisión | Bloquea | Urgencia | Responsable |
|---|---|---|---|
| `DEC-001` Proveedor y región cloud | R1 | Alta | Arquitectura + Operaciones |
| `DEC-003` Estrategia de identidad | R1 | Alta | Arquitectura + Seguridad |
| `DEC-008` Retención y residencia de datos | R1/R2 | Alta | Legal + Seguridad |
| `DEC-002` Medio de emisión fiscal | R2 | Crítica | Producto + Legal + Fiscal |
| `DEC-006` Planes y precios | Lanzamiento R2 | Alta | Dirección + Producto |
| `DEC-019` Base de comisiones | Cierre R2 | Media | Producto + Comercial |
| `DEC-007` Retenciones IVA/ISLR | R2 parcial / R3 | Crítica | Fiscal |
| `DEC-020` Anulación de comprobantes | R2/R3 | Crítica | Legal + Fiscal |
| `DEC-009` Integración bancaria | R3 | Media | Producto + Operaciones |
| `DEC-012` Ajuste por inflación | R3 | Media | Contabilidad |
| `DEC-018` Recosteo de inventario | R3+ | Media | Contabilidad |
| `DEC-010` Emisión fiscal offline | R4 | Crítica | Legal + Producto |
| `DEC-013` Hardware POS/fiscal | R4 | Media | Producto + Soporte |
| `DEC-017` Conflictos offline | R4 | Media | Producto + Arquitectura |
| `DEC-011` Fórmulas laborales | R5 | Crítica | Laboral |
| `DEC-014` Motor de nómina | R5 | Alta | Arquitectura + Laboral |
| `DEC-015` BI y réplica de lectura | R6 | Baja | Arquitectura |
| `DEC-016` Licencia y propiedad | Público | Alta | Dirección + Legal |
| `DEC-004` Almacenamiento documental | R2 | Media | Arquitectura |
| `DEC-005` Ratificación de moneda/redondeo | R1/R2 | Media | Producto + Contabilidad |

Además, los **ADR-001…ADR-006** deben ratificarse formalmente en el consejo técnico; este documento los considera aceptados como línea base, no como aprobación corporativa.

## 7. Riesgos y validaciones pendientes

| Riesgo / validación | Acción requerida | Responsable | Release |
|---|---|---|---|
| Cambio normativo fiscal (`RSK-001`) | Vigilancia mensual y validación de la PA-0093 | Fiscal + Producto | R2 |
| Falta de validación de tasas y retenciones | Contratar especialista y registrar fuentes | Fiscal | R2/R3 |
| Fórmulas laborales no validadas (`RSK-005`, `RSK-024`) | Paquete de parámetros y casos dorados revisados | Laboral | R5 |
| Proveedor fiscal no definido (`DEC-002`) | Evaluar al menos dos proveedores y probar adaptador | Producto + Arquitectura | R2 |
| Aislamiento multi-tenant (`RSK-006`) | Batería de pruebas y auditoría antes de G1 | Arquitectura + Seguridad | R1 |
| Volatilidad cambiaria/redenominación (`RSK-002`) | Modelo de versión de moneda y simulacro | Arquitectura + Contabilidad | R2 |
| Migración de datos (`RSK-014`) | Piloto con datos reales y reporte de errores | Producto + Implementación | R2 |
| Offline fiscal (`RSK-023`, `DEC-010`) | Validación legal y procedimiento de contingencia | Legal + POS | R4 |
| Privacidad y retención (`DEC-008`) | Opinión legal y política de datos | Legal + Seguridad | R1 |
| Precios y disposición a pagar (`RSK-009`) | Entrevistas y pilotos de precio | Dirección | R2 |

## 8. Documentos que requieren revisión de un especialista

| Documento | Especialista | Qué debe validar |
|---|---|---|
| [venezuela-tax.md](compliance/venezuela-tax.md) | Fiscal/contable | IVA, ISLR, IGTF, retenciones, libros, obligaciones |
| [invoicing.md](compliance/invoicing.md) | Fiscal/legal | Medios de emisión, contenido, numeración, anulación, contingencia |
| [labor-regulations.md](compliance/labor-regulations.md) | Laboral | Prestaciones, utilidades, vacaciones, aportes, liquidación |
| [billing.md](domain/billing.md) | Fiscal/contable | Documentos, impuestos, notas, libros, retenciones |
| [payroll.md](domain/payroll.md) | Laboral | Conceptos, bases, fórmulas, provisiones |
| [accounting.md](domain/accounting.md) | Contador | Plan de cuentas base, reglas de contabilización, cierre |
| [treasury.md](domain/treasury.md) | Contador/fiscal | IGTF, diferencias cambiarias, conciliación |
| [security.md](architecture/security.md) | Legal/seguridad | Privacidad, retención, base legal de tratamiento |
| [business-model.md](product/business-model.md) | Dirección/comercial | Precios, planes, economía unitaria |
| [target-market.md](product/target-market.md) | Comercial | Segmentos, competencia, canales |

## 9. Observaciones abiertas (no bloqueantes)

| ID | Observación | Recomendación |
|---|---|---|
| O-01 | El identificador exacto de la PA-0093 y su texto oficial no están verificados | Obtener Gaceta 43.484 y confirmar con especialista antes de implementar |
| O-02 | R2 promete "reportes operativos" sin un catálogo formal de reportes | Definir catálogo mínimo y criterios de aceptación al iniciar R2 |
| O-03 | `DEC-004` (almacenamiento documental) sigue abierto | Resolver antes de implementar PDFs y adjuntos |
| O-04 | CRM no tiene documento de dominio propio | Crear `docs/domain/crm.md` antes de R6 |
| O-05 | No existen aún OpenAPI, wireframes ni design system implementado | Producir en Sprint 0/R1; no son parte de esta línea base |
| O-06 | La política de conflictos offline estaba implícita | Registrada como `DEC-017` |
| O-07 | La base de comisiones estaba implícita | Registrada como `DEC-019` |
| O-08 | El recosteo de inventario no estaba decidido | Registrado como `DEC-018` |

## 10. Recomendación de siguiente paso

**No iniciar el desarrollo funcional de R1 hasta cerrar la compuerta G0.** El orden recomendado es:

1. **Aprobar las decisiones bloqueantes de R1:** `DEC-001`, `DEC-003`, `DEC-008`.
2. **Ratificar los ADR-001…ADR-006** y registrar la aprobación en [decisions/README.md](decisions/README.md).
3. **Contratar y briefear a los especialistas** fiscal/contable y laboral; entregarles los documentos del §8 y un cuestionario cerrado.
4. **Resolver `DEC-002`:** evaluar dos proveedores de emisión, validar el modo (`RangoAutorizado` vs `AutorizacionPorDocumento`) y construir un spike de adaptador.
5. **Ejecutar Sprint 0:** repositorio, CI, PostgreSQL con RLS, numeración, outbox, observabilidad, threat model y design system.
6. **Construir una rebanada vertical:** organización → empresa → tercero → artículo → cotización → pedido → factura en borrador (sin emisión fiscal), para validar límites de módulos, transacciones, permisos y auditoría.
7. **Solo después**, abrir R1 formal y avanzar a R2 con el piloto.

Criterio de éxito del siguiente paso: **G0 cerrada, ADR ratificados, especialistas contratados y spike de emisión validado**.

## 11. Registro de verificación

| Fecha | Verificación | Resultado |
|---|---|---|
| 2026-11-08 | Conteo de documentos | 47 archivos Markdown |
| 2026-11-08 | Enlaces relativos | 0 rotos |
| 2026-11-08 | IDs `RF` | 153 definidos; 0 referencias indefinidas |
| 2026-11-08 | IDs `AC` | 67 definidos; 0 referencias indefinidas; 0 sin referencia |
| 2026-11-08 | Cobertura RF → AC | 153/153 |
| 2026-11-08 | Decisiones y riesgos | 20 `DEC`, 27 `RSK`, 16 `SRC` |
| 2026-11-08 | Código/migraciones/despliegues | Ninguno; repositorio documental |

Las verificaciones se ejecutaron con scripts locales de resolución de rutas y extracción de identificadores. No se realizaron commits, despliegues ni operaciones destructivas.

## 12. Historial de cambios

| Versión | Fecha | Cambio |
|---|---|---|
| 1.0 | 2026-11-08 | Línea base documental y revisión inicial de consistencia |
