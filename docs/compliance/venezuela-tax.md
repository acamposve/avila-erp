# Fiscalidad venezolana — Alcance y modelo de parametrización

**Versión:** 1.0 · **Fecha:** 2026-11-08 · **Estado:** **requiere validación externa**

> **Aviso:** este documento describe **qué debe investigarse, modelarse y validarse**. No fija tasas, alícuotas, porcentajes, formatos ni obligaciones. Todo valor concreto está `PENDIENTE DE VALIDACIÓN LEGAL / CONTABLE` y debe provenir de una fuente registrada en [regulatory-sources.md](regulatory-sources.md) y de la aprobación de un especialista.

## 1. Alcance

| Materia | ¿En alcance del producto? | Release | Estado |
|---|---|---|---|
| IVA (ventas, compras, libros) | Sí | R2/R3 | `PENDIENTE DE VALIDACIÓN LEGAL / CONTABLE` |
| Retenciones de IVA | Sí | R3 | `PENDIENTE` |
| ISLR (retenciones y reportes) | Parcial | R3 | `PENDIENTE` |
| IGTF en medios de pago | Sí (cálculo y registro) | R2 | `PENDIENTE` |
| Libros fiscales | Sí | R3 | `PENDIENTE` |
| Comprobantes de retención | Sí | R3 | `PENDIENTE` |
| Declaraciones ante el SENIAT | **No** (solo exportación/reportes) | R3 | Excluido |
| Facturación electrónica propia homologada | **No** (integración) | R2 | Excluido |
| Tributos municipales/estadales | Parcial (registro) | R6 | `PENDIENTE` |
| Aduanas e importaciones | **No** | R6 | Excluido |
| Ajuste por inflación contable | A evaluar | R3 | `PENDIENTE DE VALIDACIÓN LEGAL / CONTABLE` |

## 2. Modelo de reglas fiscales

Toda regla fiscal es un dato versionado, no una constante de código:

```text
TaxRule
  id, tenant_id (null = regla global), country, tax_type, code
  rate, base_type, rounding, effective_from, effective_to
  legal_source_id, validation_status, approved_by, approved_at
  metadata (condiciones: tipo de contribuyente, artículo, cliente, jurisdicción)
```

Reglas:

1. La regla aplicable se resuelve por **fecha del documento** y condiciones.
2. El documento guarda el `tax_rule_id` y el snapshot de la tasa aplicada.
3. Una regla no se modifica: se crea una versión con nueva vigencia.
4. Una regla sin `legal_source_id` y `validation_status = VERIFICADA` no puede activarse en producción.
5. Las reglas globales de la plataforma se publican como versión; cada empresa puede tener parámetros propios cuando la norma lo permita (con aprobación).

## 3. IVA

### 3.1 Qué debe modelarse
- Hecho imponible y base imponible.
- Tipos de alícuota (general, reducida, adicional, exento, no sujeto, exportación) como **códigos configurables**, no como valores fijos.
- Descuentos, bonificaciones y devoluciones que afectan la base.
- IVA en compras (crédito fiscal) y su libro.
- Retenciones de IVA y su comprobante.
- Redondeo por línea según [ADR-004](../decisions/ADR-004-money-and-rounding.md).

### 3.2 Qué no se afirma
- No se fija la alícuota general ni las especiales.
- No se fija el porcentaje de retención ni el mecanismo de sustraendo.
- No se fija el formato del libro ni del comprobante.
- No se afirma qué bienes o servicios están exentos.

**Estado:** `PENDIENTE DE VALIDACIÓN LEGAL / CONTABLE`.

### 3.3 Requisitos del producto
- `FAC-002`, `FIS-001`, `FIS-002`, `FIS-003`, `FIS-007`, `FIS-010`.
- Libro de ventas y compras (`FAC-004`).
- Retenciones (`FAC-005`).
- Exportación (`FIS-009`, `FAC-014`).

## 4. ISLR

- Retenciones sobre pagos a proveedores y sobre salarios: **PENDIENTE**.
- Reportes anuales: el producto puede exportar datos, pero **no** presenta declaraciones.
- La base, tarifas y exenciones dependen de la norma y del tipo de contribuyente: no se fijan aquí.
- Ver `SRC-012`, `SRC-013`.

**Requisitos:** `FIS-003`, `FIS-005`, `FIS-009`. **Estado:** `PENDIENTE`.

## 5. IGTF

- Impuesto sobre determinadas transacciones financieras en moneda extranjera.
- Debe modelarse por **medio de pago**, moneda y base, con vigencia.
- El cálculo se registra en el cobro/pago y se refleja en la contabilidad.
- La tasa, exenciones y sujetos están **PENDIENTE DE VALIDACIÓN LEGAL / CONTABLE**.
- El producto **no** presenta declaraciones; exporta la información.

**Requisitos:** `FIS-006`, `TES-003`, `TES-004`. **Estado:** `PENDIENTE`.

## 6. Libros y comprobantes

| Documento | Contenido a validar | Release |
|---|---|---|
| Libro de ventas | Período, número, fecha, cliente, RIF, base, alícuota, impuesto, total | R3 |
| Libro de compras | Proveedor, RIF, número, fecha, base, impuesto, retención | R3 |
| Comprobante de retención de IVA | Datos del agente, sujeto retenido, base, porcentaje, monto | R3 |
| Comprobante de retención de ISLR | Datos y concepto | R3 |
| Resúmenes por alícuota | Base e impuesto por tipo | R3 |

Formatos exactos, campos obligatorios y plazos: **PENDIENTE DE VALIDACIÓN LEGAL / CONTABLE**. Ver [invoicing.md](invoicing.md).

## 7. Registro y obligaciones

- RIF y datos del contribuyente por empresa (`ADM-011`).
- Condición de contribuyente especial: a validar.
- Calendario de obligaciones y recordatorios (`FIS-004`): fechas y periodicidad **PENDIENTE**.
- Reportes al SENIAT: solo exportación; no presentación automática.
- Conservación de documentos: plazo **PENDIENTE** (`DEC-008`).

## 8. Trazabilidad y auditoría

- Cada emisión registra la regla aplicada, versión, fuente y usuario.
- Los cambios de parámetros son inmutables y auditados (`FIS-008`, `FIS-010`).
- Toda diferencia entre libro y comprobantes debe ser explicable.
- El especialista debe poder reconstruir cualquier comprobante desde el snapshot.

## 9. Lo que el producto NO hará en esta etapa

- Declarar impuestos en nombre del contribuyente.
- Emitir comprobantes sin medio autorizado validado.
- Interpretar la norma por sí mismo.
- Aplicar una alícuota "por defecto" no validada.
- Ocultar la incertidumbre al usuario: la interfaz mostrará el estado de validación de la regla.

## 10. Preguntas para el especialista fiscal

1. ¿Qué medios de emisión están autorizados y cuáles son obligatorios para cada tipo de contribuyente a partir de la vigencia de la PA-0093?
2. ¿Qué datos y leyendas debe contener una factura válida (y sus notas de crédito/débito)?
3. ¿Cómo se determina la base imponible con descuentos, devoluciones y fletes?
4. ¿Qué alícuotas y tipos aplican y desde cuándo?
5. ¿Qué operaciones están exentas, exoneradas o no sujetas?
6. ¿Cuándo y cómo se practica la retención de IVA? ¿Quién es agente?
7. ¿Qué retenciones de ISLR aplican a proveedores y a salarios?
8. ¿Cómo se calcula y declara el IGTF por medio de pago?
9. ¿Qué libros son obligatorios, con qué formato y periodicidad?
10. ¿Cómo se anula o corrige un comprobante sin incumplir?
11. ¿Qué plazo de conservación aplica a documentos fiscales y contables?
12. ¿Qué requisitos tiene la emisión offline o de contingencia?
13. ¿Qué formato de exportación acepta el SENIAT para cada reporte?
14. ¿Cómo se tratan las operaciones en divisas y las diferencias cambiarias?
15. ¿Aplica ajuste por inflación contable y con qué metodología?

**Ninguna de estas preguntas se responde por inferencia en el producto.**

## 11. Dependencias y decisiones

- `DEC-002` medio de emisión.
- `DEC-007` retenciones.
- `DEC-008` retención documental.
- `DEC-012` ajuste por inflación.
- `FIS-*`, `FAC-*`.

## 12. Documentos relacionados

- [regulatory-sources.md](regulatory-sources.md)
- [invoicing.md](invoicing.md)
- [billing.md](../domain/billing.md)
- [testing-strategy.md](../architecture/testing-strategy.md)
