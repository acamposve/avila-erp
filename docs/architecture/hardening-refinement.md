# Refinamiento de hardening por historia — T-003

**Versión:** 1.0 · **Fecha:** 2026-11-08
**Estado:** refinamiento propuesto; **ningún P1/P2 se declara cerrado**.
**Fuente:** [Plan de hardening](architecture-hardening-plan.md), [auditoría independiente](independent-architecture-audit.md), [backlog](../../backlog/sprint-backlog.md), [releases](../../backlog/releases.md).
**Tarea:** [T-003](../../tasks/T-003-hardening-backlog.md).

---

## 1. Reglas de este refinamiento

1. Un hallazgo no se cierra porque exista un ADR, un spike o un documento.
2. Se cierra solo con: decisión aplicable, implementación de producción, prueba de aceptación ejecutada y evidencia reproducible.
3. Un spike pasado demuestra el mecanismo; **no** cuenta como implementación de producción.
4. Cada refinamiento tiene historia existente o propuesta, dependencia, pruebas de cierre y dueño.
5. Los IDs de historia existentes no se renumeran. Las historias nuevas se marcan como **(propuesta)**.
6. Los valores fiscales, contables y laborales no validados siguen fuera de este plan.

---

## 2. Prioridad

| Prioridad | Refinamiento | Hallazgos | Por qué |
|---|---|---|---|
| **P0 — R1** | UoW, tenant, RLS, `platform` | P1-03, P1-05, P1-06, P2-10, P2-11 | Es la base de aislamiento y transacción de todo lo demás |
| **P0 — R1** | Numeración e idempotencia | P1-07, P2-13 | Necesario para documentos fiscales y comandos reintentables |
| **P0 — R1** | Revisión R5→R3 | P1-09 | Dependencia de release, no de código |
| **P1 — R2** | Stock, pagos, crédito | P1-01, P1-02, P2-12 | Concurrencia comercial |
| **P1 — R2** | Posting, outbox y partida doble | P1-04, P2-02, P2-04, P2-10 | Facturación y contabilidad crítica |
| **P1 — R2** | Backfill bajo RLS | P2-11 | Migración de datos del piloto |
| **P2 — R3** | Cierre y cutover | P2-14, P1-08 | Contabilidad y activación R3 |
| **P2 — R3** | FX | P2-03 | Bloqueado por política contable `D-14` |
| **Fuera** | P2-05…P2-09 y P3 | — | Requieren plan propio |

---

## 3. Registro de historias refinadas

| Refinamiento | Historia existente | Hallazgos | ADR | Pruebas | Depende de | Criterio de cierre | Dueño | Evidencia actual | Producción |
|---|---|---|---|---|---|---|---|---|---|
| UoW, tenant, RLS y `platform` | `US-PLT-002` / T-005 | P1-03, P1-05, P1-06, P2-10, P2-11 | ADR-007 | T-04, T-07, T-07b, T-08, T-09, T-09b, T-16 | T-002, T-003, DEC-003; módulos de producción | Una conexión/transacción por solicitud; `set_config(..., true)`; RLS forzada en negocio y `platform`; sin GUC residual; rollback cruzado; backfill bajo RLS; contratos versionados | @acamposve | Spike T-04/T-07/T-08/T-09/T-09b; producción parcial | Parcial: UoW y RLS en cuatro esquemas; faltan `platform`, pooling, request scope y backfill |
| Numeración e idempotencia | `US-ADM-006` / T-009 | P1-07, P2-13 | ADR-006, ADR-008 | T-10, T-11, T-11b, T-18 | UoW/T-005; autorización; `D-09` | 10.000 emisiones únicas y sin huecos; ámbitos empresa/sucursal aislados; rollback no consume; misma clave = un efecto; cuerpo distinto = 409 | @acamposve | Spike T-10/T-11/T-11b/T-18; producción con números GUID | No: los números de cotización/pedido son marcadores |
| Stock, kardex y transferencias | `US-INV-004`, `US-INV-005` / T-014, T-015 | P1-01 | ADR-008 | T-01, T-02 | `US-INV-001`, `US-INV-003`; migraciones RLS | Dos ventas de 8 sobre 10: una falla; saldo 2; un movimiento; transferencias sin deadlock y total conservado; kardex = saldo | @acamposve | Spike T-01/T-02; producción solo ítems | No: no hay stock, kardex ni costos de producción |
| Pagos y aplicaciones | `US-TES-002`, `US-TES-003` / T-020 | P1-02 | ADR-008 | T-03 | `US-FAC-001`; CxC; caja | Dos cobros de 60 sobre 100: uno confirma, saldo 40; suma de aplicaciones ≤ saldo en 100 repeticiones; trigger/constraint | @acamposve | Spike T-03 una vez | No: no hay tesorería de producción |
| Crédito y reserva de pedido | `US-VEN-002` / T-017 | P1-01, P2-12 | ADR-008 | T-01, T-17 | Stock/T-015; cliente; configuración de crédito | Pedidos concurrentes no exceden stock ni límite; fallo determinista; reserva y liberación trazables | @acamposve | Sin evidencia de crédito | No: pedido de producción no reserva stock ni valida crédito |
| Posting, outbox y frontera fiscal | `US-FAC-001` / T-019, T-024 | P1-04, P2-02, P2-04, P2-10 | ADR-009 | T-05, T-06, T-14, T-15 | T-009; R2; `D-02`, `D-03`, `D-14` | Exactamente un asiento por factura; `posting_key` único; trigger rechaza desbalance; outbox solo post-commit; una nota fiscal | @acamposve + validación contable | Spike T-05/T-06/T-14/T-15; producción solo borrador | No: no hay emisión fiscal ni contabilidad de producción |
| Cierre de período | `US-CON-002`, `US-CON-003` / T-024 | P2-14 | ADR-008, ADR-009 | T-19 | `US-CON-001`; posting/T-019 | Cierre bloquea período; posting concurrente rechazado; sin asiento fuera; reapertura auditada | @acamposve | Spike T-19 | No: no hay contabilidad de producción |
| Cutover R2→R3 | `US-CON-004` **(propuesta)** / T-025 | P1-08 | ADR-009 | T-12 | Dataset R2; `D-02`; conciliación | Apertura única y conciliada; partidas abiertas; activación; reintento idempotente; diferencia no activa | @acamposve + contabilidad | Spike T-12 simplificado | No: no hay cutover de producción |
| Dependencia R5→R3 | T-004, sin código | P1-09 | Plan §7 `D-04` | T-13 | Roadmap, releases, aceptación | R5 depende de R3 o `NOM-008` fuera de R5; documento y revisión coherentes | @acamposve | Revisión T-004: tabla R5 y T-13 pendientes | Documental |
| Niveles CON/NOM/PRO | Sin historia; addendum ADR-001 | P2-01 | ADR-001, `D-05` | Prueba de arquitectura (nueva) | `D-05` | Niveles corregidos o contratos neutrales; prueba de dependencias prohibidas | @acamposve | Sin prueba de arquitectura | No aplica |
| Backfill bajo RLS | `US-INT-001` / T-005 | P2-11 | ADR-007 | T-16 | UoW/RLS; `D-11` | Dos tenants: cada fila conserva tenant; conteos cuadran; bypass solo excepcional y auditado | @acamposve | Spike no incluye T-16 | No |
| FX y diferencias | `US-TES-004` / T-023 | P2-03 | ADR-009, `D-14` | T-20 | Política contable validada | Asiento y saldo correctos según política; casos dorados aprobados | @acamposve + contabilidad | Bloqueado | No: `T-20` bloqueado |
| Frontera VEN/FAC | `US-FAC-001` | P2-04, `D-17` | ADR-009 | Prueba de contrato (nueva) | `D-17` | Ventas solicita; Facturación emite; una sola nota fiscal; contrato verificado | @acamposve | Decisión aprobada; sin prueba | No |

---

## 4. Matriz de cierre P1

| P1 | Historia refinada | Pruebas | Evidencia en spike | Evidencia de producción | Estado |
|---|---|---|---|---|---|
| P1-01 Sobreventa | `US-INV-004/005` | T-01, T-02 | T-01/T-02 passed | No | **Abierto** |
| P1-02 Pagos | `US-TES-002/003` | T-03 | T-03 passed once | No | **Abierto** |
| P1-03 Transacción | `US-PLT-002` | T-04 | T-04 passed | Parcial: UoW compartida | **Abierto** |
| P1-04 Posting | `US-FAC-001` | T-05, T-06, T-15 | T-05/T-06/T-15 passed | No | **Abierto** |
| P1-05 RLS/pool | `US-PLT-002` | T-07, T-07b, T-08 | T-07/T-08 passed; T-07b pending | Parcial; sin pool de producción | **Abierto** |
| P1-06 `platform` | `US-PLT-002` | T-09, T-09b | T-09/T-09b passed | No hay `platform` de producción | **Abierto** |
| P1-07 Numeración | `US-ADM-006` | T-10, T-11, T-11b | T-10/T-11/T-11b passed | Marcadores GUID | **Abierto** |
| P1-08 Cutover | `US-CON-004` (propuesta) | T-12 | T-12 simplificado | No | **Abierto** |
| P1-09 R5→R3 | T-004 | T-13 documental | Roadmap/releases corregidos | T-13 pendiente | **Abierto** |

**Ningún P1 está cerrado.**

---

## 5. Matriz P2 con impacto directo

| P2 | Historia refinada | Pruebas | Evidencia | Estado |
|---|---|---|---|---|
| P2-01 Niveles CON/NOM/PRO | Addendum ADR-001 | Prueba de arquitectura | No existe | **Abierto** |
| P2-02 Partida doble | `US-FAC-001` / `US-CON-002` | T-14 | Spike T-14 | **Abierto** |
| P2-03 FX | `US-TES-004` | T-20 | Bloqueado `D-14` | **Bloqueado** |
| P2-04 Frontera comercial/fiscal | `US-FAC-001` | Contrato | `D-17` aprobada; sin prueba | **Abierto** |
| P2-10 Contratos API/eventos | `US-PLT-002`, T-015 | Pruebas de contrato | Spike T-15; sin contrato de producción | **Abierto** |
| P2-11 Backfill RLS | `US-INT-001` | T-16 | No ejecutada | **Abierto** |
| P2-12 Crédito | `US-VEN-002` | T-17 | No ejecutada | **Abierto** |
| P2-13 Idempotencia | `US-ADM-006` | T-18 | Spike T-18 | **Abierto** |
| P2-14 Cierre | `US-CON-003` | T-19 | Spike T-19 | **Abierto** |

---

## 6. Spike vs producción

### Evidencia de spike existente

- T-01/T-02: stock y transferencias.
- T-03: pago concurrente, una ejecución.
- T-04: rollback cruzado.
- T-05/T-06/T-15: posting, fallo y duplicado.
- T-07/T-08: pool/tenant y lectura.
- T-09/T-09b: `platform` y RLS.
- T-10/T-11/T-11b: numeración hasta 10.000.
- T-12: cutover simplificado.
- T-14: asiento desbalanceado.
- T-18: idempotencia concurrente.
- T-19: cierre vs posting.

### Implementación de producción existente

- UoW compartida y `set_config` en `src/Avila.Platform/Tenancy`.
- RLS forzada en `admin`, `inventory`, `sales`, `billing`.
- Flujo R1 company/party/item/quote/order/invoice draft.
- CI local de build/pruebas.

### No existe en producción

- `platform` schema y sus políticas.
- Request-scoped UoW y pipeline HTTP de negocio.
- Servicio de numeración e idempotencia.
- Stock, kardex, pagos, crédito, contabilidad, cierre y cutover.
- Contratos API/eventos versionados.
- Pruebas de arquitectura.

**Conclusión:** el spike demuestra mecanismos; no sustituye la implementación de producción.

---

## 7. Fuera de alcance

Siguen abiertos y requieren plan propio:

- `P2-05` reportes R2 sin RF.
- `P2-06` suscripción/dunning/provisioning.
- `P2-07` restauración por tenant.
- `P2-08` RTO/región.
- `P2-09` capacidad/equipo.
- P3 de auditoría tamper-evident, frontend y costos/on-call.

No se resuelven en T-003 ni con ADR-007/008/009.

---

## 8. Qué no hace este documento

- No implementa T-005 ni ninguna otra tarea.
- No cierra P1/P2.
- No crea migraciones ni toca entornos.
- No valida reglas fiscales, contables o laborales.
- No autoriza deploy ni uso de datos reales.
