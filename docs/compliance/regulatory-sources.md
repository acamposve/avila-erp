# Registro de fuentes regulatorias

**Versión:** 1.0 · **Fecha de corte:** 2026-11-08 · **Jurisdicción principal:** República Bolivariana de Venezuela

> **Este registro no es asesoría legal.** Documenta qué se consultó, cuándo y con qué estado de validación. Toda afirmación normativa usada en el producto debe poder rastrearse hasta una entrada `SRC-*`. Lo que no está validado se marca `PENDIENTE DE VALIDACIÓN LEGAL / CONTABLE`.

## 1. Método

1. Se identifican las normas y fuentes relevantes por materia (tributaria, facturación, laboral, datos).
2. Se registra: identificador, norma o fuente, jurisdicción, referencia/URL, fecha de publicación, fecha de consulta, tipo (primaria/secundaria), estado.
3. Se marca si el texto oficial fue consultado o solo referenciado.
4. Un especialista valida el contenido antes de convertirlo en regla del producto.
5. Los cambios normativos generan una nueva versión del parámetro y una entrada nueva o actualizada en este registro.

## 2. Estados de validación

| Estado | Significado |
|---|---|
| `VERIFICADA` | Texto oficial consultado y contenido confirmado por especialista |
| `CONSULTADA` | Fuente consultada; falta validación de especialista |
| `REFERENCIADA` | Norma identificada; texto no consultado en esta sesión |
| `PENDIENTE` | No se ha identificado fuente suficiente |
| `PENDIENTE DE VALIDACIÓN LEGAL / CONTABLE` | Materia regulada que no puede implementarse como definitiva |

## 3. Registro

| ID | Norma / fuente | Materia | Jurisdicción | Referencia | Publicación | Consulta | Tipo | Estado |
|---|---|---|---|---|---|---|---|---|
| `SRC-001` | Providencia Administrativa del SENIAT sobre normas generales y medios de emisión de facturas y otros documentos fiscales (identificada como PA-0093), publicada en Gaceta Oficial N.º 43.484 | Facturación / medios de emisión | Venezuela | [Nota Banca y Negocios](https://www.bancaynegocios.com/en-gaceta-las-normas-generales-y-los-medios-para-la-emision-de-facturas-y-otros-documentos-fiscales/) · [GO-43484 (PDF)](https://www.bancaynegocios.com/wp-content/uploads/2026/10/GO-43484.pdf) · [Finanzas Digital](https://finanzasdigital.com/seniat-levanta-uso-obligatorio-maquinas-fiscales-gaceta-oficial/) | 2026-10-21 | 2026-11-08 | Secundaria + PDF de Gaceta | `CONSULTADA` |
| `SRC-002` | Constitución de la República Bolivariana de Venezuela | Marco constitucional (tributario, laboral, privacidad) | Venezuela | Referencia oficial no consultada | 1999 (reformas) | 2026-11-08 | Primaria | `REFERENCIADA` |
| `SRC-003` | Código Orgánico Tributario (COT) | Obligaciones, sanciones, procedimientos | Venezuela | Gaceta Extraordinaria N.º 6.152 (referencia a confirmar) | 2014-11-18 (referencia) | 2026-11-08 | Primaria | `REFERENCIADA` |
| `SRC-004` | Ley de IVA y su Reglamento | IVA, base imponible, exenciones, retenciones | Venezuela | Gaceta Oficial N.º 38.632 (referencia a confirmar) | 2007-02-26 (referencia) | 2026-11-08 | Primaria | `REFERENCIADA` |
| `SRC-005` | Ley Orgánica del Trabajo, los Trabajadores y las Trabajadoras (LOTTT) | Relación laboral, prestaciones, vacaciones, utilidades, liquidación | Venezuela | Gaceta Extraordinaria N.º 6.076 (referencia a confirmar) | 2012-04-30 (referencia) | 2026-11-08 | Primaria | `REFERENCIADA` |
| `SRC-006` | Ley del Seguro Social y su Reglamento | IVSS, aportes, prestaciones | Venezuela | Referencia oficial no consultada | 2012 / 2003 (referencia) | 2026-11-08 | Primaria | `REFERENCIADA` |
| `SRC-007` | Ley del Régimen Prestacional de Vivienda y Hábitat (FAOV) y su reglamento | Aporte de vivienda | Venezuela | Referencia oficial no consultada | 2012 (referencia) | 2026-11-08 | Primaria | `REFERENCIADA` |
| `SRC-008` | Ley sobre el Instituto Nacional de Cooperación Educativa (INCES) | Aporte patronal | Venezuela | Referencia oficial no consultada | 2014 (referencia) | 2026-11-08 | Primaria | `REFERENCIADA` |
| `SRC-009` | Convenios cambiarios y publicaciones del Banco Central de Venezuela (BCV) | Tasas de cambio oficiales | Venezuela | Portal BCV no consultado en esta sesión | Vigente | 2026-11-08 | Primaria | `PENDIENTE` |
| `SRC-010` | Ley de Impuesto a las Grandes Transacciones Financieras (IGTF) | IGTF | Venezuela | Referencia oficial no consultada | 2015 (reforma posterior; a confirmar) | 2026-11-08 | Primaria | `PENDIENTE` |
| `SRC-011` | Providencias del SENIAT sobre retenciones de IVA | Retenciones | Venezuela | No identificada con precisión | — | 2026-11-08 | Primaria | `PENDIENTE` |
| `SRC-012` | Providencias del SENIAT sobre retenciones de ISLR | Retenciones | Venezuela | No identificada con precisión | — | 2026-11-08 | Primaria | `PENDIENTE` |
| `SRC-013` | Ley de Impuesto sobre la Renta (ISLR) y su Reglamento | ISLR, obligaciones | Venezuela | Referencia oficial no consultada | 2007 / 2015 (a confirmar) | 2026-11-08 | Primaria | `REFERENCIADA` |
| `SRC-014` | Ley Especial contra los Delitos Informáticos | Seguridad de la información | Venezuela | Referencia oficial no consultada | 2001 | 2026-11-08 | Primaria | `REFERENCIADA` |
| `SRC-015` | Normativa de protección de datos personales | Privacidad | Venezuela | No se identificó una ley general vigente aplicable | — | 2026-11-08 | — | `PENDIENTE DE VALIDACIÓN LEGAL` |
| `SRC-016` | Código de Comercio | Libros, conservación de documentos | Venezuela | Referencia oficial no consultada | Vigente | 2026-11-08 | Primaria | `REFERENCIADA` |

## 4. Afirmaciones utilizadas en la documentación

| Afirmación | Fuente | Estado | Dónde se usa |
|---|---|---|---|
| Los medios de emisión de facturas fueron redefinidos y la máquina fiscal dejó de ser obligatoria, con entrada en vigencia diferida | `SRC-001` | `CONSULTADA`; requiere texto oficial y validación | [invoicing.md](invoicing.md) |
| La transición prevé un plazo de 180 días continuos desde la publicación (vencimiento aproximado: 2027-04-19) | `SRC-001` (cálculo propio) | `HIPÓTESIS`; confirmar con Gaceta | [invoicing.md](invoicing.md) |
| Existen obligaciones de IVA, ISLR, IGTF y aportes laborales | `SRC-003`…`SRC-010` | `REFERENCIADA` | [venezuela-tax.md](venezuela-tax.md), [labor-regulations.md](labor-regulations.md) |
| Las tasas, alícuotas y porcentajes concretos no se fijan en esta línea base | — | `PENDIENTE DE VALIDACIÓN LEGAL / CONTABLE` | Todo el producto |
| La LOTTT regula prestaciones, vacaciones, utilidades y terminación | `SRC-005` | `REFERENCIADA` | [labor-regulations.md](labor-regulations.md) |

## 5. Uso de fuentes secundarias

Las fuentes secundarias (prensa, consultoras, portales) solo se usan para **orientar la investigación** y localizar el texto oficial. Nunca sustituyen una norma ni una validación de especialista. Ejemplo: la tasa general de IVA reportada por portales secundarios no se implementa como constante; se configura como parámetro sujeto a validación.

## 6. Control de cambios

- Cada nueva norma o reforma genera una entrada `SRC-*` nueva o actualiza la existente con fecha de consulta.
- Las reglas del producto referencian `SRC-*` y `effective_from`.
- Un cambio normativo no reescribe documentos históricos; crea una versión nueva de la regla.
- La revisión de este registro es trimestral o ante un cambio regulatorio relevante.

## 7. Responsables

| Rol | Responsabilidad | Estado |
|---|---|---|
| Especialista fiscal/contable | Validar IVA, ISLR, IGTF, libros, retenciones | `PENDIENTE` de contratación |
| Especialista laboral | Validar LOTTT, prestaciones, aportes, liquidaciones | `PENDIENTE` de contratación |
| Legal | Términos, privacidad, retención, licencia | `PENDIENTE` |
| Producto | Traducir la norma a parámetros y requisitos | Asignado |

## 8. Documentos relacionados

- [venezuela-tax.md](venezuela-tax.md)
- [invoicing.md](invoicing.md)
- [labor-regulations.md](labor-regulations.md)
- [documentation-review.md](../documentation-review.md)
