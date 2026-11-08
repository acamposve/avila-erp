# Normativa laboral y nómina — Alcance y parametrización

**Versión:** 1.0 · **Fecha:** 2026-11-08 · **Estado:** **requiere validación externa**

> **Aviso:** este documento identifica materias y el modelo de parametrización. **No fija fórmulas, porcentajes, días, bases ni montos.** Todo cálculo laboral concreto está `PENDIENTE DE VALIDACIÓN LEGAL / CONTABLE` y debe ser aprobado por un especialista con fuente registrada en [regulatory-sources.md](regulatory-sources.md).

## 1. Marco identificado

| Materia | Norma de referencia | Estado |
|---|---|---|
| Relación laboral, jornada, salario, prestaciones, vacaciones, utilidades, terminación | LOTTT (`SRC-005`) | `REFERENCIADA` |
| Seguridad social (IVSS) | Ley del Seguro Social y Reglamento (`SRC-006`) | `REFERENCIADA` |
| Vivienda (FAOV) | Ley del Régimen Prestacional de Vivienda y Hábitat (`SRC-007`) | `REFERENCIADA` |
| INCES | Ley sobre el INCES (`SRC-008`) | `REFERENCIADA` |
| Retenciones de ISLR sobre salarios | Ley de ISLR (`SRC-013`) y providencias (`SRC-012`) | `PENDIENTE` |
| Protección de datos del trabajador | `SRC-015` | `PENDIENTE DE VALIDACIÓN LEGAL` |

## 2. Materias a modelar

| Materia | Qué debe parametrizarse | Qué NO se fija aquí |
|---|---|---|
| Contratos | Tipo, vigencia, jornada, periodo de prueba, condiciones | Requisitos de forma y límites legales |
| Salario | Salario base, normal, integral, variables, bonos | Fórmula de integración y exclusiones |
| Vacaciones | Días por año, bono, acumulación, disfrute | Días y porcentajes |
| Utilidades | Base, días, proporcionalidad, oportunidad | Días, base y límites |
| Prestaciones sociales | Garantía, retroactivo, intereses, anticipos | Días, tasas y fórmulas |
| Terminación | Causales, preaviso, indemnizaciones, liquidación | Montos y cálculos |
| Seguridad social | IVSS, FAOV, INCES, RPE | Porcentajes, topes y bases |
| ISLR del trabajo | Retención, tarifas, unidades tributarias | Tarifas y procedimiento |
| Novedades | Ausencias, permisos, horas extra, feriados | Reglas de recargo |
| Salarios en divisas | Registro y conversión | Tratamiento legal y contable |

## 3. Modelo de parámetros

```text
LegalParameter
  code, description, category, unit
  value (decimal, tabla o expresión referenciada)
  effective_from, effective_to
  legal_source_id, validation_status, approved_by, approved_at
  jurisdiction, notes
```

Reglas:

1. Ningún valor legal se codifica en el motor.
2. Cada cálculo registra el `parameter_id` y la versión usados.
3. Un cambio de parámetro no recalcula corridas aprobadas.
4. Una corrida nueva usa los parámetros vigentes a la fecha de la corrida.
5. Un parámetro sin fuente y aprobación no se activa en producción.
6. Las fórmulas se expresan en un lenguaje determinista y versionado, con pruebas doradas.

## 4. Motor de cálculo

- Entradas: empleado, contrato, período, novedades, parámetros vigentes, conceptos.
- Salidas: líneas por concepto, totales, aportes, provisiones y recibo.
- Debe ser **determinista y reproducible**: mismos datos y parámetros → mismo resultado.
- Debe explicar cada línea: concepto, base, tasa, resultado y fuente del parámetro.
- Debe permitir recálculo controlado y comparación entre corridas.
- No debe interpretar la ley: aplica parámetros aprobados.

## 5. Conceptos previstos

| Grupo | Ejemplos de concepto | Estado |
|---|---|---|
| Ingresos | Sueldo, bono, comisión, horas extra, feriado | A validar |
| Deducciones | Seguridad social, ISLR, préstamos, ausencias | A validar |
| Aportes patronales | IVSS, FAOV, INCES, RPE | A validar |
| Provisiones | Vacaciones, utilidades, prestaciones | A validar |
| Liquidación | Prestaciones, vacaciones, utilidades, salarios pendientes | A validar |

Los nombres y la composición exacta de cada concepto deben validarse con el especialista; el producto no publica una lista legal definitiva.

## 6. Prestaciones y utilidades

- Se modelan como **cuenta individual** con movimientos y trazabilidad.
- Los intereses y la garantía se calculan según parámetros; la fórmula concreta está `PENDIENTE`.
- Las utilidades se calculan por período y se pagan según la política validada.
- La liquidación consolida conceptos y conserva el detalle.
- Cualquier anticipo o retiro queda auditado.

## 7. Seguridad social y obligaciones

- Aportes y retenciones se calculan por parámetro vigente.
- Los topes, bases y porcentajes están `PENDIENTE`.
- La declaración y pago ante organismos **no** se realiza automáticamente en R5; se exportan los datos.
- Los comprobantes de retención se emiten cuando la norma lo exija (`PENDIENTE`).

## 8. Reglas de implementación

1. Prohibido introducir porcentajes, días o fórmulas "conocidas" sin fuente y aprobación.
2. El sistema debe mostrar el estado de validación de cada parámetro.
3. Las corridas aprobadas son inmutables.
4. Los datos salariales son restringidos: cifrado, permisos y auditoría.
5. Los recibos se generan desde el snapshot de la corrida.
6. Los cambios de parámetros requieren aprobación y quedan auditados.

## 9. Lo que el producto NO hará en R5

- Dictaminar sobre la legalidad de un contrato.
- Calcular indemnizaciones sin parámetros validados.
- Presentar declaraciones ante organismos.
- Gestionar reclutamiento, desempeño o capacitación.
- Sustituir al especialista laboral.

## 10. Preguntas para el especialista laboral

1. ¿Qué días de vacaciones y bono vacacional corresponden por año y antigüedad?
2. ¿Cómo se calcula el salario normal e integral y qué conceptos lo integran?
3. ¿Cómo se calculan utilidades (base, días, proporcionalidad) y cuándo se pagan?
4. ¿Cómo se calculan prestaciones (garantía y retroactivo) e intereses?
5. ¿Qué aportes y retenciones aplican (IVSS, FAOV, INCES, RPE, ISLR) y con qué bases y topes?
6. ¿Cómo se liquidan vacaciones no disfrutadas y utilidades pendientes?
7. ¿Qué ocurre con salarios pactados en divisas?
8. ¿Qué reglas aplican a horas extra, feriados y trabajo nocturno?
9. ¿Qué documentos debe conservar el empleador y por cuánto tiempo?
10. ¿Qué datos del trabajador son sensibles y cómo deben protegerse?
11. ¿Qué cambios normativos están previstos y cómo afectan los parámetros?
12. ¿Qué comprobantes y reportes deben emitirse?

**Ninguna respuesta se implementa por inferencia.**

## 11. Privacidad de datos laborales

- Datos de salud, discapacidad, salario, cuenta bancaria y documentos de identidad son **restringidos**.
- Acceso por rol y necesidad de saber; enmascarado en listados.
- Cifrado de campos sensibles.
- Retención según `DEC-008` y validación legal.
- Sin datos reales en pruebas.

## 12. Dependencias y decisiones

- `DEC-011` fórmulas laborales.
- `DEC-014` motor de cálculo.
- `DEC-008` retención documental.
- `NOM-*`, `RRH-*`.

## 13. Documentos relacionados

- [regulatory-sources.md](regulatory-sources.md)
- [payroll.md](../domain/payroll.md)
- [venezuela-tax.md](venezuela-tax.md)
- [security.md](../architecture/security.md)
