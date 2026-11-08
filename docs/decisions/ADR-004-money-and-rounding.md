# ADR-004 — Dinero, moneda y redondeo

- **Estado:** Aceptado
- **Fecha:** 2026-11-08
- **Decisores:** Arquitectura, Contabilidad, Producto
- **Reemplaza a:** —

## 1. Contexto

ÁVILA ERP opera con bolívares, divisas, tasas de cambio variables y documentos fiscales que deben cuadrar al céntimo. Un error de redondeo o el uso de punto flotante produce diferencias contables, rechazos fiscales y desconfianza del cliente. La moneda y su escala pueden cambiar por decisiones del país (redenominación), por lo que no deben estar codificadas en el dominio.

## 2. Decisión

### 2.1 Tipos y escalas

| Concepto | Tipo | Escala | Ejemplo |
|---|---|---|---|
| Importe monetario | `numeric(19,4)` + código de moneda | La escala de la moneda (2 para VES/USD) | `1250.0000` VES |
| Precio unitario | `numeric(19,6)` | 6 | `0.123456` |
| Cantidad | `numeric(19,6)` | 6 | `1.500000` |
| Tasa de cambio | `numeric(19,8)` | 8 | `36.54321000` |
| Porcentaje / alícuota | `numeric(9,6)` | 6 | `0.160000` |
| Tipo de cambio inverso | `numeric(19,8)` | 8 | Calculado, no almacenado salvo necesidad |

Los importes se almacenan con 4 decimales para soportar monedas de 0, 2 o 3 decimales y se **redondean a la escala de la moneda al momento de registrar**. Nunca se usa `float` ni `double`. En JSON/API, los decimales viajan como **string** para evitar pérdida de precisión.

### 2.2 Moneda

- `Currency` es un catálogo con `code` (ISO 4217), `scale`, `symbol` y `version`.
- Cada empresa define su **moneda funcional**.
- Cada documento registra **moneda de transacción**, **tasa**, **fuente** y **vigencia**.
- La contabilidad registra en moneda funcional; el documento conserva su moneda original y la tasa aplicada.
- Una **redenominación** se modela como una nueva versión de moneda con factor de conversión y fecha de vigencia; no se reescriben documentos históricos.

### 2.3 Redondeo

1. Se calcula con precisión completa y se redondea **por línea** a la escala de la moneda.
2. Regla: **mitad hacia arriba, alejada de cero** (`MidpointRounding.AwayFromZero`).
3. El impuesto se calcula sobre la base gravable de la línea (cantidad × precio − descuento) y se redondea a la escala.
4. Los totales del documento son la **suma de las líneas ya redondeadas**; no se vuelve a redondear el total.
5. Si por una regla externa el total debe ajustarse, se registra una línea de **ajuste por redondeo** con cuenta contable propia y trazabilidad.
6. Las diferencias de pago en efectivo se registran como ajuste, nunca modificando el documento.
7. La misma función de redondeo se usa en backend, reportes y pruebas; el frontend solo muestra.

### 2.4 Multimoneda

- La conversión se hace con la tasa **snapshot** del documento; no se recalcula retroactivamente.
- Al cobrar/pagar en moneda distinta a la del documento se registra la **diferencia cambiaria** en cuentas específicas.
- Las revaluaciones de saldos en moneda extranjera son un proceso contable explícito y auditado (R3).
- Prohibido sumar importes de monedas distintas sin conversión documentada.

## 3. Consecuencias

### Positivas
- Cálculos reproducibles y auditables.
- Soporte de monedas con distinta escala y de redenominaciones sin reescribir historial.
- Conciliación contable y fiscal confiable.

### Negativas
- Mayor complejidad de modelo y de pruebas.
- Requiere disciplina en todos los módulos y en reportes.
- El redondeo por línea puede diferir del cálculo "total × tasa"; es intencional y documentado.

## 4. Alternativas consideradas

| Alternativa | Motivo de descarte |
|---|---|
| `decimal(18,2)` fijo | No soporta monedas de 0/3 decimales ni redenominaciones |
| `float`/`double` | Inaceptable para dinero |
| Redondear solo al total | Genera diferencias por línea y rechazos fiscales |
| Recalcular con tasa actual | Rompe la inmutabilidad y la trazabilidad |

## 5. Verificación

- Casos dorados de redondeo (mitad, negativos, descuentos, impuestos, múltiples líneas).
- Pruebas de propiedad: suma de líneas = total; conversión ida/vuelta dentro de tolerancia.
- Prueba de documento histórico que no cambia al actualizar la tasa.
- Revisión de esquema: cero columnas `float`/`real` para dinero.
- Prueba de API: decimales como string, sin notación científica.

## 6. Referencias

- [billing.md](../domain/billing.md)
- [treasury.md](../domain/treasury.md)
- [accounting.md](../domain/accounting.md)
- [database-design.md](../architecture/database-design.md)
