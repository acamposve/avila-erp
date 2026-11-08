# Mercado objetivo — ÁVILA ERP

**Versión:** 1.0 · **Fecha:** 2026-11-08 · **Estado:** hipótesis de mercado a validar

> Este documento describe segmentos, dolores y canales como **hipótesis de investigación**. Las cifras de mercado, competencia y disposición a pagar deben validarse con entrevistas, pilotos y datos de campo antes de convertirse en compromisos comerciales.

## 1. Contexto del mercado venezolano

La operación de una PYME en Venezuela combina factores que condicionan el producto:

| Factor | Implicación para ÁVILA |
|---|---|
| Uso extendido de monedas extranjeras junto al bolívar | Multimoneda, tasas con vigencia y diferencias cambiarias |
| Variación de precios y costos | Listas de precios versionadas, recálculo de costos y márgenes visibles |
| Normativa fiscal cambiante y con requisitos de emisión | Motor de reglas versionado, integración con medios autorizados y trazabilidad |
| Conectividad eléctrica y de internet intermitente | PWA, POS offline, reintentos e idempotencia |
| Banca con integraciones limitadas | Importación de estados de cuenta, conciliación asistida y medios de pago configurables |
| Informalidad y baja madurez digital | Onboarding guiado, importación desde Excel, plantillas y capacitación |
| Desconfianza hacia software local y proveedores pequeños | Transparencia de datos, exportación, SLA y seguridad demostrable |
| Presencia de ERP tradicionales con alto costo de licencia e implementación | Propuesta de entrada accesible y crecimiento por módulos |

**PENDIENTE:** dimensionar el mercado (TAM/SAM/SOM) con fuentes oficiales (INE, Conindustria, Consecomercio, gremios) y entrevistas. No se publican cifras sin estudio.

## 2. Cliente ideal inicial (ICP)

| Atributo | Perfil |
|---|---|
| Tamaño | 3 a 50 empleados |
| Facturación | Operación con ventas recurrentes y compras a proveedores |
| Geografía | Venezuela; inicialmente ciudades con mayor densidad empresarial |
| Madurez digital | Baja o media: Excel, sistemas contables aislados o ERP antiguo |
| Procesos | Compra, almacenamiento, venta, facturación, cobro y pago |
| Dolores | Stock poco confiable, cartera desordenada, facturación con riesgo, cierres lentos |
| Decisor | Dueño/gerente general; influenciado por contador y administrador |
| Presupuesto | Sensible al precio, pero con costo real alto por errores y horas administrativas |

## 3. Segmentos prioritarios

### 3.1 Distribuidores y mayoristas (primario, R2)
- **Perfil:** compran a fabricantes/importadores y venden a comercios; manejan volumen, listas de precios, crédito y logística.
- **Dolores:** control de stock por almacén, precios y descuentos por cliente, cartera y cobranza, costos reales, devoluciones.
- **Funciones críticas:** compras, inventario con lotes, ventas con crédito, facturación, cuentas por cobrar, comisiones.
- **Criterio de compra:** confiabilidad del inventario y de la facturación; capacidad de migrar datos.

### 3.2 Comercios minoristas (primario, R2/R4)
- **Perfil:** tiendas, ferreterías, farmacias, abastos, tecnología; venta de mostrador y volumen.
- **Dolores:** velocidad de caja, códigos de barras, control de caja, cierres, faltantes, promociones.
- **Funciones críticas:** POS, inventario, precios, medios de pago, facturación.
- **Criterio de compra:** rapidez, simplicidad y tolerancia a cortes de internet.

### 3.3 Empresas de servicios (primario, R2/R3)
- **Perfil:** consultoría, mantenimiento, salud, educación, servicios profesionales; venden proyectos y horas.
- **Dolores:** cotizaciones, órdenes de servicio, facturación recurrente, cobranza, rentabilidad por cliente.
- **Funciones críticas:** ventas, facturación, cuentas por cobrar, gastos, contabilidad.
- **Criterio de compra:** facilidad de facturación y control de cartera.

### 3.4 Pequeñas industrias (secundario, R6)
- **Perfil:** producción por receta u orden, consumo de insumos, mano de obra.
- **Dolores:** costo real de producción, desperdicios, planificación.
- **Funciones críticas:** inventario, producción, costos, compras, ventas.
- **Criterio de compra:** costo por producto confiable.

## 4. Personas y roles

| Persona | Objetivos | Dolores | Funciones que usa |
|---|---|---|---|
| **Dueño / Gerente** | Visibilidad, rentabilidad, control | No sabe si gana dinero; depende de reportes tardíos | Dashboards, ventas, cartera, caja, reportes |
| **Administrador** | Orden operativo y cumplimiento | Procesos manuales, errores, cierres | Compras, inventario, facturación, tesorería |
| **Contador** | Registros correctos y oportunos | Información incompleta, retenciones, libros | Contabilidad, fiscal, reportes, conciliación |
| **Vendedor** | Cerrar ventas rápido | Precios desactualizados, stock incierto, comisiones | Cotizaciones, pedidos, clientes, comisiones |
| **Almacenista** | Inventario exacto | Diferencias, recepciones incompletas, ubicaciones | Recepción, transferencias, conteos, kardex |
| **Cajero** | Cobrar rápido y cerrar bien | Lentitud, fallas de conexión, arqueos | POS, caja, medios de pago |

## 5. Dolores transversales y prioridad

| Dolor | Frecuencia | Impacto | Prioridad para ÁVILA |
|---|---|---|---|
| Inventario poco confiable | Alta | Alto | R2 — crítico |
| Facturación con riesgo fiscal | Alta | Muy alto | R2 — crítico |
| Cartera y cobranza sin control | Alta | Alto | R2/R3 — crítico |
| Precios y costos desactualizados | Alta | Alto | R2 |
| Cierres contables tardíos | Media | Alto | R3 |
| Caja lenta y sin arqueo | Media (retail) | Alto | R4 |
| Nómina manual | Media | Muy alto | R5 |
| Costo de producción desconocido | Baja (industria) | Alto | R6 |

## 6. Competencia y alternativas

> **Hipótesis de posicionamiento.** La competencia debe validarse con investigación de campo; no se afirman cuotas ni precios de terceros.

| Categoría | Ejemplos de referencia | Fortaleza percibida | Debilidad percibida | Respuesta de ÁVILA |
|---|---|---|---|---|
| ERP tradicional venezolano | Profit Plus y suites contables locales | Cobertura funcional, presencia, red de contadores | Costo, rigidez, interfaces antiguas, implementación pesada | SaaS moderno, módulos escalables, migración asistida |
| Sistemas de facturación / POS | Software de facturación y puntos de venta | Precio bajo, rapidez de venta | Poca cobertura contable/inventario | Suite integrada, POS incluido en el plan adecuado |
| Contabilidad en la nube | Herramientas contables y de facturación | Facilidad de uso | Débil en inventario, compras y operación | Núcleo operativo + contabilidad |
| Excel y procesos manuales | Hojas de cálculo | Flexibilidad, costo cero | Errores, sin trazabilidad, no escala | Migración guiada y valor inmediato |
| ERP internacionales | Soluciones globales | Madurez | Precio, localización fiscal/laboral, soporte | Localización venezolana profunda |

## 7. Estrategia de adquisición

### Canales iniciales (hipótesis)
1. **Venta directa consultiva** para distribuidores y comercios con operación compleja.
2. **Red de contadores y consultores** como canal de recomendación e implementación.
3. **Contenido y comunidad** (guías fiscales, plantillas, webinars) para captación inbound.
4. **Alianzas con proveedores** de facturación, hardware POS y servicios de pago.
5. **Programa de referidos** de clientes satisfechos.

### Motion comercial
- Diagnóstico breve → demo con datos del prospecto → prueba guiada → migración → acompañamiento de cierre del primer mes.
- El **contador** es un actor decisivo: el producto debe facilitar su trabajo, no reemplazarlo.

### Objeciones esperadas
| Objeción | Respuesta de producto |
|---|---|
| "Ya tengo un sistema" | Migración asistida, importadores y convivencia temporal |
| "Es caro" | Plan de entrada acotado; costo oculto de la operación manual |
| "No tengo internet estable" | PWA y POS offline con sincronización |
| "¿Mis datos están seguros?" | Aislamiento por tenant, cifrado, auditoría, exportación y SLA |
| "¿Cumple con el SENIAT?" | Medios de emisión autorizados y validación con especialista; no promesas sin respaldo |
| "Mi contador no lo conoce" | Modo contador, exportaciones y capacitación |

## 8. Estrategia de migración desde otros ERP

1. **Descubrimiento:** inventario de datos (clientes, proveedores, artículos, saldos, documentos abiertos).
2. **Mapeo y limpieza:** plantillas de importación, validación, detección de duplicados.
3. **Carga:** importadores por lotes con reporte de errores y idempotencia.
4. **Saldos iniciales:** apertura de inventario, cartera, caja y contabilidad en fecha de corte.
5. **Convivencia:** período de operación en paralelo con control de diferencias.
6. **Cierre:** certificación de saldos y baja del sistema anterior (cuando el cliente lo decida).

Ver requisitos `INT-004`, `ADM-016` y la épica de migración en [backlog/epics.md](../../backlog/epics.md).

## 9. Plan de validación de mercado

| Experimento | Objetivo | Evidencia esperada |
|---|---|---|
| 20 entrevistas a dueños y contadores | Confirmar dolores y disposición a pagar | Patrones repetidos, rangos de precio aceptados |
| 5 pilotos con datos reales | Validar migración y operación diaria | Tiempo a primera factura, errores, feedback |
| Landing + lista de espera | Medir interés y mensaje | Tasa de conversión por segmento |
| Prueba de canal contador | Validar recomendación | Referidos y conversión asistida |
| Análisis de competencia | Precios y cobertura | Matriz funcional y de precios validada |

## 10. Riesgos de mercado

| ID | Riesgo | Mitigación |
|---|---|---|
| RSK-009 | Baja disposición a pagar | Plan de entrada, prueba, ROI demostrado, precios por valor |
| RSK-010 | Competencia con precios agresivos | Diferenciación por integración, servicio y confiabilidad |
| RSK-011 | Dependencia de contadores | Programa de aliados y herramientas para contadores |
| RSK-012 | Baja adopción por falta de hábitos digitales | Onboarding, plantillas, capacitación y soporte cercano |
| RSK-013 | Concentración geográfica | Expansión por ciudades y canales remotos |

## 11. Documentos relacionados

- [Visión](vision.md)
- [Modelo de negocio](business-model.md)
- [Roadmap](roadmap.md)
- [PRD](../requirements/PRD.md)
