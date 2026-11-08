# Revisión de dependencia R5→R3 — T-004

**Fecha:** 2026-11-08
**Estado:** revisión entregada; **corrección pendiente de aprobación humana**.
**Tarea:** [T-004](../../tasks/T-004-r5-dependency.md)
**Decisión vigente:** `D-04` — añadir R3 o retirar `NOM-008` de R5.
**Hallazgo:** `AUD-P1-09`.

---

## 1. Resultado

Existe **una contradicción documental**: la tabla resumen del roadmap declara que R5 depende de `R1, R2`, mientras que el resto de la documentación declara `R1, R2, R3` y los criterios de aceptación de nómina exigen contabilidad.

No se modificó el roadmap ni el alcance. La corrección propuesta queda para aprobación del responsable.

---

## 2. Evidencia comparada

| Documento / sección | Declaración actual | ¿Coherente con R3? |
|---|---|---|
| [roadmap.md §2, tabla de vista general](../product/roadmap.md) | R5 depende de **R1, R2** | **No** |
| [roadmap.md §7, Dependencias](../product/roadmap.md) | `R1; R2 (tesorería); R3 (contabilidad y provisión)` | Sí |
| [roadmap.md §10, diagrama](../product/roadmap.md) | `G3[G3 Contabilidad validada] --> R5` | Sí |
| [releases.md §6, R5](../../backlog/releases.md) | Dependencias `R1/R2/R3` | Sí |
| [releases.md §8, matriz](../../backlog/releases.md) | `R5 → R1, R2, R3` | Sí |
| [project-execution-plan.md §3](../project-execution-plan.md) | R5 requiere resolver su dependencia con R3 | Sí |
| [acceptance-criteria.md §10](../requirements/acceptance-criteria.md) | `AC-NOM-04`: nómina aprobada → pago y provisión/asiento cuadran | Sí; exige contabilidad |
| [functional-requirements.md §12](../requirements/functional-requirements.md) | `NOM-008` provisión contable de nómina; `NOM-010` pago vía tesorería | Sí; exige CON |
| [payroll.md §1](../domain/payroll.md) | NOM no es responsable de asientos; los consume de Contabilidad | Sí; exige CON |

**Resultado:** 7 referencias coherentes con R3 frente a 1 tabla resumen que aún dice R1/R2.

---

## 3. Decisión vigente

`D-04` fue aprobada técnicamente: **añadir R3 o retirar `NOM-008` de R5**.

La documentación ya eligió de hecho la primera opción:

- `NOM-008` (provisión contable) sigue en R5;
- `AC-NOM-04` exige provisión y asiento conciliados;
- releases y diagrama ya declaran R3;
- solo la tabla resumen del roadmap conserva la dependencia antigua.

Por tanto, la corrección coherente es **añadir R3**, no retirar `NOM-008`.

---

## 4. Propuesta

### Opción A — recomendada

Corregir la tabla resumen de R5:

```diff
- | **R5** | ÁVILA People | Administrar personas y nómina | RRH, NOM, TES, CON | R1, R2 | Sí (complemento) |
+ | **R5** | ÁVILA People | Administrar personas y nómina | RRH, NOM, TES, CON | R1, R2, R3 | Sí (complemento) |
```

No cambia alcance, módulos, requisitos ni criterios; alinea la tabla con el resto del corpus.

### Opción B — no recomendada

Retirar `NOM-008` y los criterios contables de R5, diferiéndolos a R6. Esto sí cambia alcance y exige actualizar:

- `functional-requirements.md` (`NOM-008`);
- `acceptance-criteria.md` (`AC-NOM-04`);
- `payroll.md` (provisión);
- `releases.md` y `roadmap.md` (criterios de salida);
- el backlog de R5.

No se propone porque el valor de R5 incluye la provisión contable y el resto del corpus ya la asume.

---

## 5. Por qué esto no cierra P1-09

`AUD-P1-09` no se cierra con esta revisión:

1. La corrección de la tabla requiere aprobación y aplicación por el responsable.
2. `T-13` debe verificar la coherencia final.
3. R5 sigue bloqueado hasta que la dependencia R3 esté explícita y aprobada.
4. No se implementa nómina, contabilidad ni provisión.

La dependencia permanece **explícita y bloqueada**.

---

## 6. Criterios de aceptación de T-004

| Criterio | Evidencia | Resultado |
|---|---|---|
| Identifica secciones y decisión vigente | §2 and §3 | **Pass** |
| Produce una propuesta verificable | §4 diff and options | **Pass** |
| La dependencia permanece explícita y bloqueada | §5; no code/scope change | **Pass** |
| No modifica el alcance unilateralmente | Roadmap untouched; proposal only | **Pass** |

---

## 7. Próximos pasos

1. Aprobar la opción A.
2. Aplicar el cambio de una línea en `roadmap.md §2`.
3. Ejecutar `T-13` y actualizar el estado de `AUD-P1-09`.
4. No iniciar R5 hasta que R3 esté validado y G3 abierto.

**Dueño:** @acamposve.
**Evidencia:** [execution-log](../../tasks/evidence/T-004/execution-log.md) · [verification](../../tasks/evidence/T-004/verification.md).
