# Plan de ejecución del proyecto — ÁVILA ERP

**Versión:** 1.0 · **Fecha:** 2026-11-08 · **Estado:** propuesta de ejecución; sin fechas comprometidas

## 1. Propósito y alcance

Este plan convierte la línea base de producto, los releases, el backlog y los planes de hardening en una secuencia ejecutable. No reemplaza ni modifica el alcance normativo de [roadmap.md](product/roadmap.md), [releases.md](../backlog/releases.md), los requisitos ni los ADR; esos documentos siguen siendo la fuente de verdad de cada área.

El desglose operativo se encuentra en el [índice de tareas](../tasks/README.md); cada tarea requiere su propia rama y PR con aprobación de @acamposve.

La prioridad es llegar a **R2 — ÁVILA Comercial**, el primer release vendible, construyendo primero la plataforma segura R1. R3–R6 se mantienen como etapas posteriores y se refinan al acercarse a cada release. No se asignan fechas: capacidad, responsables y estimaciones de calendario aún deben validarse.

## 2. Punto de partida

- La [rebanada vertical R1](architecture/r1-vertical-slice.md) documenta un scaffold funcional `tenant → company → party → item → quote → order → invoice draft`, con pruebas de integración PostgreSQL. **No equivale a R1 cerrado ni a una versión de producción.**
- La misma evidencia identifica pendientes importantes: autenticación y resolución de tenant desde identidad real, endpoints de negocio, reglas de inventario/crédito, numeración, outbox, pruebas de arquitectura y preparación de despliegue.
- ADR-001…009 están registrados en [decisions/README.md](decisions/README.md); los ADR-007…009 constan como aprobados técnicamente, aunque ciertas validaciones externas siguen pendientes.
- La [auditoría arquitectónica](architecture/independent-architecture-audit.md) y el [plan de hardening](architecture/architecture-hardening-plan.md) especifican riesgos y pruebas que deben cerrarse antes de las compuertas correspondientes. Un diseño o spike aprobado no cuenta como implementación verificada.

Por tanto, el primer trabajo es contrastar el estado del código y las pruebas actuales con esta evidencia y el backlog, sin volver a dar por hecho que las tareas de un sprint hipotético están completas.

## 3. Secuencia de ejecución

| Fase | Resultado | Condición para avanzar |
|---|---|---|
| 0. Preparar y cerrar bloqueos | Línea base técnica reconciliada, responsables y decisiones bloqueantes identificados | Decisiones de R1 resueltas o con pendiente explícita; trabajo preparado según Ready |
| 1. R1 — ÁVILA Core | Plataforma multi-tenant segura, operable y auditable | G1: criterios de salida de R1 cumplidos y aislamiento verificado |
| 2. R2 — ÁVILA Comercial | Ciclo comercial y fiscal operable para un piloto | G2: piloto y validación fiscal externa documentados |
| 3. R3 — ÁVILA Finance | Contabilidad, tesorería y fiscalidad completa | G3: conciliación y cierre mensual validados |
| 4. R4 — ÁVILA Retail | Venta POS resiliente con sincronización y hardware validados | G4: pruebas offline y validación fiscal/hardware |
| 5. R5 — ÁVILA People | RRHH y nómina trazables con reglas verificadas | G5: especialista laboral valida cálculos y resultados |
| 6. R6 — ÁVILA Enterprise | Producción, CRM e integraciones para clientes mayores | Criterios de salida R6 y decisiones de publicación satisfechos |

El orden refleja dependencias, no fechas ni una obligación de trabajar en paralelo. R3 y R4 pueden planificarse después de G2 según capacidad. R5 requiere resolver su dependencia contable con R3 antes de fijar su secuencia. R6 depende de R2 y R3.

## 4. Fases y trabajo prioritario

### Fase 0 — Preparar y cerrar bloqueos

1. Verificar qué historias del [sprint backlog](../backlog/sprint-backlog.md) y qué pruebas del scaffold están realmente implementadas y pasan.
2. Confirmar responsables, capacidad, entornos, evidencia requerida y procedimiento de escalamiento para dependencias externas.
3. Resolver los bloqueos de R1: `DEC-003` (identidad) está aprobada; `DEC-001` (cloud) queda en DigitalOcean + Terraform con región pendiente; `DEC-008` (retención) queda aprobada técnicamente con validación legal pendiente, según [decisiones abiertas](decisions/README.md).
4. Convertir los hallazgos de hardening en historias vinculadas a requisitos, pruebas y evidencia. Priorizar coordinación transaccional, contexto tenant/RLS, cobertura de tablas `platform`, concurrencia de numeración y procedimiento de migraciones bajo RLS.
5. Reconciliar la dependencia de R5 respecto de R3 en roadmap, releases y criterios de salida; mantenerla bloqueada hasta que la documentación sea consistente.

**Responsables y capacidad (2026-11-08):** @acamposve asume la propiedad de las decisiones de Arquitectura, Seguridad, Operaciones y Legal para R1. La capacidad comprometida aún no está cuantificada; no se fijan fechas ni sprints por suposición. `DEC-001` mantiene la región pendiente y `DEC-008` la validación legal.

**Estado Fase 0 (2026-11-08):** T-001 y T-002 verificados; T-003 implementado y pendiente de revisión. T-004 sigue pendiente.

**Salida:** backlog listo, bloqueos explícitos con dueño y una línea base de estado contrastada con el repositorio. No se inicia trabajo dependiente de decisiones abiertas como si estas estuvieran aprobadas.

### Fase 1 — R1: ÁVILA Core

Ejecutar las historias R1 en el orden de dependencias del backlog, incluyendo:

- Resolver identidad, membresía, autenticación, MFA, sesiones y selección segura de tenant.
- Conectar el contexto de tenant y la unidad de trabajo a las peticiones; garantizar RLS también en lecturas, pooling y tablas `platform`.
- Implementar permisos con alcance, auditoría append-only, numeración e idempotencia.
- Completar aprovisionamiento y administración, exportación y soporte auditado.
- Completar CI, observabilidad, respaldos y restauración probada.

**Validación de salida:** pruebas de acceso cruzado denegado en API, consultas, exportaciones y jobs; pruebas con conexiones reutilizadas; rollback transaccional; permisos y auditoría; numeración concurrente; restauración ensayada; pipeline y migraciones verificados. Cerrar solo cuando se cumplan los criterios R1 y se abra G1.

### Fase 2 — R2: ÁVILA Comercial

Construir flujos de negocio completos y trazables, no módulos aislados:

1. Catálogo, unidades, almacenes, existencias y kardex.
2. Compras, recepción, costo y devoluciones.
3. Cotización, pedido, reserva de stock y validación concurrente de crédito.
4. Facturación, numeración, snapshot, notas y reversión del original.
5. Caja, cobros, pagos, aplicaciones parciales, cartera y cierre.
6. Importación/migración y reportes operativos con requisitos y criterios definidos.

Implementar idempotencia y controles de concurrencia antes de exponer comandos críticos. Resolver el medio de emisión (`DEC-002`) y las decisiones comerciales y fiscales aplicables al alcance. Toda regla normativa sin validación debe permanecer como `PENDIENTE DE VALIDACIÓN LEGAL / CONTABLE`; no codificar tasas, fórmulas ni formatos definitivos por suposición.

**Validación de salida:** pruebas PostgreSQL de aislamiento, rollback y concurrencia; piloto con inventario conciliado; facturas reales revisadas por especialista; notas de crédito y cobros conciliados; migración con reporte de errores; documentación de usuario y onboarding. Abrir G2 solo al cumplir los criterios de R2.

### Fase 3 — R3: ÁVILA Finance

Refinar los requisitos al iniciar el release y, antes de desarrollar las funciones bloqueadas:

- Validar con especialista el marco contable y las decisiones `DEC-007`, `DEC-009` y `DEC-012`.
- Implementar posting, partida doble, subledger-GL, reversión, períodos y cierre.
- Diseñar, ejecutar y conciliar el cutover contable R2→R3.
- Completar tesorería, conciliación bancaria, retenciones, libros y reglas de cambio aplicables.

**Validación de salida:** pruebas de atomicidad y posting, balanza cuadrada, conciliación sin diferencias, cierre mensual de piloto revisado por contador y evidencia de validación externa. Abrir G3 al cumplir los criterios de R3.

### Fase 4 — R4: ÁVILA Retail

Resolver antes del alcance correspondiente las decisiones de emisión offline, hardware y conflictos de sincronización (`DEC-010`, `DEC-013`, `DEC-017`). Implementar ventas offline, sincronización idempotente, turnos, caja, facturación POS y la matriz acotada de hardware aprobada.

**Validación de salida:** cortes de red y reinicios sin pérdidas ni duplicados, conciliación de cierre Z, factura POS validada y procedimiento de contingencia aprobado. No declarar cumplimiento fiscal offline sin validación especializada.

### Fase 5 — R5: ÁVILA People

No iniciar cálculos normativos definitivos hasta resolver la dependencia contable y obtener validaciones de `DEC-011` y `DEC-014`. Implementar expediente laboral, parámetros versionados, cálculo determinista, trazabilidad, liquidación y provisión/pago conforme a la secuencia aprobada.

**Validación de salida:** casos dorados y recálculo determinista revisados por especialista laboral, conciliación de prestaciones/utilidades, auditoría de datos sensibles y provisión integrada con contabilidad y tesorería.

### Fase 6 — R6: ÁVILA Enterprise

Refinar API pública, webhooks, e-commerce/BI, producción, CRM, SSO y tenant dedicado; resolver `DEC-015` y `DEC-016` antes de las decisiones que bloquean publicación o alcance. Preservar el monolito modular y los contratos entre módulos.

**Validación de salida:** API versionada con sandbox y cuotas, integración de partner usando la API, flujo de producción conciliado, conversión CRM demostrada y tenant dedicado probado con el mismo código.

## 5. Controles transversales

- **Seguridad y tenancy:** el servidor valida tenant y autorización; cada recurso se prueba contra acceso cruzado. RLS es defensa adicional, no el único control.
- **Integridad:** dinero como `decimal` con escala y redondeo definidos; documentos emitidos y asientos son inmutables y se revierten; comandos críticos son idempotentes.
- **Concurrencia y transacciones:** las pruebas de stock, pagos, crédito, numeración, cierres y rollback usan PostgreSQL real cuando corresponda.
- **Cumplimiento:** cada regla regulatoria lleva fuente, vigencia y validación especializada; si falta evidencia se conserva el estado pendiente y no se implementa un valor definitivo.
- **Calidad:** cada historia referencia RF y AC, tiene pruebas sintéticas reproducibles y evidencia; no se cierra una fase por existencia de código o documentos solamente.
- **Gobernanza:** sin fechas comprometidas hasta conocer capacidad real. Cambios de alcance actualizan conjuntamente roadmap, releases y backlog. No se despliega ni se aplican migraciones en entornos compartidos sin autorización.

## 6. Seguimiento y artefactos de control

Al abrir cada fase:

1. Revisar requisitos, criterios de aceptación, ADR/DEC aplicables y dependencias externas.
2. Refinar historias en el backlog con dueño, tamaño relativo, pruebas y criterio verificable de terminado.
3. Registrar riesgos nuevos como `RSK-NNN` y decisiones abiertas como `DEC-NNN`, sin cerrar decisiones por silencio.
4. Mantener evidencia de pruebas, validaciones externas y simulacros junto a los artefactos del release.
5. Revisar la secuencia al cierre de cada release; no introducir fechas sin compromiso explícito y capacidad verificada.

## 7. Documentos fuente

- [Visión](product/vision.md), [alcance funcional](product/functional-scope.md) y [PRD](requirements/PRD.md).
- [Roadmap](product/roadmap.md), [epics](../backlog/epics.md), [sprint backlog](../backlog/sprint-backlog.md) y [criterios de salida](../backlog/releases.md).
- [Auditoría arquitectónica](architecture/independent-architecture-audit.md), [plan de hardening](architecture/architecture-hardening-plan.md), [spike verificado](architecture/spike-verification-2026-11-08.md) y [rebanada vertical](architecture/r1-vertical-slice.md).
- [Decisiones y bloqueos](decisions/README.md), [estrategia de pruebas](architecture/testing-strategy.md) y [registro de fuentes regulatorias](compliance/regulatory-sources.md).
