# Visión del producto — ÁVILA ERP

**Versión:** 1.0 · **Fecha:** 2026-11-08 · **Estado:** línea base aprobada para planificación

## 1. Resumen ejecutivo

ÁVILA ERP es una suite de gestión empresarial en la nube para PYMEs venezolanas. Nace con una convicción: las pequeñas y medianas empresas de Venezuela no necesitan un "sistema administrativo" que resuelva una parte del negocio y deba reemplazarse al año siguiente; necesitan una plataforma modular que cubra compra, venta, inventario, facturación fiscal, tesorería, contabilidad, punto de venta y nómina, con la posibilidad de crecer sin migrar de producto.

La estrategia es construir **la suite completa por etapas**, comenzando por el núcleo comercial y fiscal que resuelve el día a día de un distribuidor, comercio o empresa de servicios, y extendiéndola hacia finanzas, retail, personas y enterprise. La arquitectura —monolito modular multi-tenant— se diseña desde el inicio para soportar esa evolución sin sobreingeniería ni microservicios prematuros.

## 2. El problema

Las PYMEs venezolanas operan en un entorno particularmente exigente:

| Problema | Consecuencia para el negocio |
|---|---|
| Procesos dispersos en Excel, cuadernos, WhatsApp y sistemas aislados | Información duplicada, errores, cierres lentos y poca visibilidad |
| Facturación y obligaciones fiscales complejas y cambiantes | Riesgo de incumplimiento, retrabajo y dependencia de "la persona que sabe" |
| Operación con múltiples monedas y tasas de cambio volátiles | Diferencias, pérdidas por redondeo y precios desactualizados |
| Conectividad y energía intermitentes | Ventas detenidas y pérdida de datos cuando el sistema depende de internet |
| ERP tradicionales costosos, rígidos y con implementaciones largas | Barrera de entrada para empresas de 3 a 50 empleados |
| Falta de trazabilidad de inventario y cartera | Compras de más, faltantes, morosidad y decisiones a ciegas |
| Conocimiento concentrado en pocas personas | Alta dependencia de empleados clave y difícil continuidad |

El resultado es un costo oculto: horas administrativas, errores que se pagan dos veces, decisiones sin datos y una operación que no escala.

## 3. Declaración de visión

> **Que cualquier PYME venezolana pueda operar su negocio completo —vender, comprar, inventariar, facturar, cobrar, pagar, contabilizar y pagar nómina— en una sola plataforma moderna, accesible y confiable, sin importar su tamaño ni su nivel de digitalización.**

## 4. Propuesta de valor

ÁVILA ERP propone tres ideas centrales:

1. **Una sola fuente de verdad.** Cada venta, compra, movimiento de inventario, cobro, pago y asiento nace una vez y se propaga con trazabilidad. Se elimina la reconciliación manual entre sistemas.
2. **Cumplimiento sin fricción.** La facturación, los libros y las obligaciones fiscales y laborales se apoyan en reglas parametrizadas, versionadas y auditables, actualizables sin reescribir el producto.
3. **Adopción realista.** Interfaz moderna en español, operación web y PWA, POS que tolera cortes de conectividad y migración asistida desde Excel y ERP tradicionales. La curva de adopción es parte del producto.

## 5. Problemas que resuelve, por dominio

| Dominio | Problema que resuelve | Resultado esperado |
|---|---|---|
| Ventas | Cotizaciones, pedidos y facturas en hojas sueltas; precios y descuentos inconsistentes | Ciclo comercial trazable, márgenes visibles y cobro oportuno |
| Compras | Compras sin control, recepciones incompletas, costos mal calculados | Abastecimiento controlado y costo real por operación |
| Inventario | Existencias irreales, kardex manual, pérdidas y vencimientos | Stock confiable por almacén, lote y costo |
| Fiscal | Facturación empírica y riesgo de incumplimiento | Comprobantes válidos, libros y retenciones consistentes |
| Tesorería | Caja sin arqueo, cobros sin aplicar, cartera sin seguimiento | Flujo de caja y cartera bajo control |
| Contabilidad | Contabilidad "de fin de mes" y estados financieros tardíos | Cierre mensual oportuno y confiable |
| POS | Ventas lentas, cierres manuales, dependencia de internet | Caja rápida, cierre automático y operación resiliente |
| Nómina | Cálculos manuales, prestaciones y liquidaciones con riesgo | Nómina repetible, auditable y conforme a la ley vigente |
| Producción | Costos de receta y desperdicios desconocidos | Costo de producción y rentabilidad por producto |

## 6. Diferenciadores frente al ERP tradicional

| ERP tradicional | ÁVILA ERP |
|---|---|
| Instalación local, servidores y actualizaciones manuales | SaaS en la nube, actualizaciones continuas sin instalación |
| Licencias y módulos con costo de entrada alto | Suscripción mensual escalable por plan y módulos |
| Implementaciones de meses antes del primer valor | Activación guiada y primer documento emitido en días |
| Interfaces densas y anticuadas | Experiencia web moderna, responsive y PWA |
| Personalización por código y consultoría | Parametrización versionada y API pública |
| Poca tolerancia a la operación real venezolana | Multimoneda, tasas, retenciones, IGTF y POS offline como ciudadanos de primera clase |
| Datos atrapados en el proveedor | Exportación de datos y API documentada |
| Aislamiento multiempresa dudoso | Aislamiento por tenant verificado con pruebas y RLS |

> **Hipótesis a validar:** que la disposición a pagar de una PYME venezolana por una suite integrada supera la de un conjunto de herramientas baratas pero desconectadas. Ver [target-market.md](target-market.md) y [business-model.md](business-model.md).

## 7. Principios de producto

1. **Primero lo esencial, bien hecho.** Compra, venta, inventario y facturación deben ser correctos antes de agregar dominios.
2. **El producto se vende por resultados, no por módulos.** Cada release debe resolver un problema completo de un rol.
3. **Configuración antes que código.** Preferimos parámetros versionados a desarrollos a medida.
4. **La operación no se detiene.** Degradación controlada, reintentos, idempotencia y POS offline donde el negocio lo exige.
5. **Todo es auditable.** Si un usuario cambió algo sensible, se puede demostrar quién, cuándo y qué.
6. **La migración es parte del producto.** Importadores, validación y convivencia con el sistema anterior.
7. **No prometemos cumplimiento sin validación.** Lo fiscal y laboral se publica con fuente y revisión de especialista.

## 8. Qué es y qué no es ÁVILA ERP

**Es:**
- Un ERP modular SaaS para PYMEs venezolanas, multiempresa y multi-tenant.
- Una plataforma con módulos activables por plan.
- Un producto con API e integraciones para crecer más allá del ERP.

**No es (en esta etapa):**
- Un ERP para grandes corporaciones con consolidación internacional compleja.
- Un sistema de facturación electrónica certificado por sí mismo (se integra con proveedores autorizados; ver [invoicing.md](../compliance/invoicing.md)).
- Una herramienta de nómina con fórmulas publicadas sin validación legal.
- Un marketplace, un CRM puro ni una plataforma de BI.
- Un proyecto de microservicios.

## 9. Éxito de la visión

La visión se cumple cuando una PYME puede:

- Emitir su primera factura fiscal válida el primer día de uso.
- Conocer su stock, cartera y caja en tiempo real sin conciliaciones manuales.
- Cerrar su mes contable sin depender de un experto externo para reconstruir la información.
- Operar su POS durante un corte de internet sin perder ventas.
- Pagar su nómina con un cálculo repetible y auditable.
- Migrar desde su ERP anterior sin detener la operación.

Indicadores asociados: [business-model.md §8](business-model.md#8-métricas-de-negocio).

## 10. Documentos relacionados

- [Mercado objetivo](target-market.md)
- [Modelo de negocio](business-model.md)
- [Alcance funcional](functional-scope.md)
- [Roadmap](roadmap.md)
- [PRD](../requirements/PRD.md)
