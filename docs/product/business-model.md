# Modelo de negocio — ÁVILA ERP

**Versión:** 1.0 · **Fecha:** 2026-11-08 · **Estado:** hipótesis comercial; precios y empaquetado **no validados**

> Nada de este documento constituye una lista de precios aprobada. Los planes, límites y métricas son hipótesis para validar con clientes y con el equipo comercial. Ver `DEC-006` en [decisiones](../decisions/README.md).

## 1. Modelo general

ÁVILA ERP es un **SaaS B2B por suscripción mensual**, con:

- **Organización** como unidad de contratación y de aislamiento (tenant).
- **Planes** que combinan módulos, límites y nivel de soporte.
- **Add-ons** para módulos especializados (POS, nómina, producción, BI/API).
- **Servicios** de onboarding, migración y capacitación vendidos por separado.
- **Canal de aliados** (contadores, consultores, implementadores) con comisión o margen.

```text
Cliente → Organización (tenant) → Plan + Add-ons → Suscripción mensual
                                     ├── límites (empresas, sucursales, usuarios, documentos)
                                     ├── módulos activos (feature flags)
                                     └── soporte y SLA
```

## 2. Fuentes de ingreso

| Fuente | Descripción | Madurez |
|---|---|---|
| Suscripción SaaS | Pago mensual/anual por plan y add-ons | Núcleo desde R2 |
| Onboarding y migración | Implementación inicial, carga de datos, capacitación | R2 |
| Soporte premium | SLA extendido, canal dedicado, horas de consultoría | R2/R3 |
| Módulos add-on | POS por caja, nómina por empleado, producción, BI/API | R4–R6 |
| Tenant dedicado | Infraestructura aislada, requisitos enterprise | R6 |
| Integraciones a medida | Conectores privados, desarrollo facturado | R6 |
| Certificación de partners | Formación y licencia de implementadores | R3+ |

**No se contempla** venta de datos de clientes, publicidad ni licenciamiento perpetuo en la estrategia inicial.

## 3. Hipótesis de planes

> Los límites son orientativos y deben calibrarse con uso real. Los precios se definen en `DEC-006` después de la validación.

| Plan | Cliente objetivo | Módulos incluidos | Límites orientativos | Soporte |
|---|---|---|---|---|
| **Esencial** | Comercio o servicio pequeño que inicia | ADM, VEN, COM, INV, FAC, TES básico | 1 empresa, 1 sucursal, 5 usuarios, 1 almacén | Autoservicio + correo |
| **Profesional** | Distribuidor o empresa en crecimiento | Esencial + CON + TES completo; CRM en R6 | 3 empresas, 3 sucursales, 15 usuarios | Correo + chat en horario |
| **Empresa** | Operación multisucursal | Profesional + POS + RRHH/Nómina (add-on); API en R6 | Empresas/sucursales ampliables, 50 usuarios | SLA, gestor de cuenta |
| **Enterprise** | Organización mayor o regulada | Suite completa + SSO + tenant dedicado + BI | A convenir | SLA dedicado |

### Add-ons previstos

| Add-on | Unidad de cobro (hipótesis) | Release |
|---|---|---|
| POS | Por caja/terminal activa | R4 |
| Nómina | Por empleado procesado al mes | R5 |
| Producción | Por empresa con producción | R6 |
| BI y reportes avanzados | Por organización | R6 |
| API e integraciones | Por volumen de llamadas o empresa | R6 |
| Almacenamiento documental | Por GB adicional | R6 |

### Principios de empaquetado

1. **Un plan debe resolver un negocio completo**, no obligar a comprar cinco módulos para facturar.
2. **El límite se mide en unidades comprensibles** (usuarios, empresas, sucursales, comprobantes), no en "créditos".
3. **La facturación fiscal no se limita por documento** en los planes base: limitar la facturación presiona al cliente a operar fuera del sistema.
4. **Los add-ons se activan sin migración** y con prorrateo.
5. **El downgrade no destruye datos**; solo bloquea creación por encima del límite.

## 4. Estructura de costos (cualitativa)

| Componente | Naturaleza | Palanca |
|---|---|---|
| Infraestructura cloud | Variable por tenant y uso | Arquitectura eficiente, instancias compartidas, almacenamiento por capas |
| Base de datos y respaldos | Variable | Índices, particionado, retención |
| Soporte | Fijo + variable | Autoservicio, base de conocimiento, telemetría |
| Desarrollo | Fijo | Reutilización modular, reglas parametrizadas |
| Cumplimiento y validación | Fijo | Revisión de especialistas, auditorías |
| Canal | Variable | Comisión de aliados |
| Facturación y cobranza | Variable | Pasarela local, conciliación |

## 5. Economía unitaria (hipótesis a validar)

| Métrica | Definición | Objetivo hipotético |
|---|---|---|
| ARPU | Ingreso medio por organización al mes | Validar contra disposición a pagar |
| CAC | Costo de adquisición por cliente | Recuperar en menos de 12 meses |
| LTV | Valor de vida del cliente | LTV/CAC > 3 |
| Churn mensual | Bajas / clientes activos | < 2 % mensual |
| NRR | Retención neta de ingresos | > 100 % anual |
| Margen bruto | Ingreso menos infraestructura y soporte directo | > 70 % |
| Costo de migración | Horas de implementación por cliente | Disminuir con plantillas y autoservicio |

Estos valores son **supuestos de planificación**, no resultados. Se revisan trimestralmente con datos reales.

## 6. Ciclo de vida del cliente

```text
Visita / referido → Diagnóstico → Demo → Prueba (14–30 días)
   → Contratación → Onboarding + migración → Primera factura
   → Adopción (uso semanal) → Expansión (add-ons) → Renovación
```

| Etapa | Objetivo | Métrica |
|---|---|---|
| Activación | Primera factura emitida y primer cobro registrado | % de cuentas activadas en 30 días |
| Adopción | Uso recurrente de ventas, inventario y caja | Usuarios activos semanales / usuarios licenciados |
| Valor | Cierre mensual y reportes usados | % de meses con cierre contable |
| Expansión | POS, nómina, contabilidad, sucursales | Add-ons por cuenta |
| Retención | Renovación y bajo churn | Churn, NRR |

## 7. Estrategia de adquisición y canales

1. **Venta directa** (fundador/ejecutivo comercial) en el segmento inicial: alto aprendizaje, ticket medio.
2. **Red de contadores y consultores**: formación, licencia de partner, comisión recurrente.
3. **Contenido educativo** sobre facturación, inventario y gestión en Venezuela.
4. **Alianzas** con proveedores de facturación digital, hardware POS y pasarelas de pago.
5. **Referidos** con beneficio en la renovación.
6. **Inside sales** para planes Esencial y Profesional.

**Estrategia de migración:** ver [target-market.md §8](target-market.md#8-estrategia-de-migración-desde-otros-erp). Se ofrece como servicio pagado y como herramienta autoservicio con plantillas.

## 8. Métricas de negocio

### Crecimiento y retención
- MRR, ARR, nuevo MRR, expansión, contracción, churn de logos y de ingresos.
- NRR y GRR.
- CAC por canal y payback.
- Pipeline por segmento y tasa de cierre.

### Producto y adopción
- Tiempo a la primera factura (TTFV).
- Documentos emitidos por organización/mes.
- Usuarios activos semanales.
- Módulos activos por cuenta.
- Tasa de error de migración.
- Tickets por cuenta y por módulo.
- Uptime, latencia p95 y errores 5xx.

### Calidad y confianza
- Incidentes por severidad y tiempo de resolución.
- Diferencias de inventario detectadas en conteos.
- Diferencias de conciliación contable.
- Incidentes de aislamiento entre tenants (objetivo: cero).

## 9. Estrategia de precios (por definir)

**DEC-006** debe responder:

- ¿Precio fijo por plan o por usuario con mínimo?
- ¿Moneda de cobro (USD/VES) y mecanismo de actualización por inflación/tasa?
- ¿Ciclo mensual, anual con descuento o ambos?
- ¿Prueba gratuita, demo pagada o implementación pagada?
- ¿Descuento por pago anual o por contrato?
- ¿Cómo se cobra el add-on de POS: por caja, por sucursal o por volumen?
- ¿Qué pasa con el cliente que supera el límite: aviso, bloqueo o cobro automático?

Criterios para decidir: disposición a pagar validada, costo de servir, comparabilidad con alternativas y simplicidad de la propuesta.

## 10. Riesgos comerciales

| ID | Riesgo | Mitigación |
|---|---|---|
| RSK-009 | Baja disposición a pagar | Pilotos de precio, plan de entrada, ROI documentado |
| RSK-010 | Competencia con precios agresivos | Integración y servicio diferenciados |
| RSK-011 | Dependencia del canal contador | Programa de aliados y herramientas propias |
| RSK-019 | Canibalización entre planes | Límites claros y valor por segmento |
| RSK-020 | Impago y morosidad | Cobro anticipado, suspensión gradual, gestión de cobranza |

## 11. Documentos relacionados

- [Mercado objetivo](target-market.md)
- [Visión](vision.md)
- [Alcance funcional](functional-scope.md)
- [Roadmap](roadmap.md)
- [Decisiones abiertas](../decisions/README.md)
