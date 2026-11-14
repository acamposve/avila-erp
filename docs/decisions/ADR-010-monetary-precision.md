# ADR-010 — Precisión monetaria y multimoneda

- **Estado:** **Aceptado** — 2026-11-14.
- **Decisores:** @acamposve (Arquitectura, Producto, Contabilidad).
- **Complementa:** [ADR-004](ADR-004-money-and-rounding.md). **No reemplaza** ADR-004 ni cambia sus reglas.
- **Alcance:** estándar de implementación y verificación para R1/R2.

## 1. Contexto

ADR-004 ya fijó tipos, escala, redondeo y multimoneda. Falta un estándar de cumplimiento que impida que cada módulo elija precisiones distintas, use punto flotante o recalcule tasas históricas. Este ADR convierte ADR-004 en una regla verificable.

## 2. Decisión

### 2.1 Tipos canónicos

ADR-004 es normativo:

| Concepto | Tipo PostgreSQL | Escala |
|---|---|---:|
| Importe monetario | `numeric(19,4)` | 4 |
| Precio unitario | `numeric(19,6)` | 6 |
| Cantidad | `numeric(19,6)` | 6 |
| Tasa de cambio | `numeric(19,8)` | 8 |
| Porcentaje / alícuota | `numeric(9,6)` | 6 |

No se usa `float`, `double`, `real` ni `money` de PostgreSQL para importes. La escala de la moneda (0, 2 o 3 decimales) se aplica al **registrar** el importe; el almacenamiento conserva 4 decimales.

### 2.2 Redondeo determinista

1. Se usa **mitad hacia arriba, alejada de cero** (`MidpointRounding.AwayFromZero`), tal como exige ADR-004.
2. **No se usa banker's rounding.** Se evaluó y se rechaza porque contradice ADR-004 y produce diferencias con la regla aprobada.
3. Se redondea por línea a la escala de la moneda; el impuesto se calcula sobre la base gravable de la línea; los totales son la suma de líneas ya redondeadas.
4. La conversión a moneda base se redondea a la escala de la moneda base. No se vuelve a redondear el total.
5. Las diferencias se registran como línea de ajuste trazable; nunca se modifica un documento emitido.

### 2.3 Moneda base, transaccional y de referencia

Cada documento monetario registra:

| Campo | Regla |
|---|---|
| `base_currency` | Moneda funcional de la empresa; obligatoria |
| `transaction_currency` | Moneda del documento; obligatoria |
| `reference_currency` | Opcional, solo reporte/visualización; no contabiliza |
| `exchange_rate` | `numeric(19,8)`, snapshot del momento del documento |
| `rate_source` | Fuente identificable y versionada |
| `rate_effective_at` | Vigencia de la tasa usada |

- Si `transaction_currency = base_currency`, la tasa es `1` y no hay diferencia cambiaria.
- Se almacenan importes en moneda transaccional y en moneda base calculados con la **misma tasa snapshot**.
- La moneda de referencia nunca se suma con otras monedas ni sustituye a la moneda base.
- No se recalcula retroactivamente una tasa; un cambio de tasa genera un nuevo documento/evento, no reescribe el histórico.
- Está prohibido sumar importes de monedas distintas sin conversión documentada a una moneda base.

### 2.4 Catálogo y contrato de API

- `Currency` es un catálogo versionado por `code` ISO 4217, `scale`, `symbol` y vigencia.
- Los importes viajan en JSON/API como **string decimal** (sin notación científica), nunca como `number` binario.
- Los contratos públicos exponen moneda, tasa y fuente; no aceptan tasas calculadas por el cliente.
- Los redondeos se ejecutan en el servidor; el frontend solo muestra.

### 2.5 Verificación obligatoria

- Escaneo de esquema: cero columnas `float`/`double`/`real` para dinero.
- Pruebas de redondeo: mitad, negativos, descuentos, impuestos y múltiples líneas.
- Propiedad: suma de líneas redondeadas = total; conversión ida/vuelta dentro de tolerancia documentada.
- Prueba de documento histórico: actualizar la tasa no cambia el documento.
- Prueba de API: decimales como string, sin pérdida de precisión.
- Revisión de código: ningún módulo define su propia escala o función de redondeo.

## 3. Consecuencias

- Todos los módulos comparten una única semántica monetaria.
- Los reportes multimoneda deben declarar moneda base y tasa; no pueden sumar monedas.
- R2/R3 no puede introducir una escala distinta sin un ADR que reemplace ADR-004.

## 4. Referencias

- [ADR-004](ADR-004-money-and-rounding.md)
- [Facturación](../domain/billing.md)
- [Tesorería](../domain/treasury.md)
- [Contabilidad](../domain/accounting.md)
- [Diseño de base de datos](../architecture/database-design.md)
