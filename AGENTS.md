# AGENTS.md — Contrato operativo para agentes de IA

Este archivo es **obligatorio** para cualquier agente de IA (y útil para personas) que trabaje en ÁVILA ERP. Es un índice de reglas, no un reemplazo de la documentación: ante conflicto, prevalece la documentación enlazada y, por encima de todo, una instrucción explícita del responsable humano del repositorio.

**Regla de oro:** si una tarea exige violar una regla de este archivo, el agente **DEBE detenerse**, explicar el conflicto y proponer una decisión (`DEC-NNN`) o un ADR. No debe improvisar.

---

## 1. Lectura obligatoria antes de actuar

| Si vas a… | Lee primero |
|---|---|
| Entender el producto | [Visión](docs/product/vision.md), [alcance](docs/product/functional-scope.md), [PRD](docs/requirements/PRD.md) |
| Crear o modificar un endpoint | [Arquitectura](docs/architecture/architecture-overview.md), [integración](docs/architecture/integration-strategy.md), [seguridad](docs/architecture/security.md) |
| Tocar persistencia o migraciones | [Base de datos](docs/architecture/database-design.md), [multi-tenancy](docs/architecture/multi-tenancy.md) |
| Implementar reglas de negocio | [Modelo de dominio](docs/domain/domain-model.md) y el documento del módulo |
| Tocar dinero, impuestos o numeración | [ADR-004](docs/decisions/ADR-004-money-and-rounding.md), [ADR-006](docs/decisions/ADR-006-document-numbering.md), [facturación](docs/compliance/invoicing.md) |
| Tocar nómina o fiscalidad | [Fiscal](docs/compliance/venezuela-tax.md), [laboral](docs/compliance/labor-regulations.md), [fuentes](docs/compliance/regulatory-sources.md) |
| Escribir pruebas | [Estrategia de pruebas](docs/architecture/testing-strategy.md), [criterios de aceptación](docs/requirements/acceptance-criteria.md) |
| Preparar un PR | [CONTRIBUTING.md](CONTRIBUTING.md) |

Si el documento no existe o está marcado `PENDIENTE`, **no inventes**: registra la decisión y limita el cambio.

## 2. Reglas arquitectónicas no negociables

1. **NO introduzcas microservicios**, buses externos, service mesh ni bases de datos por módulo. La arquitectura es un **monolito modular** ([ADR-001](docs/decisions/ADR-001-modular-monolith.md)).
2. **Respeta los límites de módulo.** Un módulo solo accede a otro mediante su proyecto `Avila.Modules.<Módulo>.Contracts`; nunca mediante tablas, `DbContext`, entidades internas ni SQL directo.
3. **NO dupliques reglas de negocio.** El cálculo de impuestos, totales, inventario, saldos y nómina vive en un único módulo dueño. Las validaciones del frontend son solo experiencia de usuario.
4. **NO cambies una decisión arquitectónica sin autorización.** Un ADR aceptado solo se reemplaza con otro ADR aprobado por el responsable de arquitectura. Un agente nunca se autoaprueba un ADR.
5. **NO agregues dependencias estructurales nuevas** (ORM, mensajería, caché distribuida, autenticación, framework de mediación) sin ADR y aprobación.
6. **Las transacciones no cruzan procesos.** La consistencia fuerte se logra dentro de una transacción local; lo demás va por outbox ([ADR-005](docs/decisions/ADR-005-transactional-outbox.md)).
7. **El servidor es la autoridad.** Prohibido confiar en `tenant_id`, permisos, precios o totales enviados por el cliente.

## 3. Reglas de multi-tenancy (críticas)

- **NUNCA** leas, escribas ni agregues datos de un tenant distinto al del contexto validado.
- Toda tabla de negocio **DEBE** tener `tenant_id NOT NULL`, índices compuestos que lo incluyan y FK compuestas cuando referencien otra tabla de negocio.
- El `tenant_id` se obtiene de la identidad autenticada + membresía validada en el servidor; **no** de un header, query string o body sin validación.
- RLS es **defensa adicional**, no el único mecanismo. Prohibido considerar los filtros globales de EF Core como garantía suficiente.
- Trabajos en segundo plano y eventos **DEBEN** llevar el tenant explícito; prohibido usar estado ambiental entre tenants.
- Un acceso de soporte entre tenants requiere rol de plataforma, justificación, ticket y auditoría.
- Todo cambio en tenancy **DEBE** incluir pruebas de aislamiento (intento de acceso cruzado que falla) y, si aplica, de concurrencia.

Detalle: [multi-tenancy.md](docs/architecture/multi-tenancy.md) y [ADR-003](docs/decisions/ADR-003-multi-tenancy.md).

## 4. Reglas de dominio y datos

- **Dinero:** `decimal` con escala explícita, moneda obligatoria y redondeo determinista; prohibido `float`/`double` y prohibido sumar importes de monedas distintas sin conversión trazable.
- **Inmutabilidad:** un documento emitido, contabilizado o fiscal **NO SE EDITA NI SE BORRA**. Se revierte con nota de crédito, contraasiento u operación equivalente que referencie el original.
- **Numeración:** los comprobantes fiscales usan la secuencia transaccional de [ADR-006](docs/decisions/ADR-006-document-numbering.md); prohibido `SEQUENCE`, `max(id)+1` o numeración en el cliente para documentos fiscales.
- **Idempotencia:** todo comando que crea documentos, pagos o asientos **DEBE** aceptar `Idempotency-Key` y persistir el resultado.
- **Auditoría:** toda operación sensible (anulación, ajuste, cambio de permisos, reapertura, cambio de tasa, acceso de soporte) **DEBE** registrar actor, antes/después, tenant y correlación.
- **Fechas:** almacenar `timestamptz` en UTC; la fecha de negocio es `date` en `America/Caracas`. No usar hora local del servidor.
- **Borrado lógico:** permitido solo donde la ley y el dominio lo admitan; **prohibido** en documentos fiscales y asientos.

## 5. Reglas fiscales y laborales (Venezuela)

- **NO inventes** tasas, alícuotas, porcentajes de retención, formatos, requisitos de homologación, fórmulas de prestaciones ni obligaciones.
- Toda regla fiscal o laboral **DEBE** ser parametrizable, versionada por vigencia y con fuente registrada en [regulatory-sources.md](docs/compliance/regulatory-sources.md) (fuente, fecha de consulta, jurisdicción, estado).
- Si falta evidencia, usa exactamente: `PENDIENTE DE VALIDACIÓN LEGAL / CONTABLE` y deja el punto de extensión; no implementes el valor.
- Prohibido codificar valores regulatorios en constantes, migraciones o seeds sin aprobación de un especialista y sin registro de fuente.
- Los cambios normativos se implementan como nuevas versiones de reglas con `effective_from`/`effective_to`; nunca sobrescribiendo el histórico.

## 6. Seguridad

- **NO almacenes secretos** (contraseñas, tokens, claves, cadenas de conexión) en el repositorio, en tests, en logs ni en capturas.
- **NO registres PII** en logs: documentos de identidad, datos bancarios, salarios, salud. Enmascara siempre.
- Aplica mínimo privilegio y denegación por defecto en permisos y consultas.
- Valida entrada y autoriza cada recurso en el servidor; no confíes en ocultar botones en la UI.
- No desactives TLS, CORS, CSP, rate limiting ni validaciones de seguridad para "probar".
- Un hallazgo de seguridad se reporta de forma privada según [CONTRIBUTING.md](CONTRIBUTING.md).

Detalle: [security.md](docs/architecture/security.md).

## 7. Pruebas y evidencia

- Todo cambio funcional **DEBE** incluir pruebas automatizadas en el nivel adecuado (unitario, integración con PostgreSQL real, contrato, E2E).
- En operaciones críticas, **prioriza pruebas de aislamiento multi-tenant y de concurrencia** (numeración, stock, pagos, cierres, nómina).
- **No consideres una tarea terminada sin evidencia de validación**: salida de pruebas, migración aplicada en limpio, prueba de aislamiento, revisión de logs o captura verificable.
- Prohibido deshabilitar, omitir o "ajustar" una prueba para que pase, salvo que la prueba sea incorrecta; en ese caso se corrige con justificación en el PR.
- No se aceptan pruebas que dependan de hora, orden aleatorio o red externa sin control.

## 8. Git, PR y despliegue

- **PROHIBIDO** hacer commits directamente a `main`; se trabaja en `feature/<id>-<slug>`, `fix/<id>-<slug>` o `docs/<slug>`.
- Todo cambio llega por **Pull Request** con revisión humana. Un agente no aprueba su propio PR.
- **PROHIBIDO** fusionar, hacer push forzado a ramas protegidas, reescribir historia compartida o eliminar ramas sin autorización.
- **PROHIBIDO** desplegar, ejecutar migraciones en entornos compartidos, borrar datos, rotar credenciales o tocar infraestructura sin autorización explícita del responsable.
- **PROHIBIDO** modificar configuraciones de GitHub, reglas de protección, secretos de CI o workflows sin autorización.
- Los commits siguen Conventional Commits y el PR completa la plantilla de [CONTRIBUTING.md](CONTRIBUTING.md).

## 9. Documentación

- Cambios de alcance, requisitos o arquitectura **DEBEN** actualizar la documentación en el mismo PR.
- Nuevos requisitos usan un ID libre del módulo; **no se renumeran IDs existentes**.
- Nuevas decisiones abiertas se registran como `DEC-NNN` con dueño, impacto y release bloqueado.
- Nuevos riesgos se registran como `RSK-NNN`.
- Si detectas una contradicción, **no la resuelvas unilateralmente**: documéntala en [documentation-review.md](docs/documentation-review.md) y propone la corrección.

## 10. Acciones prohibidas (lista rápida)

- Crear microservicios o extraer servicios sin ADR.
- Consultar tablas de otro módulo o saltarse contratos.
- Cambiar el modelo de tenancy, la estrategia de numeración o el modelo de dinero sin aprobación.
- Introducir `float` para dinero, `DateTime.Now`, `Guid.NewGuid()` para PK de negocio o SQL concatenado.
- Desactivar RLS, auditoría, validación de tenant o permisos "temporalmente".
- Inventar normativa, tasas, fórmulas o formatos oficiales.
- Subir secretos, datos reales de clientes o PII a tests/fixtures.
- Commit a `main`, push forzado, deploy, borrado de datos o migración destructiva sin autorización.
- Marcar una tarea como completada sin pruebas y evidencia.

## 11. Cuándo detenerse y escalar

Detente y solicita decisión humana si:

1. La tarea contradice un ADR aceptado o este archivo.
2. Requiere un valor fiscal/laboral no validado.
3. Requiere acceso cruzado entre tenants o datos de producción.
4. Implica una migración destructiva, pérdida de datos o cambio de contrato público.
5. No existe criterio de aceptación verificable para el cambio.
6. La única forma de avanzar es deshabilitar una prueba o un control de seguridad.

Formato de escalamiento: **contexto → regla afectada → opciones → recomendación → decisión requerida**.

## 12. Checklist antes de abrir un PR

- [ ] Leí la documentación del módulo y los ADR aplicables.
- [ ] El cambio respeta límites de módulo y contratos públicos.
- [ ] `tenant_id` presente y validado en servidor; pruebas de aislamiento agregadas.
- [ ] Dinero con `decimal`, moneda y redondeo correctos; sin `float`.
- [ ] Documentos emitidos/contabilizados inmutables; reversión implementada si aplica.
- [ ] Idempotencia en comandos críticos.
- [ ] Auditoría de operaciones sensibles.
- [ ] Numeración fiscal conforme a ADR-006 (si aplica).
- [ ] Sin secretos, PII ni datos reales.
- [ ] Pruebas unitarias/integración/E2E según corresponda y verdes.
- [ ] Documentación y trazabilidad (`RF`, `AC`, `DEC`) actualizadas.
- [ ] Sin cambios directos a `main`, sin deploy, sin operaciones destructivas.

## 13. Índice de referencia

- Producto: [visión](docs/product/vision.md) · [alcance](docs/product/functional-scope.md) · [roadmap](docs/product/roadmap.md)
- Requisitos: [PRD](docs/requirements/PRD.md) · [RF](docs/requirements/functional-requirements.md) · [RNF](docs/requirements/non-functional-requirements.md) · [AC](docs/requirements/acceptance-criteria.md)
- Arquitectura: [overview](docs/architecture/architecture-overview.md) · [modular](docs/architecture/modular-monolith.md) · [tenancy](docs/architecture/multi-tenancy.md) · [BD](docs/architecture/database-design.md) · [seguridad](docs/architecture/security.md)
- Dominio: [modelo](docs/domain/domain-model.md) · [ventas](docs/domain/sales.md) · [inventario](docs/domain/inventory.md) · [facturación](docs/domain/billing.md) · [contabilidad](docs/domain/accounting.md)
- Cumplimiento: [fuentes](docs/compliance/regulatory-sources.md) · [fiscal](docs/compliance/venezuela-tax.md) · [facturación](docs/compliance/invoicing.md) · [laboral](docs/compliance/labor-regulations.md)
- Gobierno: [glosario](docs/GLOSSARY.md) · [contribución](CONTRIBUTING.md) · [decisiones](docs/decisions/README.md) · [revisión](docs/documentation-review.md)
