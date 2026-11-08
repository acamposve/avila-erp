# ÁVILA ERP

**Todo tu negocio. En un solo lugar.**

ÁVILA ERP es una suite ERP SaaS multiempresa y multi-tenant para PYMEs venezolanas, diseñada como un producto modular por etapas: ventas, compras, inventario, facturación fiscal, tesorería, contabilidad, punto de venta, nómina, producción, CRM e integraciones.

> **Estado del repositorio:** línea base documental v1.0 (2026-11-08). Este repositorio contiene **solo documentación**. No hay código de aplicación, proyectos .NET o React, migraciones ni infraestructura. El desarrollo comienza después de aprobar las decisiones abiertas de [docs/decisions/README.md](docs/decisions/README.md).

---

## 1. Qué es ÁVILA ERP

- **Producto:** ERP comercial en la nube para empresas de 3 a 50 empleados, con arquitectura preparada para organizaciones mayores.
- **Mercado inicial:** Venezuela. Distribuidores y mayoristas, comercios minoristas y empresas de servicios; pequeñas industrias en una etapa posterior.
- **Estrategia:** suite completa por releases (R1…R6), no un sistema administrativo desechable.
- **Modelo:** suscripción mensual SaaS con planes y módulos escalables; precios y empaquetado son **hipótesis comerciales** a validar.
- **Posicionamiento:** alternativa moderna, accesible y funcional a los ERP tradicionales del mercado venezolano.

## 2. Principios irrenunciables

1. **Multi-tenancy desde el día uno.** El aislamiento entre organizaciones no es una capa opcional.
2. **Monolito modular.** Sin microservicios en la arquitectura inicial; límites de módulo explícitos y verificables.
3. **El servidor es la autoridad.** Contexto de tenant, permisos, reglas de negocio y cálculos se validan en el servidor.
4. **Dinero exacto.** `decimal` + moneda + tasa versionada; nunca `float`/`double`.
5. **Historia inmutable.** Documentos contabilizados y fiscales no se editan ni se borran: se revierten con contrapartidas.
6. **Reglas parametrizadas y versionadas.** Tasas, alícuotas, retenciones y reglas laborales viven en configuración con vigencia y fuente.
7. **Trazabilidad completa.** Inventario, numeración documental y operaciones sensibles dejan evidencia auditable.
8. **Nada fiscal o laboral se inventa.** Lo no validado se marca `PENDIENTE DE VALIDACIÓN LEGAL / CONTABLE`.

## 3. Arquitectura de referencia (resumen)

| Capa | Tecnología |
|---|---|
| Backend | C# / ASP.NET Core sobre **.NET 10 (LTS)**, Minimal APIs |
| Persistencia | **PostgreSQL 18**, Entity Framework Core 10 / Npgsql |
| Procesos asíncronos | .NET Worker Service + transactional outbox |
| Frontend | **React 19 + TypeScript 5.x + Vite + Material UI 7**, SPA responsive y PWA |
| Observabilidad | OpenTelemetry (traces, métricas, logs) vía OTLP |
| Despliegue | Contenedores sobre infraestructura cloud administrada |

Detalle en [docs/architecture/architecture-overview.md](docs/architecture/architecture-overview.md) y en los ADR de [docs/decisions/](docs/decisions/).

## 4. Mapa documental

| Grupo | Documentos | Para quién |
|---|---|---|
| Producto | [Visión](docs/product/vision.md) · [Modelo de negocio](docs/product/business-model.md) · [Mercado](docs/product/target-market.md) · [Alcance funcional](docs/product/functional-scope.md) · [Roadmap](docs/product/roadmap.md) | Dirección, Product Owners, inversores |
| Requisitos | [PRD](docs/requirements/PRD.md) · [Requisitos funcionales](docs/requirements/functional-requirements.md) · [Requisitos no funcionales](docs/requirements/non-functional-requirements.md) · [Criterios de aceptación](docs/requirements/acceptance-criteria.md) | Producto, QA, desarrollo |
| Arquitectura | [Visión general](docs/architecture/architecture-overview.md) · [Monolito modular](docs/architecture/modular-monolith.md) · [Multi-tenancy](docs/architecture/multi-tenancy.md) · [Seguridad](docs/architecture/security.md) · [Diseño de base de datos](docs/architecture/database-design.md) · [Integraciones](docs/architecture/integration-strategy.md) · [Pruebas](docs/architecture/testing-strategy.md) | Arquitectura, desarrollo, SRE |
| Dominio | [Modelo de dominio](docs/domain/domain-model.md) y un documento por módulo | Desarrollo, Producto, especialistas |
| Cumplimiento | [Fuentes regulatorias](docs/compliance/regulatory-sources.md) · [Fiscal Venezuela](docs/compliance/venezuela-tax.md) · [Facturación](docs/compliance/invoicing.md) · [Laboral](docs/compliance/labor-regulations.md) | Legal, Contabilidad, Producto |
| Decisiones | [Índice y decisiones abiertas](docs/decisions/README.md) · ADR-001…ADR-009 | Arquitectura, dirección |
| Backlog | [Epics](backlog/epics.md) · [Releases](backlog/releases.md) · [Sprint backlog](backlog/sprint-backlog.md) | Producto, desarrollo |
| Gobierno | [AGENTS.md](AGENTS.md) · [CONTRIBUTING.md](CONTRIBUTING.md) · [Glosario](docs/GLOSSARY.md) · [Revisión documental](docs/documentation-review.md) | Todo el equipo y agentes de IA |

Índice completo y orden de lectura sugerido: [docs/README.md](docs/README.md).

## 5. Releases

| Release | Nombre | Foco | ¿Vendible de forma independiente? |
|---|---|---|---|
| R1 | ÁVILA Core | Plataforma, tenancy, identidad, permisos, auditoría, numeración | No (habilitador) |
| R2 | ÁVILA Comercial | Ventas, compras, inventario, facturación VE | **Sí — primer producto comercializable** |
| R3 | ÁVILA Finance | Contabilidad, tesorería completa, retenciones y libros | Sí (complemento) |
| R4 | ÁVILA Retail | POS, hardware, operación offline | Sí (complemento) |
| R5 | ÁVILA People | RRHH y nómina | Sí (complemento) |
| R6 | ÁVILA Enterprise | Producción, CRM, BI, e-commerce, API pública, integraciones | Sí (segmento alto) |

Ver [backlog/releases.md](backlog/releases.md) y [docs/product/roadmap.md](docs/product/roadmap.md). **No se publican fechas comprometidas:** el roadmap se expresa en secuencia, dependencias y criterios de salida.

## 6. Convenciones documentales

- **Idioma:** documentos en español; identificadores de código, tablas, eventos y API en inglés.
- **Requisitos:** `MÓD-NNN` (p. ej. `VEN-001`). No se renumeran; se deprecan.
- **No funcionales:** `NFR-CAT-NNN` (p. ej. `NFR-SEC-003`).
- **Criterios de aceptación:** `AC-MÓD-NN`.
- **Epics:** `EP-MÓD-NN`. **Historias:** `US-MÓD-NNN`.
- **Decisiones abiertas:** `DEC-NNN`. **Riesgos:** `RSK-NNN`. **Fuentes:** `SRC-NNN`.
- **Niveles de obligación:** `DEBE` / `NO DEBE` (obligatorio), `DEBERÍA` (recomendado), `PUEDE` (opcional), conforme al uso de RFC 2119.
- **Estado de una afirmación:** `CONFIRMADO`, `HIPÓTESIS`, `PENDIENTE`, `PENDIENTE DE VALIDACIÓN LEGAL / CONTABLE`.
- Toda afirmación normativa incluye fuente, fecha de consulta, jurisdicción y estado de validación en [docs/compliance/regulatory-sources.md](docs/compliance/regulatory-sources.md).

## 7. Cómo empezar a leer

1. Producto y alcance: [visión](docs/product/vision.md) → [alcance](docs/product/functional-scope.md) → [roadmap](docs/product/roadmap.md).
2. Qué se construye: [PRD](docs/requirements/PRD.md) → [requisitos funcionales](docs/requirements/functional-requirements.md).
3. Cómo se construye: [arquitectura](docs/architecture/architecture-overview.md) → [multi-tenancy](docs/architecture/multi-tenancy.md) → [base de datos](docs/architecture/database-design.md).
4. Reglas de negocio: [modelo de dominio](docs/domain/domain-model.md) y el módulo correspondiente.
5. Antes de tocar código: [AGENTS.md](AGENTS.md) y [CONTRIBUTING.md](CONTRIBUTING.md).

## 8. Estado de decisiones y validación externa

Las decisiones de hardening `D-01`…`D-18` y los ADR-007…ADR-009 están **aprobados técnicamente** (2026-11-08). La verificación con PostgreSQL 18 incluye **19 pruebas de hardening** y **2 pruebas de la rebanada R1**; el detalle está en [spike-verification-2026-11-08.md](docs/architecture/spike-verification-2026-11-08.md) y [r1-vertical-slice.md](docs/architecture/r1-vertical-slice.md). **Ningún P1 se declara cerrado** hasta completar implementación y pruebas de producción.

Las materias fiscales y laborales siguen requiriendo revisión de un especialista venezolano; ver [docs/documentation-review.md](docs/documentation-review.md).

## 9. Licencia y propiedad

**PENDIENTE (DEC-016).** No se distribuye licencia de código hasta que se defina el modelo legal del producto.
