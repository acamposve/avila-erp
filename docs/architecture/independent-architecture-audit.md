# Auditoría arquitectónica independiente — ÁVILA ERP

**Autor:** Principal Software Architect independiente
**Fecha:** 2026-11-08
**Alcance:** documentación maestra v1.0 (47 documentos), sin acceso a código, entornos ni contratos de proveedores
**Naturaleza:** auditoría documental y técnica; no sustituye una prueba de penetración, una revisión legal ni una validación contable

---

## 1. Veredicto general

# APPROVED WITH CONDITIONS

La documentación es suficientemente madura para **iniciar Sprint 0 y una rebanada vertical de validación**, pero **no** para declarar R1 cerrado ni para llevar R2 a producción. Las condiciones no son cosméticas: se refieren a la semántica de ejecución que determina si el ERP es correcto bajo concurrencia, si el aislamiento multi-tenant sobrevive al pooling y si la contabilidad cuadra cuando falla un participante de la transacción.

No se identificó un hallazgo **P0** con evidencia documental: no hay una fuga inevitable ni una imposibilidad arquitectónica demostrada. Sin embargo, varios **P1** son condiciones de correctitud; si se ignoran, se convierten en P0 en producción.

### Condiciones mínimas para cambiar el veredicto a APPROVED

1. Ratificar un **modelo de ejecución transaccional y de tenant** (conexión, transacción, `SET LOCAL`, pooling, tablas `platform`).
2. Definir y probar **control de concurrencia** para stock, aplicación de pagos, límite de crédito, numeración y cierre de período.
3. Resolver la **ambigüedad de contabilización síncrona vs. outbox** y el **cutover contable R2→R3**.
4. Corregir la **semántica de `branch_id`** en numeración y la **cobertura RLS de `platform`**.
5. Corregir la **dependencia R5→R3** o retirar del alcance R5 los criterios que exigen contabilidad.
6. Obtener validación externa fiscal/laboral antes de R2/R5, según las compuertas ya definidas.

---

## 2. Resumen ejecutivo

ÁVILA ERP tiene una base documental superior a la media de un proyecto pre-código: los ADR están justificados, el modelo de tenancy es explícito, el dinero y la numeración tienen reglas deterministas, la disciplina de cumplimiento es ejemplar y el roadmap evita fechas ficticias. La documentación también identifica correctamente sus incertidumbres regulatorias en lugar de inventar tasas o fórmulas.

El problema principal **no es falta de volumen ni de dominio**: es que el documento describe **invariantes** (“el stock no puede ser negativo”, “el pago no puede exceder el saldo”, “si el asiento falla se revierte todo”) pero **no describe los mecanismos de ejecución** que los hacen ciertos bajo concurrencia y fallos parciales. En un ERP, esa diferencia separa un diseño correcto de una demo.

Los riesgos más serios son:

1. **Sobreventa y sobre-aplicación de pagos** bajo dos transacciones concurrentes, porque no hay estrategia de bloqueo documentada.
2. **Commit parcial entre módulos** si los `DbContext` por módulo no comparten realmente conexión y transacción.
3. **Factura sin asiento** si la contabilización se implementa como consumidor de outbox en lugar de participar de la transacción crítica.
4. **Fuga o corrupción entre tenants** si el contexto RLS no se establece en lecturas/autocommit y en conexiones reutilizadas del pool, o si las tablas `platform` con `tenant_id` quedan fuera de la política.
5. **Numeración fiscal duplicada o inutilizable** por la semántica ambigua de `branch_id` y los `NULL` en índices únicos.
6. **Activar R3 sin contabilizar el histórico de R2**, dejando libros y subledgers descuadrados.
7. **R5 con criterios de salida que dependen de R3** aunque el roadmap declare que depende solo de R1/R2.

Ninguno de estos hallazgos exige reescribir la arquitectura. Todos exigen **decisiones explícitas, diseño de mecanismos y pruebas de fallo/concurrencia** antes de las compuertas correspondientes.

---

## 3. Hallazgos P0–P3

### 3.1 Resumen

| ID | Severidad | Hallazgo | Naturaleza |
|---|---|---|---|
| AUD-P1-01 | P1 | Concurrencia de stock y reservas sin mecanismo de bloqueo | Defecto de diseño |
| AUD-P1-02 | P1 | Aplicación concurrente de pagos sin control de sobre-aplicación | Defecto de diseño |
| AUD-P1-03 | P1 | Coordinación transaccional entre módulos afirmada pero no especificada | Defecto de diseño |
| AUD-P1-04 | P1 | Contabilización síncrona vs. outbox: semántica contradictoria | Defecto de consistencia |
| AUD-P1-05 | P1 | Contexto RLS y pooling/lecturas sin transacción no resueltos | Defecto de diseño |
| AUD-P1-06 | P1 | Cobertura RLS de tablas `platform` con `tenant_id` no definida | Defecto de cobertura |
| AUD-P1-07 | P1 | Esquema de numeración con `branch_id` nullable en PK y unicidad con `NULL` | Defecto de esquema |
| AUD-P1-08 | P1 | Contabilización retroactiva al activar R3 no diseñada | Información/alcance faltante |
| AUD-P1-09 | P1 | R5 depende de R3 para provisión contable, pero el roadmap no lo declara | Defecto de planificación |
| AUD-P2-01 | P2 | Inversión de niveles: CON consume eventos de NOM/PRO (nivel 7) | Defecto de consistencia |
| AUD-P2-02 | P2 | Partida doble no garantizada por constraint; BR-019 afirma “Constraint + prueba” | Defecto de precisión técnica |
| AUD-P2-03 | P2 | Diferencias cambiarias sin fórmula, fecha de tasa ni cuentas | Información faltante |
| AUD-P2-04 | P2 | Frontera “factura comercial” vs. “comprobante fiscal” ambigua | Ambigüedad de dominio |
| AUD-P2-05 | P2 | “Reportes operativos” de R2 sin RF ni AC | Requisito incompleto |
| AUD-P2-06 | P2 | Suscripción SaaS, dunning y aprovisionamiento inicial sin diseño suficiente | Información/alcance faltante |
| AUD-P2-07 | P2 | No hay restauración por tenant; solo PITR de toda la base | Decisión discutible/mejora |
| AUD-P2-08 | P2 | RTO 4 h con región única y recuperación regional recién en R6 | Inconsistencia de NFR |
| AUD-P2-09 | P2 | R2 es grande para un equipo pequeño y no hay plan de capacidad | Riesgo de viabilidad |
| AUD-P2-10 | P2 | Contratos de API y de eventos sin esquema formal/versionado verificable | Documentación faltante |
| AUD-P2-11 | P2 | Migraciones y backfills bajo RLS no tienen procedimiento | Defecto operativo |
| AUD-P2-12 | P2 | Límite de crédito sin control de concurrencia | Defecto de diseño |
| AUD-P2-13 | P2 | Idempotencia sin comportamiento para solicitudes concurrentes en vuelo | Defecto de especificación |
| AUD-P2-14 | P2 | Cierre de período sin bloqueo/cutoff frente a postings concurrentes | Defecto de diseño |
| AUD-P3-01 | P3 | Auditoría append-only no es a prueba de manipulación | Mejora futura |
| AUD-P3-02 | P3 | Arquitectura frontend/offline poco detallada | Documentación faltante |
| AUD-P3-03 | P3 | Sin modelo de costos de infraestructura ni runbooks/on-call | Mejora operativa |

**P0:** no se identificó ninguno con evidencia documental. Se advierte que un P1 no mitigado se comporta como P0 en producción.

---

### 3.2 Hallazgos P1

#### AUD-P1-01 — Concurrencia de stock y reservas sin mecanismo de bloqueo

- **Severidad:** P1
- **Naturaleza:** Defecto de diseño
- **Documento y sección:** [inventory.md §6 y §12](../domain/inventory.md#6-existencias-y-reservas); [PRD BR-020](../requirements/PRD.md#6-reglas-de-negocio-globales); [testing-strategy.md §3.2](testing-strategy.md#32-concurrencia); [acceptance-criteria AC-INV-06](../requirements/acceptance-criteria.md)
- **Descripción:** La documentación enuncia que el stock no puede quedar negativo, que la reserva no puede exceder la existencia y que “el sistema nunca deja saldos inconsistentes”. También exige pruebas de concurrencia. Pero no especifica el mecanismo: no hay `SELECT ... FOR UPDATE` sobre `stock_balances`, no hay actualización condicional atómica, no se elige nivel de aislamiento y no se define el orden de bloqueo entre artículo/almacén/lote. El único bloqueo explícito del corpus está en numeración y outbox.
- **Escenario de fallo:** Existencias de 10 unidades del artículo A en el almacén W. Dos cajeros/vendedores confirman simultáneamente ventas de 8 unidades en dos transacciones `READ COMMITTED`. Ambas leen 10, ambas validan “10 ≥ 8”, ambas insertan el movimiento de salida y ambas confirman. Resultado: existencia −6 o, si se actualiza con valor calculado, 2 con dos salidas de 8; en cualquier caso, el kardex y la existencia no representan la realidad y se vendió stock inexistente.
- **Impacto:** Sobreventa, pérdida de confianza, diferencias de inventario, costo de venta incorrecto, incumplimiento de pedidos y posibles asientos descuadrados contra inventario.
- **Recomendación:** Definir una política única por agregado: bloquear la fila de `stock_balances` con `SELECT ... FOR UPDATE` en orden determinista (tenant, almacén, artículo, lote) antes de validar y mover; o usar una actualización condicional atómica (`UPDATE ... WHERE quantity >= :qty`) y verificar filas afectadas. Las reservas deben participar del mismo bloqueo o de una restricción equivalente. Documentar el nivel de aislamiento y el manejo de deadlocks/reintentos.
- **Prueba o evidencia necesaria:** Prueba de integración con PostgreSQL real y dos conexiones concurrentes: stock 10, dos ventas de 8; exactamente una confirma y la otra recibe un error de negocio; el saldo final es 2 y la suma de movimientos coincide. Repetir con 20 conexiones y con reservas simultáneas.
- **Dependencias:** AUD-P1-03 (transacción), AUD-P2-12 (crédito), ADR-004 (costo), INV-R-05/06, AC-INV-06.

#### AUD-P1-02 — Aplicación concurrente de pagos sin control de sobre-aplicación

- **Severidad:** P1
- **Naturaleza:** Defecto de diseño
- **Documento y sección:** [treasury.md §6 y §12](../domain/treasury.md#6-cobros-y-pagos); [PRD BR-027](../requirements/PRD.md#6-reglas-de-negocio-globales); [testing-strategy.md §3.2](testing-strategy.md#32-concurrencia)
- **Descripción:** Tesorería afirma que “no se aplica más que el saldo (validación transaccional para evitar concurrencia)” y que la suma aplicada no excede el importe. No se define el bloqueo de la partida (`receivables`/`payables`), ni una restricción de suma, ni el tratamiento de dos aplicaciones simultáneas. La frase “validación transaccional” no identifica el mecanismo.
- **Escenario de fallo:** Factura por 100 con saldo abierto 100. Dos cobros de 60 se registran simultáneamente y ambos se aplican a la misma partida. Cada transacción lee saldo 100, valida 60 ≤ 100, inserta su aplicación y confirma. Total aplicado: 120; saldo: −20. El cliente queda con saldo a favor inexistente o la cartera deja de cuadrar contra el mayor.
- **Impacto:** Cartera incorrecta, cobros duplicados, diferencias contables, reclamos de clientes y conciliación imposible.
- **Recomendación:** Bloquear la partida con `SELECT ... FOR UPDATE` antes de calcular el saldo; validar la suma de aplicaciones dentro de la misma transacción; añadir una restricción/trigger que impida `SUM(applied) > original_amount`; definir el comportamiento ante aplicación y reversión concurrentes. Si se opta por concurrencia optimista, usar `row_version` y reintento controlado, no solo una lectura previa.
- **Prueba o evidencia necesaria:** Dos cobros concurrentes de 60 contra una partida de 100: uno confirma, el otro falla con error de saldo; saldo 40; suma de aplicaciones 60. Repetir con aplicación y reversión simultáneas y con pagos en moneda distinta.
- **Dependencias:** AUD-P1-03, AUD-P2-03 (FX), TES-R-02/03, BR-027.

#### AUD-P1-03 — Coordinación transaccional entre módulos afirmada pero no especificada

- **Severidad:** P1
- **Naturaleza:** Defecto de diseño
- **Documento y sección:** [modular-monolith.md §5.1](modular-monolith.md#51-unidad-de-trabajo); [ADR-001 §2 y §5](../decisions/ADR-001-modular-monolith.md); [architecture-overview.md §6.2](architecture-overview.md#62-contabilización-de-un-documento); [database-design.md §2](database-design.md#2-esquemas)
- **Descripción:** Se afirma que “todos los módulos participantes comparten la conexión y la transacción mediante `IUnitOfWork`/`ITransactionCoordinator`”. Sin embargo, la estructura define un `DbContext` por módulo y un esquema por módulo. No se especifica cómo varios `DbContext` comparten la misma `DbConnection` y `DbTransaction`, en qué orden se ejecutan los `SaveChanges`, quién hace `Commit`, ni qué pasa si un `SaveChanges` falla después de otro. La interfaz existe como nombre, no como contrato de ejecución.
- **Escenario de fallo:** `EmitirFactura` usa `BillingDbContext` para insertar el comprobante y `TreasuryDbContext` para crear la partida por cobrar. Si cada contexto abre su propia conexión, el comprobante se confirma y la partida falla; o el comprobante queda persistido sin número consistente si el orden se invierte. Si se comparte conexión pero no transacción, el `SaveChanges` de Billing ya hizo commit y Tesorería no puede revertirlo.
- **Impacto:** Documentos fiscales sin cartera, cartera sin asiento, numeración consumida sin documento, y una supuesta consistencia fuerte que no existe. Es exactamente el tipo de fallo que un ERP no puede permitir.
- **Recomendación:** Aprobar un **modelo de ejecución** explícito: una conexión y una transacción por caso de uso; contextos de módulo enlistados en la misma transacción; `SaveChanges` coordinado con un único `Commit`; política de excepciones y rollback; prohibición de abrir conexiones propias dentro del caso de uso. documentar la implementación con EF Core (por ejemplo, `Database.UseTransaction` sobre una conexión compartida) y probar el fallo forzado de cada participante. Si algún efecto no puede ser transaccional, debe ir por outbox y declararse eventual.
- **Prueba o evidencia necesaria:** Prueba de integración que fuerza una excepción en Tesorería y en Contabilidad después de que Facturación insertó el comprobante; verificar cero filas de factura, cero partida y cero asiento, y que el número no se consumió. Repetir con un fallo de conexión a mitad de la transacción.
- **Dependencias:** AUD-P1-04, AUD-P1-05, ADR-001, ADR-005, AUD-P2-11.

#### AUD-P1-04 — Contabilización síncrona vs. outbox: semántica contradictoria

- **Severidad:** P1
- **Naturaleza:** Defecto de consistencia
- **Documento y sección:** [modular-monolith.md §5.2](modular-monolith.md#52-contabilización); [domain-model.md §5 y §6](../domain/domain-model.md#6-consistencia-entre-contextos); [architecture-overview.md §6.2](architecture-overview.md#62-contabilización-de-un-documento); [billing.md §5](../domain/billing.md#5-emisión); [accounting.md §10](../domain/accounting.md#10-eventos)
- **Descripción:** La documentación presenta dos descripciones que no están conciliadas:
  1. La contabilización es un **evento de dominio en proceso, dentro de la transacción**, y si el asiento falla se revierte la factura.
  2. `billing.invoice.issued.v1` es un evento **“Dominio + integración”** que se publica en outbox, y el flujo de billing muestra `FAC->>OUT: InvoiceIssued` antes de que el worker procese la autorización.
  La documentación no define si el handler de Contabilidad se ejecuta antes del `Commit` (síncrono) o como consumidor del outbox (después del `Commit`), ni si se ejecuta dos veces. `accounting.entry.posted.v1` es solo de integración, pero no aclara el mecanismo de posting.
- **Escenario de fallo:** El equipo implementa el handler de Contabilidad como consumidor del outbox porque el evento se llama “integración”. La factura se confirma y responde al usuario; el worker falla al crear el asiento por una cuenta inexistente. La factura existe, la cartera existe y el asiento no. El cierre de mes detecta documentos sin contabilizar, pero el daño ya está hecho y el usuario no lo sabía. Alternativamente, si se implementa doble (síncrono y outbox), se duplica el asiento si no hay idempotencia estricta.
- **Impacto:** Pérdida de integridad financiera, libros incompletos, cierres bloqueados y confianza erosionada. Es un riesgo de auditoría, no solo técnico.
- **Recomendación:** Separar formalmente dos categorías:
  - **Evento crítico de contabilización:** se despacha en proceso, antes del `Commit`, con un contrato explícito, orden definido y fallo que revierte la transacción. Si Contabilidad no está activa, el documento se marca como “no contabilizado” y se bloquea el cierre hasta regularizar.
  - **Evento de integración:** se escribe en outbox después del `Commit`, con idempotencia y consumidores eventuales.
  Añadir un ADR específico y una prueba que demuestre que un fallo contable impide la factura (cuando CON está activo) y que el mismo evento no se procesa dos veces.
- **Prueba o evidencia necesaria:** Factura con cuenta contable inválida: la transacción completa falla, no queda factura ni número consumido. Factura válida: un solo asiento, un solo evento de integración. Repetición del evento: no duplica el asiento.
- **Dependencias:** AUD-P1-03, AUD-P1-08, ADR-005, AC-CON-04, AC-FAC-01.

#### AUD-P1-05 — Contexto RLS y pooling/lecturas sin transacción no resueltos

- **Severidad:** P1
- **Naturaleza:** Defecto de diseño
- **Documento y sección:** [ADR-003 §3 y §4](../decisions/ADR-003-multi-tenancy.md); [multi-tenancy.md §2 y §4.3](multi-tenancy.md#43-rls); [database-design.md §8](database-design.md#8-rls-y-roles)
- **Descripción:** La política RLS usa `current_setting('app.tenant_id', true)` y ADR-003 afirma que `SET LOCAL app.tenant_id` se ejecuta “al inicio de la transacción”. No se define:
  - si **toda** petición, incluidas las lecturas, abre una transacción explícita;
  - cómo se establece el contexto cuando un endpoint solo lee;
  - el ciclo de vida de la conexión en el pool (¿se usa `SET LOCAL` o `SET`? ¿quién resetea?);
  - el comportamiento con PgBouncer en modo transacción, que el propio ADR menciona como riesgo pero no decide;
  - cómo se obtiene el tenant en jobs globales y en health checks.
- **Escenario de fallo:** Un `GET /api/v1/invoices` se ejecuta en autocommit. El interceptor intenta `SET LOCAL app.tenant_id` en una transacción implícita que termina antes de la consulta; la consulta siguiente no tiene `app.tenant_id`, RLS devuelve cero filas (fail closed) y el usuario ve una pantalla vacía. Peor: si se usa `SET` a nivel de sesión para evitar el problema, la conexión vuelve al pool con el tenant A; la siguiente petición del tenant B reutiliza la conexión antes de que el interceptor la resetee y puede leer datos de A. Ambos escenarios son conocidos y no están cerrados en el diseño.
- **Impacto:** Disponibilidad (lecturas vacías) o fuga entre tenants (si se usa sesión y no se resetea). El segundo caso es crítico.
- **Recomendación:** Elegir y documentar uno de dos modelos:
  1. **Transacción por petición:** abrir transacción explícita para toda operación, incluidas lecturas; `SET LOCAL` al inicio; `Commit`/`Rollback` al final. Es el modelo más seguro y encaja con `SET LOCAL`.
  2. **Interceptor de conexión:** `SET` al adquirir y `RESET`/`DISCARD ALL` al devolver, con pruebas de reutilización. Solo si se demuestra que el reset es confiable.
  Definir pooler (Npgsql pooling, PgBouncer transaction/session), tamaño, timeout y pruebas de reutilización. Añadir una prueba que ejecute tenant A y B secuencialmente sobre la misma conexión física y verifique que B nunca ve A.
- **Prueba o evidencia necesaria:** Prueba de integración con pool de 1 conexión: A consulta, B consulta; B solo ve B. Prueba de endpoint de lectura sin transacción explícita: falla de forma controlada o funciona según el modelo elegido. Prueba con PgBouncer si se despliega.
- **Dependencias:** AUD-P1-06, AUD-P1-03, ADR-002, ADR-003, NFR-ISO-001/004.

#### AUD-P1-06 — Cobertura RLS de tablas `platform` con `tenant_id` no definida

- **Severidad:** P1
- **Naturaleza:** Defecto de cobertura
- **Documento y sección:** [database-design.md §2, §5 y §8](database-design.md#8-rls-y-roles); [ADR-003 §2](../decisions/ADR-003-multi-tenancy.md); [multi-tenancy.md §4.3](multi-tenancy.md#43-rls)
- **Descripción:** El esquema `platform` contiene tablas que son datos de negocio o control de negocio y que, por su naturaleza, deberían estar acotadas por tenant: `document_sequences`, `idempotency_records`, `audit_log`, `subscriptions`, `usage_meters`, `support_access_grants`, entre otras. La regla de RLS se expresa como “cada tabla de negocio”, pero no se define si `platform` está incluido o exento. Tampoco se definen políticas para esas tablas ni un rol separado que las aísle. El documento de base de datos solo dice “Habilitar y forzar RLS en cada tabla de negocio; validar en CI que no exista tabla sin política”, sin enumerar el alcance.
- **Escenario de fallo:** Si el rol `avila_app` conserva `SELECT` sobre `platform.audit_log` porque se usa para la consola de auditoría, una consulta de soporte que olvide el filtro `tenant_id` devuelve eventos de todas las organizaciones. O un bug en el repositorio de numeración hace `SELECT ... FROM platform.document_sequences WHERE company_id = ...` sin tenant; si hay empresas con el mismo ID lógico en distintos tenants, se lee o incrementa la secuencia equivocada.
- **Impacto:** Fuga de metadatos y auditoría entre tenants, corrupción de numeración fiscal, exposición de planes y uso. Es un fallo de aislamiento, aunque no sea de documentos transaccionales.
- **Recomendación:** Clasificar cada tabla de `platform` como:
  - **tenant-scoped:** RLS forzada con `tenant_id`; o
  - **platform-only:** sin acceso para `avila_app`, solo rol de plataforma y funciones auditadas.
  Enumerar las tablas y sus políticas en el diseño de base de datos. Añadir la introspección de CI para `platform`, no solo para los esquemas de módulo. Probar acceso cruzado a `audit_log`, `document_sequences` y `subscriptions`.
- **Prueba o evidencia necesaria:** Introspección que falla si una tabla con `tenant_id` en `platform` no tiene RLS o no está en la lista de excepciones aprobadas. Prueba de consulta sin filtro con rol `avila_app`: 0 filas de otros tenants. Prueba de actualización de secuencia ajena: rechazo.
- **Dependencias:** AUD-P1-05, AUD-P1-07, ADR-003, NFR-ISO-002/003.

#### AUD-P1-07 — Esquema de numeración con `branch_id` nullable en PK y unicidad con `NULL`

- **Severidad:** P1
- **Naturaleza:** Defecto de esquema / decisión no documentada
- **Documento y sección:** [ADR-006 §2](../decisions/ADR-006-document-numbering.md); [multi-tenancy.md §4.2](multi-tenancy.md#42-claves-y-restricciones); [billing.md §5](../domain/billing.md#5-emisión)
- **Descripción:** El DDL de `platform.document_sequences` declara `branch_id uuid` (nullable) y luego lo incluye en la `PRIMARY KEY`. En PostgreSQL, una `PRIMARY KEY` obliga a `NOT NULL`, por lo que la columna no puede ser nula aunque se declare así; la semántica es ambigua: el DDL sugiere que es opcional, pero la PK la obliga. Además, el índice único documentado para documentos incluye `branch_id`; en PostgreSQL los `NULL` se consideran distintos en un índice único, por lo que dos documentos de empresa sin sucursal podrían compartir serie y número. No se declara si todo documento tiene sucursal obligatoria ni qué pasa con documentos a nivel de empresa.
- **Escenario de fallo:** Se intenta emitir una factura a nivel de empresa sin sucursal. El `INSERT` en `document_sequences` falla por `NOT NULL` en `branch_id`, o se usa un UUID sentinel. Si la tabla de documentos acepta `branch_id NULL`, el índice único permite dos filas con `(tenant, company, NULL, serie, 1)` porque los `NULL` no colisionan. Resultado: dos comprobantes con el mismo número.
- **Impacto:** Imposibilidad de numerar cierto tipo de documento o duplicación de números fiscales. Es un riesgo regulatorio y de auditoría.
- **Recomendación:** Decidir explícitamente una de dos:
  1. **Sucursal obligatoria:** `branch_id NOT NULL`, FK compuesta a `branches`, y todos los documentos tienen sucursal. Documentar la regla.
  2. **Numeración a nivel empresa:** clave con `branch_id` opcional modelada con una columna `scope`/`branch_key` no nula (por ejemplo `COALESCE(branch_id, uuid_nil())`) o `UNIQUE NULLS NOT DISTINCT` (PostgreSQL 15+) y una PK que no dependa de un nullable.
  Añadir pruebas para documentos con y sin sucursal y para concurrencia en ambos ámbitos.
- **Prueba o evidencia necesaria:** Migración en limpio que crea la tabla; inserción de dos documentos de empresa sin sucursal; se espera rechazo de duplicado; inserción de documentos de dos sucursales; numeración independiente. Prueba de concurrencia por ámbito.
- **Dependencias:** AUD-P1-06, ADR-006, AC-FAC-01, AC-ADM-06.

#### AUD-P1-08 — Contabilización retroactiva al activar R3 no diseñada

- **Severidad:** P1
- **Naturaleza:** Información/alcance faltante
- **Documento y sección:** [domain-model.md §6](../domain/domain-model.md#6-consistencia-entre-contextos); [roadmap.md §4 y §5](../product/roadmap.md); [functional-scope.md §2](../product/functional-scope.md#2-matriz-de-módulos-por-release); [accounting.md §7](../domain/accounting.md#7-períodos-y-cierre)
- **Descripción:** R2 se vende sin Contabilidad. R3 introduce Contabilidad. El documento de dominio afirma: “Cuando la contabilidad no está activa (R2 sin `CON`), el evento de contabilización no se publica y se registra la razón; al activarse, se genera el asiento de apertura y se concilia el histórico.” No existe epica, requisito, criterio de aceptación, procedimiento de cutover ni diseño de datos para:
  - reconstruir los asientos de facturas, cobros y compras de R2;
  - definir la fecha de corte y los saldos de apertura;
  - conciliar CxC/CxP, inventario e ingresos;
  - tratar documentos de R2 emitidos después del corte pero antes de la activación;
  - evitar duplicar asientos si se reprocesan eventos.
- **Escenario de fallo:** Un cliente opera R2 durante seis meses. Al activar R3, se cargan saldos de apertura “a mano” sin reconstruir el detalle. La suma de CxC del subledger no coincide con la cuenta de clientes; el inventario de apertura no coincide con el kardex; el estado de resultados no refleja las ventas de R2. El contador no puede cerrar el primer mes de R3 y descubre que el ERP “no tiene historia contable”.
- **Impacto:** Bloquea la venta de R3 a clientes existentes, obliga a trabajo manual, erosiona la confianza y puede requerir una migración costosa. Es un riesgo comercial directo para el modelo de expansión por releases.
- **Recomendación:** Diseñar un **cutover contable R2→R3** como parte de R3:
  - fecha de corte y período de apertura;
  - generación de asientos de apertura desde subledgers y kardex;
  - replay idempotente de eventos históricos o importación de saldos con detalle;
  - conciliación obligatoria antes de habilitar posting corriente;
  - reporte de diferencias y bloqueo de cierre hasta resolverlas;
  - pruebas con un dataset R2 representativo.
  Añadir requisito y AC explícitos; no dejarlo como frase en el modelo.
- **Prueba o evidencia necesaria:** Dataset con 3 meses de facturas, cobros, compras y stock R2; activar R3; generar apertura; balanza cuadrada; CxC/CxP conciliadas; kardex vs inventario; cero asientos duplicados; cierre del primer mes posible.
- **Dependencias:** AUD-P1-03, AUD-P1-04, AUD-P2-02, CON-010/012, AC-CON-05.

#### AUD-P1-09 — R5 depende de R3 para provisión contable, pero el roadmap no lo declara

- **Severidad:** P1
- **Naturaleza:** Defecto de planificación
- **Documento y sección:** [roadmap.md §7](../product/roadmap.md); [releases.md §6](../../backlog/releases.md); [payroll.md §14](../domain/payroll.md#14-dependencias); [functional-scope.md §2](../product/functional-scope.md#2-matriz-de-módulos-por-release)
- **Descripción:** El roadmap declara que R5 depende de “R1; R2 (tesorería)” y la tabla de releases dice “R1/R2”. Pero los criterios de salida de R5 incluyen “Asientos de nómina conciliados con contabilidad”, el requisito `NOM-008` es “Provisión contable de nómina”, el documento de nómina declara dependencia de `CON` para asientos y provisiones, y la matriz de alcance pone “Integración nómina” en la columna R5 de Contabilidad. Sin R3, esos criterios no pueden cumplirse.
- **Escenario de fallo:** El equipo planifica R5 después de R2, sin R3. Al llegar al criterio de salida, no hay módulo de contabilidad para conciliar; se pospone el criterio o se implementa una integración ad hoc. El release se declara “casi terminado” y arrastra deuda que impide vender nómina a clientes que necesitan contabilidad.
- **Impacto:** Roadmap inviable, release R5 sin criterio de salida real, retrabajo y riesgo de prometer una nómina que no se integra con finanzas.
- **Recomendación:** Corregir la dependencia a **R3** (o declarar explícitamente que `NOM-008` y los criterios contables se difieren a R6 y retirarlos de R5). Actualizar `roadmap.md`, `releases.md`, `epics.md` y la matriz de alcance. Añadir una prueba de integración nómina→contabilidad en el alcance R5.
- **Prueba o evidencia necesaria:** Revisión de consistencia que muestre R5 dependiendo de R3 y todos los requisitos contables de R5 cubiertos por un release predecessor. Prueba de corrida de nómina con asiento y provisión conciliados.
- **Dependencias:** AUD-P1-08, NOM-008, CON-006, EP-NOM-03.

---

### 3.3 Hallazgos P2

#### AUD-P2-01 — Inversión de niveles: CON consume eventos de NOM/PRO (nivel 7)

- **Severidad:** P2
- **Naturaleza:** Defecto de consistencia
- **Documento y sección:** [ADR-001 §5](../decisions/ADR-001-modular-monolith.md); [modular-monolith.md §3](modular-monolith.md#3-mapa-de-módulos-y-niveles); [domain-model.md §5](../domain/domain-model.md#5-eventos-de-dominio-e-integración); [accounting.md §6](../domain/accounting.md#6-reglas-de-contabilización)
- **Descripción:** La regla dice que un módulo solo puede depender de módulos de nivel inferior. Contabilidad está en nivel 6; Nómina y Producción están en nivel 7. Sin embargo, Contabilidad consume `payroll.run.approved.v1`, `payroll.termination.completed.v1`, `production.consumption.posted.v1` y `production.output.posted.v1`; para hacerlo referencia sus contratos. Eso es una dependencia de nivel 6 hacia nivel 7, prohibida por la propia regla. El diagrama lo oculta diciendo que las flechas de CON son “suscripción a eventos, no dependencia de código”, pero la suscripción requiere referenciar el contrato.
- **Escenario de fallo:** La prueba de arquitectura configurada según ADR-001 fallaría si se aplica literalmente y `Accounting` referencia `Payroll.Contracts` o `Production.Contracts`. El equipo relaja la prueba “temporalmente” o excluye Contabilidad, y la regla deja de ser aplicable.
- **Impacto:** La regla de dependencias pierde valor; el build y el orden de módulos se vuelven ambiguos; futuros módulos copian la excepción.
- **Recomendación:** Elegir una solución explícita:
  1. subir Contabilidad a nivel 8 y bajar/publicar los eventos de NOM/PRO como contratos compartidos de integración;
  2. definir un proyecto `Avila.Contracts.IntegrationEvents` neutral, sin dependencia de módulo, y que Contabilidad consuma ese contrato;
  3. declarar una excepción formal “event subscription may point upward” y adaptar la prueba de arquitectura.
  Actualizar ADR-001, diagrama y prueba.
- **Prueba o evidencia necesaria:** Prueba de arquitectura que verifique la regla elegida; build completo; revisión del mapa de dependencias.
- **Dependencias:** ADR-001, AUD-P1-04, NOM/PRO events.

#### AUD-P2-02 — Partida doble no garantizada por constraint; BR-019 afirma “Constraint + prueba”

- **Severidad:** P2
- **Naturaleza:** Defecto de precisión técnica
- **Documento y sección:** [PRD BR-019](../requirements/PRD.md#6-reglas-de-negocio-globales); [accounting.md §5](../domain/accounting.md#5-asientos) y [§11](../domain/accounting.md#11-invariantes); [database-design.md §6](database-design.md#6-tablas-críticas)
- **Descripción:** BR-019 dice que todo asiento cumple partida doble y la verificación es “Constraint + prueba”. Un `CHECK` de fila no puede sumar líneas de otras filas; para garantizar `SUM(debe)=SUM(haber)` hace falta un trigger diferido, un procedimiento almacenado o una validación de aplicación. La documentación no especifica ninguno. `CON-R-01` es una invariante de dominio, no una restricción de base de datos.
- **Escenario de fallo:** Una migración de datos, un script de soporte o un bug de aplicación inserta un asiento con debe 100 y haber 90. La base lo acepta; la balanza queda descuadrada y los estados financieros no cuadran. La prueba unitaria no cubre SQL directo.
- **Impacto:** Estados financieros inválidos; confianza en la contabilidad comprometida.
- **Recomendación:** Implementar una validación a nivel de base de datos (trigger diferido sobre `journal_entries`/`journal_lines` o procedimiento de posting) y mantener la validación de dominio. Corregir la redacción de BR-019 para no prometer un `CHECK` inexistente.
- **Prueba o evidencia necesaria:** Intento de inserción directa de asiento desbalanceado: rechazo. Inserción balanceada: aceptación. Prueba de concurrencia de dos líneas y de reversión. Verificación de que el trigger no bloquea la creación por partes si se usa diferido.
- **Dependencias:** AUD-P1-04, CON-R-01, CON-002.

#### AUD-P2-03 — Diferencias cambiarias sin fórmula, fecha de tasa ni cuentas

- **Severidad:** P2
- **Naturaleza:** Información faltante
- **Documento y sección:** [ADR-004 §2.4](../decisions/ADR-004-money-and-rounding.md); [treasury.md §7](../domain/treasury.md#7-multimoneda); [accounting.md §6](../domain/accounting.md#6-reglas-de-contabilización); [functional-requirements TES-010](../requirements/functional-requirements.md)
- **Descripción:** Se dice que al cobrar/pagar en moneda distinta se registra la diferencia cambiaria en cuentas específicas y que la revaluación es un proceso explícito. No se define: tasa aplicable (documento, cobro, cierre), base de cálculo (importe original vs. saldo), tratamiento de pagos parciales, cuenta de ganancia/pérdida, redondeo, ni asiento de revaluación. `TES-010` está marcado “Pendiente validación contable”, pero no hay una decisión (`DEC`) específica ni una fórmula propuesta.
- **Escenario de fallo:** Factura USD 100 con tasa 30 (VES 3.000). Cobro parcial USD 60 con tasa 40 (VES 2.400) aplicado a la factura. ¿La diferencia es 60×(40−30)=600? ¿O se compara el valor residual? ¿Se reconoce ganancia/pérdida en el cobro o solo al cierre? Sin regla, dos implementaciones producen asientos distintos y ninguna es auditable.
- **Impacto:** Resultados financieros incorrectos, diferencias no explicables, problemas en auditoría y en conciliación de cartera.
- **Recomendación:** Definir una política contable de FX con ejemplos numéricos: tasa snapshot del documento, tasa del cobro/pago, cálculo de diferencia realizada por aplicación, tratamiento de saldo remanente, cuentas de ganancia/pérdida, redondeo y revaluación de cierre. Validar con contador y registrarlo como decisión.
- **Prueba o evidencia necesaria:** Casos dorados: pago total, pago parcial, pago en otra moneda, reversión, revaluación; cada uno con asiento esperado y conciliación de saldo.
- **Dependencias:** ADR-004, TES-010, CON-006, AUD-P1-02, especialista contable.

#### AUD-P2-04 — Frontera “factura comercial” vs. “comprobante fiscal” ambigua

- **Severidad:** P2
- **Naturaleza:** Ambigüedad de dominio
- **Documento y sección:** [sales.md §1 y §7](../domain/sales.md); [billing.md §1](../domain/billing.md#1-propósito-y-límites); [functional-requirements VEN-006/VEN-007/FAC-003](../requirements/functional-requirements.md); [functional-scope.md §3.3 y §3.5](../product/functional-scope.md)
- **Descripción:** Facturación se declara “el único módulo que emite comprobantes fiscales”, pero Ventas incluye “facturación desde pedido o nota de entrega” (`VEN-006`), “notas de crédito y débito comerciales” (`VEN-007`) y “devoluciones” (`VEN-008`). No queda claro si Ventas crea un documento comercial propio, si solo solicita la emisión a Facturación, ni si una nota de crédito comercial y una fiscal son el mismo documento o dos. La terminología “facturación” en Ventas y “Facturación” como módulo agrava la ambigüedad.
- **Escenario de fallo:** El equipo implementa una “nota de crédito comercial” en Ventas para una devolución y, por separado, una nota de crédito fiscal en Facturación. El stock se reingresa una vez, la cartera se revierte dos veces o la comisión se revierte en el documento equivocado. El cliente recibe dos comprobantes con efectos distintos.
- **Impacto:** Duplicidad de documentos, inconsistencia fiscal/cartera/stock, confusión de usuarios y de auditores.
- **Recomendación:** Definir el ciclo documental único:
  - Ventas crea cotización/pedido/entrega y **solicita** la emisión fiscal; no crea comprobantes fiscales.
  - Facturación es dueña del comprobante fiscal y de la nota de crédito/débito fiscal.
  - Si existe un documento comercial no fiscal (nota de entrega, remito), se nombra explícitamente y no se confunde con factura.
  - Una devolución comercial y su nota de crédito fiscal deben ser el mismo caso de uso orquestado, no dos documentos independientes.
  Actualizar requisitos, dominio y glosario.
- **Prueba o evidencia necesaria:** Flujo de devolución total y parcial: un solo reingreso de stock, una sola nota de crédito, una sola reversión de comisión y cartera. Prueba de que Ventas no puede emitir numeración fiscal.
- **Dependencias:** VEN-006/007/008, FAC-003, AUD-P1-04, glosario.

#### AUD-P2-05 — “Reportes operativos” de R2 sin RF ni AC

- **Severidad:** P2
- **Naturaleza:** Requisito incompleto
- **Documento y sección:** [roadmap.md R2](../product/roadmap.md); [functional-scope.md §3](../product/functional-scope.md); [functional-requirements.md](../requirements/functional-requirements.md)
- **Descripción:** R2 promete “reportes operativos: ventas, compras, stock, kardex, saldos de cartera y caja”, pero no hay requisito funcional, AC ni épica específica para reportes. Las consultas de módulo pueden existir, pero un reporte tiene criterios propios (filtros, totales, exportación, rendimiento, permisos). La única referencia es `FAC-008` (representación) y `ADM-016` (exportación de tenant), que no cubren el catálogo.
- **Escenario de fallo:** El equipo implementa pantallas de listado y asume que son los reportes. El cliente pide “ventas por vendedor y período” con totales y exportación; no está en el alcance, no tiene AC y se convierte en trabajo no planificado o en un reporte inconsistente con la definición financiera.
- **Impacto:** Alcance no controlado, expectativas comerciales incumplidas, reportes que no cuadran con contabilidad.
- **Recomendación:** Añadir un requisito de “catálogo mínimo de reportes R2” con IDs, criterios de aceptación, filtros, totales, permisos y exportación; o retirar la promesa del roadmap. Definir el reporte como consulta de solo lectura con totales verificados contra el modelo transaccional.
- **Prueba o evidencia necesaria:** Un AC por reporte con dataset conocido y totales esperados; prueba de rendimiento y de aislamiento.
- **Dependencias:** R2, FAC-008, ADM-016, NFR-PER-005.

#### AUD-P2-06 — Suscripción SaaS, dunning y aprovisionamiento inicial sin diseño suficiente

- **Severidad:** P2
- **Naturaleza:** Información/alcance faltante
- **Documento y sección:** [business-model.md §3](../product/business-model.md); [administration.md §2](../domain/administration.md); [functional-requirements PLT-001…006](../requirements/functional-requirements.md); [multi-tenancy.md §7](multi-tenancy.md#7-ciclo-de-vida-del-tenant)
- **Descripción:** El modelo comercial depende de suscripción mensual, planes y límites, pero el único requisito es `PLT-004` (“suscripción, cobro recurrente, prorrateo y gestión de impago”). No hay modelo de estados de suscripción, dunning, prorrateo, cambio de plan, suspensión automática, reactivación, reembolsos ni conciliación de cobros SaaS. Tampoco se define el aprovisionamiento inicial completo: roles base, series, plan de cuentas, almacenes, reglas fiscales y datos de ejemplo. `ADM-001` crea la organización, pero no siembra el mínimo operable.
- **Escenario de fallo:** Un cliente nuevo se registra; la organización queda creada pero sin plan de cuentas ni series; no puede facturar hasta que un operador configure todo manualmente. Un cliente impaga; no hay proceso automático de suspensión y el equipo sigue prestando servicio. Un upgrade de plan no prorratea y el cliente recibe un cobro inesperado.
- **Impacto:** Onboarding lento, ingresos no cobrados, churn por fricción y operación manual no escalable.
- **Recomendación:** Definir un dominio de suscripción (plan, contrato, ciclo, factura SaaS, pago, dunning, suspensión) y un “tenant provisioning checklist” con seeds mínimos. Decidir si la facturación SaaS es manual en R2 (válido) y documentarlo; no dejarlo implícito.
- **Prueba o evidencia necesaria:** E2E de alta→plan→primer documento; prueba de impago→suspensión→reactivación; prueba de cambio de plan con prorrateo; checklist de provisioning verificable.
- **Dependencias:** PLT-001…006, ADM-001, multi-tenancy §7, DEC-006.

#### AUD-P2-07 — No hay restauración por tenant; solo PITR de toda la base

- **Severidad:** P2
- **Naturaleza:** Decisión discutible/mejora
- **Documento y sección:** [architecture-overview.md §10](architecture-overview.md#10-recuperación-ante-fallos); [database-design.md §11](database-design.md#11-respaldos-y-recuperación); [multi-tenancy.md §7](multi-tenancy.md#7-ciclo-de-vida-del-tenant)
- **Descripción:** La estrategia de respaldo es PITR de la base completa. La exportación de tenant existe, pero no hay procedimiento de restauración por tenant. Si un tenant borra o corrompe sus datos, restaurar la base completa revierte también a todos los demás tenants a un punto anterior. La exportación no reconstruye relaciones, documentos y archivos con integridad referencial.
- **Escenario de fallo:** Un administrador de un tenant elimina por error 200 facturas (o un bug las corrompe). Para recuperarlo, se restaura la base completa de las 03:00; se pierden las operaciones de todos los tenants desde esa hora. No hay forma de restaurar solo ese tenant.
- **Impacto:** Pérdida de datos para clientes inocentes, SLA incumplido, riesgo reputacional y legal.
- **Recomendación:** Definir un procedimiento de restauración lógica por tenant (export completo + import idempotente + verificación de integridad) o una modalidad de tenant dedicado para clientes que lo requieran. Probar el procedimiento con un tenant sintético y medir RTO/RPO por tenant.
- **Prueba o evidencia necesaria:** Exportar tenant A, borrar datos, restaurar solo A, verificar documentos, saldos, archivos y numeración; los demás tenants intactos. Documento con pasos y tiempos.
- **Dependencias:** ADM-016, DEC-008, PLT-006, backups.

#### AUD-P2-08 — RTO 4 h con región única y recuperación regional recién en R6

- **Severidad:** P2
- **Naturaleza:** Inconsistencia de NFR
- **Documento y sección:** [architecture-overview.md §7](architecture-overview.md#7-despliegue) y [§10](architecture-overview.md#10-recuperación-ante-fallos); [non-functional-requirements NFR-AVA-001/003](../requirements/non-functional-requirements.md); [ADR-002](../decisions/ADR-002-postgresql.md)
- **Descripción:** La arquitectura propone una región única con multi-AZ y declara RTO ≤ 4 h. Ante pérdida de la región, el plan es “restauración en región secundaria (R6)”. Antes de R6 no hay infraestructura secundaria documentada. Un RTO de 4 h para pérdida regional no es alcanzable solo con backups en la misma región.
- **Escenario de fallo:** La región cloud sufre una interrupción prolongada. No hay réplica ni infraestructura en otra región; el servicio no vuelve en 4 h. El equipo descubre que el RTO prometido solo aplica a fallos de AZ, no a pérdida regional.
- **Impacto:** Incumplimiento de disponibilidad, clientes sin servicio, presión para un plan de recuperación improvisado.
- **Recomendación:** Aclarar el alcance del RTO: “fallo de AZ/instancia” vs “pérdida regional”. Si se promete 4 h para pérdida regional, diseñar backup cross-region y runbook desde R2; si no, ajustar el NFR y comunicar la limitación. Prueba de restauración en otra región.
- **Prueba o evidencia necesaria:** Simulacro de pérdida regional (o tabletop) con tiempos medidos; documento de RTO/RPO por escenario.
- **Dependencias:** NFR-AVA-001/003, DEC-001, DEC-015, R6.

#### AUD-P2-09 — R2 es grande para un equipo pequeño y no hay plan de capacidad

- **Severidad:** P2
- **Naturaleza:** Riesgo de viabilidad
- **Documento y sección:** [roadmap.md R2](../product/roadmap.md); [releases.md R2](../../backlog/releases.md); [sprint-backlog.md](../../backlog/sprint-backlog.md); [ADR-001 §1](../decisions/ADR-001-modular-monolith.md)
- **Descripción:** R2 contiene 56 requisitos funcionales, 17 epics, inventario, ventas, compras, facturación fiscal, tesorería, migración y reportes, más la integración con un proveedor fiscal. El sprint backlog asume 10 sprints de 2 semanas sin declarar tamaño de equipo, velocidad, especialistas ni capacidad de QA/legal. No hay estimación ni plan de staffing; el roadmap evita fechas, lo cual es correcto, pero también evita demostrar viabilidad.
- **Escenario de fallo:** Un equipo de 3–5 personas intenta R2 en el orden propuesto; la integración fiscal y la migración consumen el margen; se recorta calidad o se retrasa el lanzamiento sin criterio. El “primer producto comercializable” se convierte en un año de trabajo.
- **Impacto:** Riesgo comercial y de ejecución; el producto puede llegar tarde o con deuda.
- **Recomendación:** Declarar capacidad supuesta (equipo, roles, dedicación), identificar el camino crítico (fiscal, migración, inventario), y definir un “R2 mínimo vendible” con recortes explícitos si la capacidad no alcanza. Separar el alcance “must” del “should” y validar con un piloto.
- **Prueba o evidencia necesaria:** Plan de capacidad aprobado; backlog priorizado con Must/Should; criterio de recorte; revisión de avance por sprint.
- **Dependencias:** R2, R5/R3 dependencies, DEC-006.

#### AUD-P2-10 — Contratos de API y de eventos sin esquema formal/versionado verificable

- **Severidad:** P2
- **Naturaleza:** Documentación faltante
- **Documento y sección:** [integration-strategy.md §2 y §3](integration-strategy.md); [modular-monolith.md §7](modular-monolith.md#7-eventos); [testing-strategy.md §5](testing-strategy.md#5-pruebas-de-contrato-y-api)
- **Descripción:** Se definen principios de API (v1, Problem Details, cursor, idempotencia) y un catálogo de eventos, pero no existe un artefacto OpenAPI/JSON Schema, no se define el formato exacto del sobre de evento (solo un ejemplo), no hay reglas de compatibilidad verificables ni pruebas de contrato concretas. Para un monolito modular, los contratos internos son la principal defensa contra el acoplamiento; para R6, los contratos externos son la promesa a partners.
- **Escenario de fallo:** Ventas y Facturación evolucionan el mismo DTO con campos distintos; el evento `order.confirmed.v1` cambia de significado sin cambiar de versión; un partner integra y se rompe. No hay test que detecte la incompatibilidad.
- **Impacto:** Acoplamiento, regresiones y rotura de integraciones.
- **Recomendación:** Definir contratos versionados con esquema (OpenAPI 3.1 para API, JSON Schema para eventos), generación de clientes/tipos, pruebas de compatibilidad hacia atrás y política de deprecación. Publicar el catálogo de eventos con payload y semántica.
- **Prueba o evidencia necesaria:** CI que valide OpenAPI contra implementación y esquemas de eventos contra fixtures; prueba de compatibilidad de una versión anterior.
- **Dependencias:** ADR-005, INT-001…003, R6.

#### AUD-P2-11 — Migraciones y backfills bajo RLS no tienen procedimiento

- **Severidad:** P2
- **Naturaleza:** Defecto operativo
- **Documento y sección:** [database-design.md §9](database-design.md#9-migraciones); [multi-tenancy.md §10](multi-tenancy.md#10-rendimiento-y-operación); [ADR-003 §2](../decisions/ADR-003-multi-tenancy.md)
- **Descripción:** Las migraciones usan `avila_migrator`, mientras que la aplicación usa `avila_app` con RLS. No se define cómo se ejecutan backfills que leen o escriben filas de todos los tenants: ¿el migrador tiene `BYPASSRLS`? ¿Se itera tenant por tenant con `SET LOCAL`? ¿Cómo se valida que un backfill no mezcla tenants? Tampoco se define el orden de migraciones entre esquemas con FKs compartidas.
- **Escenario de fallo:** Una migración agrega una columna `tenant_id` y hace `UPDATE ... SET tenant_id = ...` sin contexto. Si el migrador tiene `BYPASSRLS`, actualiza todas las filas; si no, no ve ninguna. En ambos casos el resultado es incorrecto o incompleto.
- **Impacto:** Migraciones fallidas, datos corruptos o bloqueos de despliegue.
- **Recomendación:** Definir un procedimiento de migración/backfill: rol, `BYPASSRLS` explícito y auditado o iteración por tenant; transacciones por lote; verificación de conteos; rollback; pruebas en base con datos. No permitir backfills ad hoc en producción.
- **Prueba o evidencia necesaria:** Migración en base con dos tenants y backfill; verificar que cada fila conserva su tenant y que los conteos cuadran. Prueba de migración en limpio y desde la versión anterior.
- **Dependencias:** ADR-002, ADR-003, AUD-P1-05/06.

#### AUD-P2-12 — Límite de crédito sin control de concurrencia

- **Severidad:** P2
- **Naturaleza:** Defecto de diseño
- **Documento y sección:** [sales.md §6](../domain/sales.md#6-crédito-y-reservas); [PRD BR-023](../requirements/PRD.md#6-reglas-de-negocio-globales); [VEN-R-04](../domain/sales.md#11-invariantes)
- **Descripción:** La regla `saldo_abierto + pedido_pendiente ≤ límite` se valida al confirmar el pedido, pero no se especifica bloqueo del cliente, de la cartera o de una tabla de crédito. Dos pedidos concurrentes pueden leer el mismo saldo y ambos pasar.
- **Escenario de fallo:** Cliente con límite 1.000 y saldo 900. Dos pedidos de 200 se confirman simultáneamente; ambos leen 900 y validan 1.100 > 1.000? No, 900+200=1.100 > 1.000, ambos deberían fallar; si el saldo es 700, ambos pasan (700+200=900 ≤ 1.000) y el cliente queda en 1.100. El riesgo es real con pedidos concurrentes.
- **Impacto:** Exceso de crédito, morosidad y pérdida.
- **Recomendación:** Bloquear una fila de crédito por cliente o serializar la confirmación por cliente; validar el compromiso pendiente en la misma transacción. Añadir prueba concurrente.
- **Prueba o evidencia necesaria:** Dos pedidos concurrentes que juntos exceden el límite; exactamente uno confirma o ambos se rechazan; el saldo comprometido nunca excede el límite.
- **Dependencias:** AUD-P1-01/02, VEN-009, BR-023.

#### AUD-P2-13 — Idempotencia sin comportamiento para solicitudes concurrentes en vuelo

- **Severidad:** P2
- **Naturaleza:** Defecto de especificación
- **Documento y sección:** [database-design.md §6](database-design.md#6-tablas-críticas); [AGENTS.md §4](../../AGENTS.md); [integration-strategy.md §2](integration-strategy.md)
- **Descripción:** Se define `Idempotency-Key` y una tabla con PK `(tenant_id, idempotency_key, operation)`, pero no se especifica qué ocurre si dos solicitudes con la misma clave llegan simultáneamente: ¿la segunda espera, recibe 409, recibe la respuesta de la primera, o intenta insertar y falla por unique? Tampoco se define el tiempo de retención de la respuesta, el tamaño máximo, ni qué pasa si la primera solicitud falla después de persistir el registro.
- **Escenario de fallo:** El cliente reintenta por timeout mientras la primera solicitud sigue en curso. La segunda llega antes del commit, no encuentra registro, ejecuta la operación y crea un segundo documento. O encuentra un registro `pending` y devuelve una respuesta vacía/incorrecta.
- **Impacto:** Duplicados en pagos/facturas, exactamente lo que la idempotencia debía evitar.
- **Recomendación:** Definir la semántica: insertar la clave al inicio con estado `in_progress` y restricción única; si existe `in_progress`, responder 409/retry-after o esperar; al completar, guardar respuesta y estado; TTL; limpieza; comportamiento ante fallo. Probar concurrencia real.
- **Prueba o evidencia necesaria:** Dos solicitudes simultáneas con la misma clave: un solo efecto, segunda respuesta consistente. Reintento tras timeout: misma respuesta.
- **Dependencias:** ADR-005, TES-003/004, FAC-001, AUD-P1-02.

#### AUD-P2-14 — Cierre de período sin bloqueo/cutoff frente a postings concurrentes

- **Severidad:** P2
- **Naturaleza:** Defecto de diseño
- **Documento y sección:** [accounting.md §7](../domain/accounting.md#7-períodos-y-cierre); [CON-R-10](../domain/accounting.md#11-invariantes); [functional-requirements CON-005/CON-011](../requirements/functional-requirements.md)
- **Descripción:** El cierre “verifica asientos pendientes, conciliación y documentos sin contabilizar” y luego bloquea el período. No se define un bloqueo del período durante la verificación ni un cutoff para postings concurrentes. Un asiento puede entrar entre la verificación y el bloqueo.
- **Escenario de fallo:** El cierre lee “no hay pendientes”, luego un cobro concurrente contabiliza un asiento, y el cierre bloquea el período. El asiento queda en un período cerrado o se pierde; la balanza del cierre no incluye el cobro.
- **Impacto:** Cierre incorrecto, diferencias contables, reapertura no planificada.
- **Recomendación:** Bloquear el período (fila `periods` con `FOR UPDATE` o estado `closing`) antes de verificar; rechazar postings durante el cierre; definir cutoff y proceso de reapertura. Probar concurrencia cierre/posting.
- **Prueba o evidencia necesaria:** Cierre concurrente con posting; uno falla o el posting espera; la balanza final incluye todo o nada.
- **Dependencias:** AUD-P1-03/04, CON-005/011, AUD-P2-02.

---

### 3.4 Hallazgos P3

#### AUD-P3-01 — Auditoría append-only no es a prueba de manipulación

- **Severidad:** P3
- **Naturaleza:** Mejora futura
- **Documento y sección:** [security.md §9](security.md#9-auditoría); [database-design.md §6](database-design.md#6-tablas-críticas)
- **Descripción:** `platform.audit_log` es append-only para el rol de aplicación, pero no tiene hash chain, firma, WORM ni verificación de integridad. Un administrador de base de datos o un error operativo puede alterar filas. Para auditoría fiscal puede no ser suficiente.
- **Escenario de fallo:** Un operador con acceso a la base modifica un registro de auditoría para ocultar un cambio de precio; no hay evidencia de manipulación.
- **Impacto:** Pérdida de confianza en la auditoría; posible incumplimiento.
- **Recomendación:** Evaluar hash encadenado por tenant, exportación a almacenamiento WORM o firma periódica; al menos, restringir el acceso y monitorear cambios. Documentar la limitación.
- **Prueba o evidencia necesaria:** Verificación de hash/exportación; prueba de alteración detectada. 
- **Dependencias:** DEC-008, auditoría fiscal, R3+.

#### AUD-P3-02 — Arquitectura frontend/offline poco detallada

- **Severidad:** P3
- **Naturaleza:** Documentación faltante
- **Documento y sección:** [architecture-overview.md §5](architecture-overview.md#5-stack-tecnológico); [point-of-sale.md §5](../domain/point-of-sale.md#5-operación-offline)
- **Descripción:** El frontend se describe por stack y carpetas, pero no hay arquitectura de estado, caché, invalidación, permisos de UI, manejo de sesión multi-tenant, i18n, accesibilidad ni estrategia offline más allá de POS. Para un equipo pequeño, la mitad del producto es frontend.
- **Escenario de fallo:** Cada módulo implementa su propio estado/caché; los permisos se ocultan solo en UI; el cambio de tenant deja datos cacheados; el POS offline no reconcilia catálogo/precios.
- **Impacto:** Inconsistencia de UI, fugas de datos en caché, deuda de frontend.
- **Recomendación:** Documento de arquitectura frontend con estado servidor, claves de caché por tenant, invalidación, permisos, sesión, i18n, accesibilidad y offline. No es bloqueante para R1.
- **Prueba o evidencia necesaria:** Prueba de cambio de tenant sin datos residuales; prueba de caché; auditoría de accesibilidad.
- **Dependencias:** R2, POS R4.

#### AUD-P3-03 — Sin modelo de costos de infraestructura ni runbooks/on-call

- **Severidad:** P3
- **Naturaleza:** Mejora operativa
- **Documento y sección:** [business-model.md §4](../product/business-model.md); [architecture-overview.md §8](architecture-overview.md#8-observabilidad)
- **Descripción:** El modelo de negocio tiene una estructura de costos cualitativa, pero no un modelo de costo por tenant/uso (DB, storage, observabilidad, soporte). Tampoco hay runbooks, guardias, escalamiento ni presupuesto de error. Para un SaaS de PYMEs, el margen depende de esto.
- **Escenario de fallo:** Cien tenants generan costos de observabilidad y almacenamiento que consumen el margen; no hay alerta de costo ni plan de capacidad. Un incidente fuera de horario no tiene runbook.
- **Impacto:** Pérdida de margen, respuesta lenta, decisiones reactivas.
- **Recomendación:** Modelo de costo unitario por tenant (infra + soporte), presupuesto de error SLO, runbooks de incidentes y guardias. Puede posponerse a R2, pero debe existir antes de escalar.
- **Prueba o evidencia necesaria:** Cálculo de costo por tenant; simulación de 100/1.000 tenants; runbook probado en simulacro.
- **Dependencias:** R2, NFR-OBS, NFR-AVA.

---

## 4. Evaluación por área

### A. Multi-tenancy

**Fortalezas:** modelo organización→empresa→sucursal coherente; `tenant_id` obligatorio; FK compuestas; RLS forzada; contexto validado en servidor; pruebas de acceso cruzado; archivos y caché con prefijo de tenant. Es uno de los diseños más sólidos del corpus.

**Brechas:** no se define el ciclo de vida de la conexión/pool ni la transacción de lectura (`AUD-P1-05`); no se define la cobertura de `platform` (`AUD-P1-06`); la numeración tiene una ambigüedad de ámbito (`AUD-P1-07`); migraciones/backfills bajo RLS (`AUD-P2-11`).

**Riesgo residual:** el aislamiento es correcto en el modelo, pero depende de una implementación de contexto que aún no está especificada. Sin las pruebas de pool y de tablas `platform`, no puede afirmarse que no haya acceso cruzado.

### B. Integridad financiera

**Fortalezas:** dinero decimal, redondeo determinista, snapshot de tasa, inmutabilidad, reversión por contrapartida, outbox, numeración, auditoría. El ADR-004 es claro y útil.

**Brechas:** concurrencia de pagos (`AUD-P1-02`); coordinación transaccional (`AUD-P1-03`); posting síncrono vs. outbox (`AUD-P1-04`); partida doble no garantizada en BD (`AUD-P2-02`); FX incompleto (`AUD-P2-03`); cierre concurrente (`AUD-P2-14`); idempotencia en vuelo (`AUD-P2-13`).

**Veredicto:** **Parcial.** El diseño conceptual es correcto, pero no está listo para producción hasta que los P1 y los P2 de FX/cierre estén cerrados.

### C. Inventario

**Fortalezas:** kardex reconstruible desde movimientos; costo promedio móvil explicado con ejemplo; reservas, lotes, vencimientos, seriales; separación de movimientos y saldos.

**Brechas:** no hay mecanismo de bloqueo para stock/reservas (`AUD-P1-01`); la reserva puede exceder la existencia si dos transacciones compiten; no se define el orden de bloqueo entre artículo/almacén/lote; el costo promedio concurrente no está especificado; devoluciones y cancelaciones no tienen regla de concurrencia.

**Veredicto:** **Parcial.** La reconstrucción es posible, la operación concurrente no está garantizada.

### D. Facturación venezolana

**Fortalezas:** separación explícita entre emisión comercial y fiscal; Facturación como único emisor; adaptador `IFiscalEmitter`; estados `PendienteAutorizacion`; contingencia marcada; fuentes con fecha y estado; no se inventan tasas ni formatos.

**Brechas:** la frontera “factura comercial” vs. “comprobante fiscal” sigue ambigua (`AUD-P2-04`); el proveedor no está seleccionado (`DEC-002`); la validez fiscal de la emisión offline y la anulación están pendientes (`DEC-010`, `DEC-020`); la PA-0093 está sustentada en una fuente secundaria y debe verificarse con Gaceta y especialista.

**Veredicto:** **Adecuado con condiciones.** La arquitectura de integración es razonable; la operación fiscal no puede declararse lista hasta la validación externa y la selección del medio.

### E. Arquitectura .NET

**Fortalezas:** monolito modular coherente; ensamblados por módulo y contratos; sin microservicios; sin MediatR; esquema por módulo; migraciones por módulo; Worker para outbox; OpenTelemetry; EF Core/Npgsql.

**Brechas:** coordinación de `DbContext`/transacción (`AUD-P1-03`); inversión de niveles CON vs. NOM/PRO (`AUD-P2-01`); contratos sin esquema formal (`AUD-P2-10`); registros de eventos y dispatcher no definidos; no hay prueba de arquitectura concreta más allá de la intención.

**Veredicto:** **Adecuado con condiciones.** La arquitectura es viable para un equipo pequeño, pero la semántica de transacción y la regla de dependencias deben cerrarse antes de R1.

### F. Operación SaaS

**Fortalezas:** onboarding y planes identificados; suspensión/exportación; outbox; observabilidad; backups PITR; SLOs; soporte auditado; modelo de negocio y métricas.

**Brechas:** suscripción/dunning/provisioning incompletos (`AUD-P2-06`); restauración por tenant (`AUD-P2-07`); RTO/región (`AUD-P2-08`); capacidad/equipo (`AUD-P2-09`); costos/on-call (`AUD-P3-03`); frontend (`AUD-P3-02`).

**Veredicto:** **Parcial.** Hay una base operativa, pero no un modelo completo de SaaS comercial.

---

## 5. Decisiones arquitectónicas que deben ratificarse

| Decisión | Estado documental | Por qué debe ratificarse | Resultado esperado |
|---|---|---|---|
| **ADR-001 — Monolito modular** | Aceptado en el corpus | La regla de niveles se contradice con CON→NOM/PRO (`AUD-P2-01`) | Niveles y excepciones corregidos; prueba de arquitectura ejecutable |
| **ADR-002 — PostgreSQL 18** | Aceptado | Falta decidir pooling, RLS de plataforma, réplica y recuperación regional | Modelo operativo de conexión y recuperación |
| **ADR-003 — Multi-tenancy** | Aceptado | No cubre contexto de lectura/pool ni tablas `platform` (`AUD-P1-05/06`) | Política de ejecución de tenant y cobertura RLS completa |
| **ADR-004 — Dinero y redondeo** | Aceptado | FX y redondeo de diferencias no están cerrados (`AUD-P2-03`) | Política FX y casos dorados |
| **ADR-005 — Outbox** | Aceptado | No separa posting crítico de integración (`AUD-P1-04`) | Dos categorías de eventos y contrato de dispatcher |
| **ADR-006 — Numeración** | Aceptado | `branch_id` ambiguo y unicidad con NULL (`AUD-P1-07`) | Semántica de ámbito y constraint correcta |
| **ADR-007 propuesto — Modelo de ejecución transaccional y de tenant** | No existe | Es la condición para AUD-P1-03/05/06 | Conexión, transacción, `SET LOCAL`, pooling y roles |
| **ADR-008 propuesto — Control de concurrencia por agregado** | No existe | Es la condición para AUD-P1-01/02/12 | Bloqueo/optimismo por stock, cartera, crédito y período |
| **ADR-009 propuesto — Contabilización crítica y cutover R2→R3** | No existe | Es la condición para AUD-P1-04/08 | Posting síncrono, idempotencia y activación contable |
| **DEC propuesta — Ámbito de numeración (empresa/sucursal)** | No existe | AUD-P1-07 | Regla de negocio y esquema |
| **DEC propuesta — Dependencia R5→R3** | No existe | AUD-P1-09 | Roadmap y alcance coherentes |

No se propone crear microservicios, cambiar el motor de base de datos ni introducir un broker externo. Las decisiones son de **semántica de ejecución**, no de estilo.

---

## 6. Decisiones que pueden posponerse

| Decisión | Release que la necesita | Por qué puede esperar | Condición |
|---|---|---|---|
| `DEC-004` almacenamiento documental | R2 | No bloquea R1 ni la rebanada vertical | Resolver antes de PDFs/adjuntos |
| `DEC-006` planes y precios | Lanzamiento R2 | No bloquea arquitectura; sí el negocio | Validar con pilotos |
| `DEC-007` retenciones IVA/ISLR | R2 parcial / R3 | R2 puede operar sin retenciones si el cliente no las requiere | Validación fiscal antes de ofrecerlas |
| `DEC-009` integración bancaria | R3 | R3 puede iniciar con importación de archivos | Validar API/archivo |
| `DEC-010` emisión fiscal offline | R4 | No afecta R1–R3 | Validación legal antes de POS offline fiscal |
| `DEC-011`/`DEC-014` nómina | R5 | No bloquea R2/R3 | Validación laboral y motor |
| `DEC-012` ajuste por inflación | R3 | Puede implementarse después del núcleo contable | Validación contable |
| `DEC-013` hardware POS | R4 | No bloquea R2/R3 | Matriz de hardware |
| `DEC-015` BI/réplica | R6 | R2/R3 pueden usar reportes y exportación | Medir necesidad |
| `DEC-016` licencia | Lanzamiento público | No bloquea Sprint 0 ni R1 | Decisión legal antes de publicar |
| `DEC-017` conflictos offline | R4 | POS es R4 | Definir política antes de offline |
| `DEC-018` recosteo | R3+ | No es necesario para R2 | Validar con contabilidad |
| `DEC-019` comisiones | R2 | Puede implementarse al final de R2 | Validar con comercial |
| `PLT-006` tenant dedicado | R6 | No bloquea SaaS compartido | Cliente enterprise |
| `ADM-021` SSO/SCIM | R6 | No bloquea R1–R5 | Cliente enterprise |

**No pueden posponerse:** `DEC-001` (región), `DEC-003` (identidad), `DEC-008` (retención/residencia), `DEC-002` (medio de emisión para R2), la semántica de numeración, el modelo de ejecución transaccional y la cobertura RLS de `platform`.

---

## 7. Riesgos que requieren especialistas externos

| Riesgo / materia | Especialista | Evidencia documental | Qué debe entregar | Release |
|---|---|---|---|---|
| Medios de emisión y requisitos de facturación (PA-0093, Gaceta 43.484) | Fiscal/legal | [invoicing.md](../compliance/invoicing.md), [regulatory-sources.md](../compliance/regulatory-sources.md) | Texto oficial, requisitos, formatos, vigencia, anulación | R2 |
| IVA, ISLR, IGTF, retenciones y libros | Contador/fiscalista | [venezuela-tax.md](../compliance/venezuela-tax.md) | Tasas vigentes, bases, retenciones, formatos, plazos | R2/R3 |
| Fórmulas laborales (LOTTT, prestaciones, utilidades, vacaciones, liquidación) | Laboral | [labor-regulations.md](../compliance/labor-regulations.md), [payroll.md](../domain/payroll.md) | Parámetros y casos dorados firmados | R5 |
| Marco contable, plan de cuentas y estados financieros | Contador | [accounting.md](../domain/accounting.md) | Plan base, reglas, presentación, ajuste por inflación | R3 |
| Privacidad, retención y residencia de datos | Legal/privacidad | [security.md](security.md), `DEC-008` | Base legal, plazos, transferencia internacional | R1/R2 |
| Seguridad ofensiva y ASVS | Pentester/seguridad | [security.md](security.md), NFR-SEC | Informe sin críticos/altos; amenazas de tenancy | R2 |
| Certificación del proveedor de emisión | Proveedor/fiscal | [integration-strategy.md](integration-strategy.md), `DEC-002` | Contrato, sandbox, requisitos técnicos | R2 |
| Residencia/contrato cloud | Legal/operaciones | `DEC-001`, [architecture-overview.md](architecture-overview.md) | Región, SLA, subencargados, salida | R1 |
| Precios, disposición a pagar y competencia | Comercial/investigación | [business-model.md](../product/business-model.md), [target-market.md](../product/target-market.md) | Validación de planes y willingness to pay | R2 |

**Nota:** la documentación ya marca estas materias como `PENDIENTE DE VALIDACIÓN LEGAL / CONTABLE`; el riesgo no es que falten, sino que el roadmap no puede cerrar R2/R3/R5 sin ellas. La auditoría no opina sobre el contenido legal.

---

## 8. Roadmap de remediación priorizado

### Fase 0 — Antes de cerrar Sprint 0

1. Aprobar **ADR-007 (ejecución transaccional/tenant)**, **ADR-008 (concurrencia)** y **ADR-009 (contabilización/cutover)**.
2. Corregir `branch_id`/numeración (`AUD-P1-07`) y decidir el ámbito empresa/sucursal.
3. Corregir la dependencia R5→R3 (`AUD-P1-09`) o retirar `NOM-008` de R5.
4. Añadir al DoD las pruebas de concurrencia, rollback cruzado, RLS de pool y tablas `platform`.
5. Ratificar formalmente ADR-001…006 con las correcciones anteriores.

### Fase 1 — Antes de R1 exit

6. Implementar y probar el contexto RLS en lecturas y pool (`AUD-P1-05`).
7. Definir y probar RLS para tablas `platform` o su aislamiento por rol (`AUD-P1-06`).
8. Implementar la coordinación de transacción entre contextos (`AUD-P1-03`).
9. Probar numeración concurrente con y sin sucursal (`AUD-P1-07`).
10. Definir el procedimiento de migración/backfill bajo RLS (`AUD-P2-11`).

### Fase 2 — Antes de producción R2

11. Implementar bloqueo de stock/reservas (`AUD-P1-01`).
12. Implementar bloqueo de aplicación de pagos (`AUD-P1-02`).
13. Implementar bloqueo de crédito (`AUD-P2-12`).
14. Cerrar la semántica de contabilización (`AUD-P1-04`) y añadir la prueba de factura sin asiento.
15. Definir idempotencia en vuelo (`AUD-P2-13`).
16. Definir restauración por tenant (`AUD-P2-07`) o aceptar formalmente el riesgo.
17. Resolver `DEC-002` y validar fiscalmente los documentos.
18. Corregir el catálogo de reportes R2 o retirarlo (`AUD-P2-05`).

### Fase 3 — Antes de R3

19. Diseñar y probar el cutover R2→R3 (`AUD-P1-08`).
20. Definir FX (`AUD-P2-03`).
21. Implementar la garantía de partida doble en BD (`AUD-P2-02`).
22. Diseñar cierre/cutoff concurrente (`AUD-P2-14`).
23. Ratificar el marco contable con especialista.

### Fase 4 — Antes de R4/R5/R6

24. Rediseñar dependencia R5→R3 y provisión (`AUD-P1-09`), si no se hizo antes.
25. Resolver conflictos offline y hardware (R4).
26. Validar fórmulas laborales (R5).
27. Aprobar capacidad/equipo para R2–R3 (`AUD-P2-09`).
28. Definir contratos API/eventos (`AUD-P2-10`), frontend (`AUD-P3-02`), costos/on-call (`AUD-P3-03`).

### Fase 5 — Continuo

29. Auditoría tamper-evident (`AUD-P3-01`) cuando el volumen/regulación lo justifique.
30. Revisión trimestral de RLS, backups y SLOs.

---

## 9. Criterios objetivos para considerar cerrada la auditoría

La auditoría se cierra cuando exista evidencia verificable de:

| # | Criterio | Evidencia | Responsable |
|---|---|---|---|
| 1 | ADR-007, ADR-008 y ADR-009 aprobados | ADR firmados y enlazados | Arquitectura |
| 2 | ADR-001…006 ratificados con correcciones | Acta/estado `Aceptado` | Arquitectura |
| 3 | RLS probada en lecturas, pool y `platform` | Suite de integración con dos tenants y una conexión; introspección 100% | Backend + Seguridad |
| 4 | Transacción cruzada rollback | Prueba de fallo en cada participante; cero filas | Backend |
| 5 | Stock concurrente | 2 ventas de 8 con stock 10: una sola confirma; saldo 2 | Backend + QA |
| 6 | Pago concurrente | 2 cobros de 60 sobre 100: uno confirma; saldo 40 | Backend + QA |
| 7 | Crédito concurrente | 2 pedidos que exceden el límite: no se excede | Backend + QA |
| 8 | Numeración | 10.000 emisiones concurrentes; empresa y sucursal; sin duplicados ni `NULL` ambiguo | Backend |
| 9 | Contabilización crítica | Factura con asiento inválido no se emite; factura válida genera un asiento | Backend + Contabilidad |
| 10 | Cutover R2→R3 | Dataset R2, apertura, balanza cuadrada y CxC/CxP conciliadas | Contabilidad + Backend |
| 11 | Partida doble en BD | Inserción directa desbalanceada rechazada; balanceada aceptada | DBA/Backend |
| 12 | R5 coherente | Roadmap y releases con R5→R3 o `NOM-008` fuera de R5 | Producto |
| 13 | Restauración por tenant | Simulacro con un tenant y los demás intactos | Operaciones |
| 14 | Validación externa | Informes fiscal y laboral con fuente y fecha | Legal/Fiscal/Laboral |
| 15 | Capacidad | Plan de equipo y alcance R2 aprobado | Dirección/Producto |
| 16 | Contratos | OpenAPI/JSON Schema y pruebas de compatibilidad | Arquitectura/Backend |

**No se considera cerrada** por la existencia de documentos, ni por la aprobación de un release, si falta la evidencia de los puntos 3–12.

---

## 10. Balance final

### Lo que está bien y debe preservarse

- La **separación de contextos** y la intención de monolito modular son correctas para el tamaño y el mercado.
- El **modelo de tenancy** (organización→empresa→sucursal, `tenant_id`, FK compuestas, RLS) es sólido y está por encima de la media.
- **ADR-004** (dinero/redondeo) y **ADR-006** (numeración) son decisiones valiosas y bien argumentadas, con correcciones menores.
- La **disciplina de cumplimiento** —fuentes, fecha, jurisdicción, estado, `PENDIENTE DE VALIDACIÓN LEGAL / CONTABLE`— es ejemplar y debe mantenerse.
- El **roadmap sin fechas ficticias** y la trazabilidad RF→AC→release son adecuados para planificación.
- La **estrategia de pruebas** prioriza aislamiento y concurrencia, que es exactamente donde está el riesgo.

### Lo que no debe confundirse con madurez

El volumen documental y la coherencia narrativa no sustituyen la definición de:

- cómo se comparte una transacción entre módulos;
- cómo se bloquea un agregado;
- cómo se establece y resetea el tenant en una conexión pooled;
- qué evento es crítico y cuál es eventual;
- qué pasa cuando R2 se convierte en R3.

Esos cinco puntos son la diferencia entre “documentación completa” y “arquitectura ejecutable”. La auditoría los trata como condiciones, no como detalles de implementación.

### Recomendación final

**Iniciar Sprint 0 con la rebanada vertical, pero no abrir R1 formal ni comprometer R2 hasta cerrar los P1 de ejecución.** El mejor primer entregable técnico no es una pantalla: es una prueba de integración que demuestre aislamiento, rollback cruzado y concurrencia de stock/pago/numeración. Si esa prueba pasa, la arquitectura merece confianza; si no, el equipo habrá descubierto el problema antes de que sea un incidente de producción.

---

## Anexo — Evidencia documental revisada

- [AGENTS.md](../../AGENTS.md), [docs/README.md](../README.md), [GLOSSARY.md](../GLOSSARY.md)
- ADR-001…ADR-006 y [decisiones](../decisions/README.md)
- [functional-requirements.md](../requirements/functional-requirements.md), [non-functional-requirements.md](../requirements/non-functional-requirements.md), [acceptance-criteria.md](../requirements/acceptance-criteria.md), [PRD.md](../requirements/PRD.md)
- [architecture-overview.md](architecture-overview.md), [modular-monolith.md](modular-monolith.md), [multi-tenancy.md](multi-tenancy.md), [database-design.md](database-design.md), [security.md](security.md), [integration-strategy.md](integration-strategy.md), [testing-strategy.md](testing-strategy.md)
- [domain-model.md](../domain/domain-model.md) y documentos de módulo (ventas, compras, inventario, facturación, tesorería, contabilidad, nómina, POS, producción)
- [venezuela-tax.md](../compliance/venezuela-tax.md), [invoicing.md](../compliance/invoicing.md), [labor-regulations.md](../compliance/labor-regulations.md), [regulatory-sources.md](../compliance/regulatory-sources.md)
- [roadmap.md](../product/roadmap.md), [functional-scope.md](../product/functional-scope.md), [epics.md](../../backlog/epics.md), [releases.md](../../backlog/releases.md), [sprint-backlog.md](../../backlog/sprint-backlog.md), [documentation-review.md](../documentation-review.md)

**Limitaciones:** esta auditoría es documental. No se ejecutaron pruebas, no se revisó código, no se verificaron contratos de proveedores ni se emitió opinión legal. Las afirmaciones sobre PostgreSQL se basan en su comportamiento documentado y deben confirmarse con pruebas de integración en la versión objetivo (PostgreSQL 18) antes de cerrar los hallazgos.
