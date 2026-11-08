# Índice de documentación — ÁVILA ERP

Línea base documental **v1.0** (2026-11-08). Todos los documentos están en español y usan la nomenclatura de [GLOSSARY.md](GLOSSARY.md).

## 1. Orden de lectura recomendado

### Para dirección y Product Owners
1. [Visión](product/vision.md)
2. [Mercado objetivo](product/target-market.md)
3. [Modelo de negocio](product/business-model.md)
4. [Alcance funcional](product/functional-scope.md)
5. [Roadmap](product/roadmap.md)
6. [Plan de ejecución del proyecto](project-execution-plan.md)
7. [PRD](requirements/PRD.md)
8. [Decisiones abiertas](decisions/README.md)

### Para arquitectura y desarrollo
1. [AGENTS.md](../AGENTS.md) (reglas obligatorias)
2. [Visión general de arquitectura](architecture/architecture-overview.md)
3. [Monolito modular](architecture/modular-monolith.md)
4. [Multi-tenancy](architecture/multi-tenancy.md)
5. [Diseño de base de datos](architecture/database-design.md)
6. [Seguridad](architecture/security.md)
7. [Estrategia de integración](architecture/integration-strategy.md)
8. [Estrategia de pruebas](architecture/testing-strategy.md)
9. [ADR-001](decisions/ADR-001-modular-monolith.md), [ADR-002](decisions/ADR-002-postgresql.md), [ADR-003](decisions/ADR-003-multi-tenancy.md), [ADR-004](decisions/ADR-004-money-and-rounding.md), [ADR-005](decisions/ADR-005-transactional-outbox.md), [ADR-006](decisions/ADR-006-document-numbering.md), [ADR-007](decisions/ADR-007-transaction-and-tenant-context.md), [ADR-008](decisions/ADR-008-concurrency-by-aggregate.md), [ADR-009](decisions/ADR-009-accounting-and-cutover.md)
10. [Verificación del spike](architecture/spike-verification-2026-11-08.md)
11. [Requisitos funcionales](requirements/functional-requirements.md) y [criterios de aceptación](requirements/acceptance-criteria.md)

### Para especialistas de dominio
- [Modelo de dominio](domain/domain-model.md)
- [Administración](domain/administration.md) · [Ventas](domain/sales.md) · [Compras](domain/purchasing.md) · [Inventario](domain/inventory.md)
- [Facturación](domain/billing.md) · [Tesorería](domain/treasury.md) · [Contabilidad](domain/accounting.md)
- [Nómina y RRHH](domain/payroll.md) · [POS](domain/point-of-sale.md) · [Producción](domain/production.md)

### Para cumplimiento y legal
- [Registro de fuentes regulatorias](compliance/regulatory-sources.md)
- [Fiscal Venezuela](compliance/venezuela-tax.md)
- [Facturación](compliance/invoicing.md)
- [Normativa laboral](compliance/labor-regulations.md)
- [Revisión documental y pendientes](documentation-review.md)

## 2. Inventario documental

| Documento | Propósito | Estado |
|---|---|---|
| [../README.md](../README.md) | Portada, principios, mapa documental | Aprobado como línea base |
| [../AGENTS.md](../AGENTS.md) | Contrato operativo para agentes de IA | Aprobado como línea base |
| [../CONTRIBUTING.md](../CONTRIBUTING.md) | Flujo de trabajo, ramas, PR, pruebas | Aprobado como línea base |
| [GLOSSARY.md](GLOSSARY.md) | Vocabulario y nomenclatura canónica | Aprobado como línea base |
| [product/vision.md](product/vision.md) | Visión, propuesta de valor, problemas, diferenciadores | Aprobado como línea base |
| [product/business-model.md](product/business-model.md) | SaaS, planes (hipótesis), métricas, adquisición | Hipótesis comercial |
| [product/target-market.md](product/target-market.md) | Segmentos, dolores, competencia, canales | Hipótesis de mercado |
| [product/functional-scope.md](product/functional-scope.md) | Alcance por dominio y release | Aprobado como línea base |
| [product/roadmap.md](product/roadmap.md) | Secuencia de releases, riesgos, criterios de salida | Aprobado como línea base |
| [project-execution-plan.md](project-execution-plan.md) | Secuencia de ejecución, dependencias y compuertas del proyecto | Propuesta de ejecución; sin fechas |
| [requirements/PRD.md](requirements/PRD.md) | Objetivos, usuarios, casos de uso, flujos, reglas | Aprobado como línea base |
| [requirements/functional-requirements.md](requirements/functional-requirements.md) | Catálogo `MÓD-NNN` con release y prioridad | Aprobado como línea base |
| [requirements/non-functional-requirements.md](requirements/non-functional-requirements.md) | RNF verificables (`NFR-CAT-NNN`) | Aprobado como línea base |
| [requirements/acceptance-criteria.md](requirements/acceptance-criteria.md) | Criterios `AC-MÓD-NN` verificables | Aprobado como línea base |
| [architecture/architecture-overview.md](architecture/architecture-overview.md) | Componentes, despliegue, observabilidad, DR | Aprobado como línea base |
| [architecture/modular-monolith.md](architecture/modular-monolith.md) | Límites, contratos, dependencias, enforcement | Aprobado como línea base |
| [architecture/multi-tenancy.md](architecture/multi-tenancy.md) | Aislamiento, RLS, ciclo de vida del tenant | Aprobado como línea base |
| [architecture/security.md](architecture/security.md) | Autenticación, autorización, threat model, privacidad | Aprobado como línea base |
| [architecture/database-design.md](architecture/database-design.md) | Esquemas, convenciones, constraints, migraciones | Aprobado como línea base |
| [architecture/integration-strategy.md](architecture/integration-strategy.md) | API, eventos, webhooks, terceros | Aprobado como línea base |
| [architecture/testing-strategy.md](architecture/testing-strategy.md) | Pirámide, aislamiento, concurrencia, CI | Aprobado como línea base |
| [domain/domain-model.md](domain/domain-model.md) | Contextos, agregados, eventos, ownership | Aprobado como línea base |
| [domain/administration.md](domain/administration.md) | Organización, empresas, usuarios, permisos | Aprobado como línea base |
| [domain/sales.md](domain/sales.md) | Cotización a cobro | Aprobado como línea base |
| [domain/purchasing.md](domain/purchasing.md) | Requerimiento a pago | Aprobado como línea base |
| [domain/inventory.md](domain/inventory.md) | Artículos, movimientos, valoración | Aprobado como línea base |
| [domain/billing.md](domain/billing.md) | Comprobantes fiscales y numeración | Aprobado como línea base |
| [domain/treasury.md](domain/treasury.md) | CxC, CxP, caja, bancos, multimoneda | Aprobado como línea base |
| [domain/accounting.md](domain/accounting.md) | Plan de cuentas, asientos, cierres | Aprobado como línea base |
| [domain/payroll.md](domain/payroll.md) | RRHH y nómina | Aprobado como línea base; fórmulas pendientes |
| [domain/point-of-sale.md](domain/point-of-sale.md) | POS, hardware, offline | Aprobado como línea base |
| [domain/production.md](domain/production.md) | BOM, órdenes, costos | Aprobado como línea base |
| [compliance/regulatory-sources.md](compliance/regulatory-sources.md) | Fuentes, fechas, jurisdicción, estado | Registro vivo |
| [compliance/venezuela-tax.md](compliance/venezuela-tax.md) | IVA, ISLR, IGTF, libros, SENIAT | Requiere validación externa |
| [compliance/invoicing.md](compliance/invoicing.md) | Medios de emisión, PA-0093, contingencias | Requiere validación externa |
| [compliance/labor-regulations.md](compliance/labor-regulations.md) | LOTTT, prestaciones, seguridad social | Requiere validación externa |
| [decisions/README.md](decisions/README.md) | Índice de ADR y decisiones abiertas | Vivo |
| [decisions/ADR-001-modular-monolith.md](decisions/ADR-001-modular-monolith.md) | Monolito modular | Aceptado |
| [decisions/ADR-002-postgresql.md](decisions/ADR-002-postgresql.md) | PostgreSQL | Aceptado |
| [decisions/ADR-003-multi-tenancy.md](decisions/ADR-003-multi-tenancy.md) | Multi-tenancy compartido + RLS | Aceptado |
| [decisions/ADR-004-money-and-rounding.md](decisions/ADR-004-money-and-rounding.md) | Dinero y redondeo | Aceptado |
| [decisions/ADR-005-transactional-outbox.md](decisions/ADR-005-transactional-outbox.md) | Outbox | Aceptado |
| [decisions/ADR-006-document-numbering.md](decisions/ADR-006-document-numbering.md) | Numeración documental | Aceptado |
| [decisions/ADR-007-transaction-and-tenant-context.md](decisions/ADR-007-transaction-and-tenant-context.md) | Modelo transaccional y contexto de tenant | Aceptado técnicamente |
| [decisions/ADR-008-concurrency-by-aggregate.md](decisions/ADR-008-concurrency-by-aggregate.md) | Concurrencia por agregado | Aceptado técnicamente |
| [decisions/ADR-009-accounting-and-cutover.md](decisions/ADR-009-accounting-and-cutover.md) | Contabilización y cutover R2→R3 | Aceptado técnicamente; validación contable pendiente |
| [architecture/spike-verification-2026-11-08.md](architecture/spike-verification-2026-11-08.md) | Evidencia T-01…T-19 (19 pruebas) sobre PostgreSQL 18 | Spike verificado; producción pendiente |
| [architecture/r1-vertical-slice.md](architecture/r1-vertical-slice.md) | Esqueleto de producción, migraciones y flujo R1 | Scaffold verificado; endpoints/auth pendientes |
| [backlog/epics.md](../backlog/epics.md) | Epics por release | Aprobado como línea base |
| [backlog/releases.md](../backlog/releases.md) | Contenido y criterios de salida por release | Aprobado como línea base |
| [backlog/sprint-backlog.md](../backlog/sprint-backlog.md) | Sprint 0 y siguientes, historias iniciales | Vivo |
| [documentation-review.md](documentation-review.md) | Hallazgos, contradicciones resueltas, pendientes | Vivo |
| [tasks-validation.md](tasks-validation.md) | Validación de logs y documentos por tarea (`T-001`…`T-037`) | Hallazgos abiertos |

## 3. Reglas de mantenimiento

1. Todo cambio de alcance actualiza `functional-scope.md`, `functional-requirements.md`, `backlog/releases.md` y `backlog/epics.md` en el mismo PR.
2. Toda decisión arquitectónica nueva se registra como ADR antes de implementarse.
3. Toda decisión que no pueda tomarse se registra como `DEC-NNN` en [decisions/README.md](decisions/README.md) con dueño y release que bloquea.
4. Toda afirmación fiscal o laboral nueva requiere entrada en [compliance/regulatory-sources.md](compliance/regulatory-sources.md).
5. Los documentos no se eliminan: se marcan como obsoletos y se enlaza el reemplazo.
6. La revisión de consistencia se repite al cierre de cada release; ver [documentation-review.md](documentation-review.md).
