# Criterios de aceptación — ÁVILA ERP

**Versión:** 1.0 · **Fecha:** 2026-11-08 · **Estado:** línea base

Cada criterio es verificable y está asociado a requisitos en [functional-requirements.md](functional-requirements.md). El formato es **Dado / Cuando / Entonces**, con la evidencia esperada. Un criterio se cumple solo con evidencia reproducible (prueba automatizada, informe o demostración registrada).

## 1. Plataforma SaaS

| ID | Dado | Cuando | Entonces | Verificación |
|---|---|---|---|---|
| AC-PLT-01 | Una organización con plan Esencial | Intenta activar un módulo o exceder un límite no contratado | El sistema bloquea la acción con mensaje claro, sin perder datos, y registra el intento | Prueba E2E + API |
| AC-PLT-02 | Una organización en prueba | Se registra y completa el onboarding | Se aprovisiona automáticamente, se mide el uso y se notifica al responsable | Prueba E2E |
| AC-PLT-03 | Un operador de plataforma con rol de soporte | Accede a un tenant con justificación | El acceso queda auditado, es de solo lectura por defecto y expira | Prueba + consulta de auditoría |

## 2. Administración

| ID | Dado | Cuando | Entonces | Verificación |
|---|---|---|---|---|
| AC-ADM-01 | Una organización nueva | Se aprovisiona con empresa y sucursal | Queda operativa con datos iniciales, preferencias y sin acceso a otros tenants | Integración + aislamiento |
| AC-ADM-02 | Un usuario existente en la organización A | Es invitado a la organización B | Puede cambiar de contexto; sus permisos y datos se resuelven por membresía y no se mezclan | Integración |
| AC-ADM-03 | Un usuario sin permiso `inventory.stock.adjust` | Intenta ajustar stock por API | Recibe 403, no se modifica el stock y se audita el intento | Prueba de autorización |
| AC-ADM-04 | Una operación sensible (anulación, cambio de rol, acceso de soporte) | Se ejecuta | Se registra actor, tenant, antes/después, fecha y correlación; el registro es inmutable | Consulta de auditoría |
| AC-ADM-05 | Una empresa sin perfil fiscal completo | Intenta emitir una factura | La emisión se bloquea con lista de campos faltantes | Prueba negativa |
| AC-ADM-06 | Series configuradas para dos sucursales | Se emiten documentos concurrentemente | Cada serie conserva su secuencia y no hay cruces entre sucursales | Prueba de concurrencia |
| AC-ADM-07 | Una organización suspendida | Un usuario intenta escribir | La escritura se bloquea; lectura y exportación se permiten según política y queda auditado | Prueba E2E |

## 3. Inventario

| ID | Dado | Cuando | Entonces | Verificación |
|---|---|---|---|---|
| AC-INV-01 | Un catálogo con artículos, servicios, variantes y UoM | Se crea una venta con conversión de unidad | El stock se descuenta en la unidad de inventario correcta y el precio se conserva | Integración |
| AC-INV-02 | Un almacén con movimientos | Se consulta existencias y kardex | El saldo coincide con la suma de movimientos y el kardex muestra origen, documento y costo | Prueba de dominio |
| AC-INV-03 | Dos almacenes de la misma empresa | Se transfiere stock con aprobación | Se registra salida, tránsito y entrada; el total de la empresa no cambia | Integración |
| AC-INV-04 | Un artículo con lote y vencimiento | Se vende y se devuelve | El lote se conserva en ambas operaciones y el vencimiento se controla | Prueba de dominio |
| AC-INV-05 | Compras a distinto costo | Se recibe y luego se vende | El costo promedio se recalcula correctamente y el margen usa el costo vigente | Prueba con caso dorado |
| AC-INV-06 | Stock disponible 10 y reservas por 8 | Se confirma un pedido por 5 con política de no negativo | La operación se rechaza o se autoriza según permiso, sin dejar stock inconsistente | Prueba de concurrencia |

## 4. Ventas

| ID | Dado | Cuando | Entonces | Verificación |
|---|---|---|---|---|
| AC-VEN-01 | Una cotización vigente | Se convierte en pedido | Se conservan líneas, precios y cliente; se reserva stock y se registra el vínculo | Integración |
| AC-VEN-02 | Un descuento superior al umbral | Un vendedor sin permiso lo aplica | El sistema exige aprobación; sin ella no se confirma y queda registro | Prueba de flujo |
| AC-VEN-03 | Un cliente con límite de crédito y saldo | Se confirma un pedido que excede el límite | Se bloquea o requiere aprobación; la comisión se calcula sobre la base correcta | Prueba de dominio |
| AC-VEN-04 | Una factura emitida | Se registra una devolución total | Se genera nota de crédito, se revierte la comisión, reingresa el stock y se actualiza la cartera | Integración E2E |

## 5. Compras

| ID | Dado | Cuando | Entonces | Verificación |
|---|---|---|---|---|
| AC-COM-01 | Una orden de compra sobre el umbral | Se intenta confirmar sin aprobación | Se bloquea y se notifica al aprobador | Prueba de flujo |
| AC-COM-02 | Una orden por 100 y recepción por 100 | Llega factura por 105 con tolerancia 2 % | Se marca diferencia, se bloquea el registro automático y se permite excepción aprobada | Prueba de tres vías |
| AC-COM-03 | Una compra en divisas | Se registra la factura | Se guarda moneda, tasa, fuente y fecha; el costo en moneda funcional es trazable | Prueba de dominio |

## 6. Facturación

| ID | Dado | Cuando | Entonces | Verificación |
|---|---|---|---|---|
| AC-FAC-01 | Una empresa y sucursal configuradas | Se emiten 10.000 facturas concurrentes | No hay números duplicados ni saltos no justificados; cada factura conserva su serie | Prueba de estrés |
| AC-FAC-02 | Una factura con líneas gravadas, exentas y descuento | Se calcula | Impuestos por línea y totales cuadran según reglas vigentes; el redondeo es reproducible | Caso dorado |
| AC-FAC-03 | Una factura emitida | Se emite una nota de crédito parcial | El original permanece inmutable, la nota referencia la factura y los saldos se ajustan | Prueba de dominio |
| AC-FAC-04 | Una factura emitida | Se intenta editar o borrar | La operación se rechaza; la única vía es la reversión documentada | Prueba negativa |
| AC-FAC-05 | El proveedor de emisión no responde | Se emite una factura | Se aplica la política de contingencia validada o se bloquea con mensaje; no se genera un comprobante inválido | Prueba de fallo |
| AC-FAC-06 | Un período con facturas | Se exportan comprobantes y libros | El archivo contiene todos los documentos del período y es verificable | Prueba de exportación |

## 7. Fiscal

| ID | Dado | Cuando | Entonces | Verificación |
|---|---|---|---|---|
| AC-FIS-01 | Una regla fiscal con vigencia | Se emite un documento en una fecha dada | Se aplica la versión vigente; el documento conserva la versión usada | Prueba de versionado |
| AC-FIS-02 | Un medio de pago en divisa | Se registra un cobro | El IGTF se calcula según la regla configurada y queda desglosado | Caso dorado |
| AC-FIS-03 | Un período con ventas y compras | Se generan libros y retenciones | Los totales cuadran con los documentos; el especialista valida el formato | Revisión externa |
| AC-FIS-04 | Un cambio de alícuota | Se actualiza el parámetro | Los documentos anteriores no cambian; los nuevos usan la nueva vigencia y el cambio queda auditado | Prueba de auditoría |

## 8. Tesorería

| ID | Dado | Cuando | Entonces | Verificación |
|---|---|---|---|---|
| AC-TES-01 | Una caja cerrada | Se intenta registrar un cobro en efectivo | Se rechaza; con caja abierta se registra y afecta el arqueo | Prueba E2E |
| AC-TES-02 | Una factura por 1.000 | Se cobra 400 en efectivo y 300 por transferencia | Se crean dos aplicaciones, el saldo es 300 y la caja refleja solo el efectivo | Integración |
| AC-TES-03 | Un anticipo de cliente | Se aplica a una factura | El saldo se reduce sin duplicar el ingreso y la aplicación es trazable | Prueba de dominio |
| AC-TES-04 | Un estado de cuenta importado | Se ejecuta la conciliación | Las partidas coincidentes se marcan y las diferencias quedan en revisión | Prueba con archivo real |
| AC-TES-05 | Un cobro en moneda distinta a la funcional | Se aplica a una factura | Se registra la diferencia cambiaria y la contabilidad la refleja | Caso dorado |

## 9. Contabilidad

| ID | Dado | Cuando | Entonces | Verificación |
|---|---|---|---|---|
| AC-CON-01 | Un plan de cuentas con cuentas imputables | Se registra un asiento desbalanceado | Se rechaza; uno balanceado se contabiliza y queda inmutable | Prueba de dominio |
| AC-CON-02 | Asientos contabilizados en un período | Se consultan diario, mayor y balanza | Los saldos cuadran y los estados financieros son consistentes | Prueba de reportes |
| AC-CON-03 | Un período cerrado | Se intenta contabilizar | Se rechaza; la reapertura requiere permiso y auditoría | Prueba negativa |
| AC-CON-04 | Una factura emitida | Se procesa el evento de contabilización | Se genera el asiento automático con auxiliar de tercero y centro de costo correctos | Integración |
| AC-CON-05 | Un asiento contabilizado | Se revierte | Se crea un contraasiento que referencia el original; la balanza vuelve al saldo previo | Prueba de dominio |

## 10. RRHH y Nómina

| ID | Dado | Cuando | Entonces | Verificación |
|---|---|---|---|---|
| AC-RRH-01 | Un empleado con contrato y cargo | Se consulta su expediente | Solo usuarios autorizados ven datos sensibles; el historial está completo | Prueba de autorización |
| AC-RRH-02 | Un empleado con vacaciones pendientes | Se solicita y aprueba vacaciones | El saldo se descuenta, se registra el período y se refleja en la nómina | Prueba de flujo |
| AC-NOM-01 | Parámetros laborales vigentes | Se ejecuta una nómina con novedades | Cada concepto se calcula de forma determinista y el recálculo produce el mismo resultado | Caso dorado + revisión |
| AC-NOM-02 | Un período con utilidades y prestaciones | Se procesa | Los montos coinciden con el cálculo del especialista y quedan trazados por concepto | Revisión externa |
| AC-NOM-03 | Un empleado que egresa | Se liquida | Se incluyen todos los conceptos aplicables y el recibo es verificable | Caso dorado |
| AC-NOM-04 | Una nómina aprobada | Se paga y contabiliza | Se genera el pago en tesorería y la provisión/asiento cuadran | Integración |

## 11. POS

| ID | Dado | Cuando | Entonces | Verificación |
|---|---|---|---|---|
| AC-POS-01 | Una sesión de caja abierta | Se registra una venta con código de barras | La venta se completa en menos del objetivo y actualiza stock y caja | Prueba en dispositivo |
| AC-POS-02 | Un pago mixto | Se cobra con efectivo y tarjeta | Se calcula vuelto, se imprime el comprobante y se abre la gaveta | Prueba de hardware |
| AC-POS-03 | El POS sin conexión | Se realizan 20 ventas y se reconecta | Las ventas se sincronizan sin duplicados y el stock converge | Prueba de red |
| AC-POS-04 | Una jornada de caja | Se cierra con diferencia | Se solicita autorización, se registra la diferencia y el reporte Z cuadra | Prueba E2E |

## 12. Producción, CRM e integraciones

| ID | Dado | Cuando | Entonces | Verificación |
|---|---|---|---|---|
| AC-PRO-01 | Una receta con componentes | Se emite una orden de producción | Se reservan y consumen componentes según receta y se registra el producto terminado | Integración |
| AC-PRO-02 | Una orden con desperdicio | Se cierra la orden | El costo incluye materiales, mano de obra y merma; el inventario cuadra | Caso dorado |
| AC-CRM-01 | Un prospecto con oportunidad | Se registra una actividad | El pipeline refleja la etapa, el responsable y el historial | Prueba E2E |
| AC-CRM-02 | Una oportunidad ganada | Se convierte a cliente y se cotiza | Se crea el cliente sin duplicados y la cotización conserva el vínculo | Integración |
| AC-INT-01 | Una credencial de integración | Se llama la API con scope limitado | Solo accede a los recursos permitidos; el intento fuera de scope se rechaza | Prueba de seguridad |
| AC-INT-02 | Un webhook suscrito | Ocurre un evento | Se entrega firmado, con reintentos e idempotencia; el fallo se registra | Prueba de integración |
| AC-INT-03 | Documentos y catálogo | Se exportan para BI | El formato es documentado, completo y reimportable | Prueba de exportación |

## 13. Criterios transversales

| ID | Dado | Cuando | Entonces | Verificación |
|---|---|---|---|---|
| AC-SEC-01 | Dos tenants con datos similares | Un usuario de A intenta acceder a un recurso de B por ID, filtro o reporte | La operación falla con 404/403 sin filtrar existencia; se audita el intento | Batería de aislamiento |
| AC-SEC-02 | Un usuario con MFA habilitado | Inicia sesión y revoca la sesión | El acceso exige segundo factor; la revocación invalida refresh y acceso | Prueba de seguridad |
| AC-SEC-03 | Un usuario con rol limitado | Intenta una acción administrativa | Se rechaza en servidor aunque la UI la muestre | Prueba de autorización |
| AC-PERF-01 | CO-1 con dataset representativo | Se ejecuta la prueba de carga | Se cumplen NFR-PER-001…005 sin errores | Informe k6 |
| AC-PERF-02 | 10.000 emisiones concurrentes | Se ejecutan | Se cumplen NFR-DAT-004 y NFR-PER-003 | Prueba de estrés |
| AC-OBS-01 | Una operación crítica | Se ejecuta | Traza, métricas, log sin PII y auditoría correlacionadas | Revisión de telemetría |
| AC-MIG-01 | Un archivo CSV con errores | Se importa | Se rechazan filas inválidas con detalle, se importan las válidas y el resultado es reconciliable | Prueba de importación |

## 14. Reglas de uso

1. Un AC no se marca cumplido sin evidencia enlazada.
2. Los AC transversales (`SEC`, `PERF`, `OBS`, `MIG`) aplican a todos los releases.
3. Si un AC cambia, se actualiza el requisito asociado y se registra en [documentation-review.md](../documentation-review.md).
4. Las pruebas que verifican aislamiento y concurrencia son obligatorias en CI para los módulos marcados como críticos.

## 15. Documentos relacionados

- [Requisitos funcionales](functional-requirements.md)
- [Requisitos no funcionales](non-functional-requirements.md)
- [Estrategia de pruebas](../architecture/testing-strategy.md)
- [PRD](PRD.md)
