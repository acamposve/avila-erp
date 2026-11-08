# Decisiones de arquitectura y decisiones abiertas

**Versión:** 1.0 · **Fecha:** 2026-11-08

## 1. Architecture Decision Records (ADR)

| ADR | Título | Estado | Impacto |
|---|---|---|---|
| [ADR-001](ADR-001-modular-monolith.md) | Monolito modular como estilo arquitectónico | Aceptado | Estructura de todo el backend |
| [ADR-002](ADR-002-postgresql.md) | PostgreSQL como base de datos | Aceptado | Persistencia, RLS, operación |
| [ADR-003](ADR-003-multi-tenancy.md) | Multi-tenancy compartido con `tenant_id` + RLS | Aceptado | Aislamiento y modelo de datos |
| [ADR-004](ADR-004-money-and-rounding.md) | Dinero, moneda y redondeo | Aceptado | Cálculos fiscales, contables y de inventario |
| [ADR-005](ADR-005-transactional-outbox.md) | Transactional outbox e inbox | Aceptado | Eventos, integraciones y confiabilidad |
| [ADR-006](ADR-006-document-numbering.md) | Numeración documental segura y concurrente | Aceptado | Comprobantes fiscales y documentos internos |
| [ADR-007](ADR-007-transaction-and-tenant-context.md) | Modelo transaccional y contexto de tenant | Aceptado técnicamente (2026-11-08) | UoW, pooling, RLS de negocio y `platform` |
| [ADR-008](ADR-008-concurrency-by-aggregate.md) | Concurrencia por agregado | Aceptado técnicamente (2026-11-08) | Stock, pagos, crédito, numeración, cierre |
| [ADR-009](ADR-009-accounting-and-cutover.md) | Contabilización y cutover R2→R3 | Aceptado técnicamente; validación contable pendiente | Posting, outbox, apertura y conciliación |

Los ADR se aprueban por el responsable de arquitectura. Un agente de IA **no** puede aprobar ni reemplazar un ADR; solo proponerlo.

> **Aprobación de hardening:** las decisiones `D-01`…`D-18` del [plan de hardening](../architecture/architecture-hardening-plan.md) fueron aprobadas técnicamente el 2026-11-08. `D-02`, `D-03` y `D-14` mantienen validación contable externa antes de R3; `D-04` y `D-09` se reflejan en roadmap y numeración. La aprobación no cierra los P1: faltan implementación y pruebas.

## 2. Decisiones abiertas (`DEC`)

Una decisión abierta bloquea el trabajo que depende de ella. Mientras no se resuelva, se implementa solo la estructura extensible, nunca un valor regulatorio o comercial definitivo.

| ID | Decisión | Contexto | Opciones | Recomendación | Dueño | Bloquea | Estado |
|---|---|---|---|---|---|---|---|
| DEC-001 | Proveedor cloud y región primaria | Latencia desde Venezuela, costo, residencia de datos | Azure Brazil South / AWS us-east-1 o sa-east-1 / DigitalOcean Droplet / proveedor local | Región cercana con soporte de PostgreSQL administrado; validar latencia y residencia | Arquitectura + Operaciones | R1 | **Aprobada (DigitalOcean + Terraform); región pendiente** |
| DEC-002 | Medio de emisión fiscal | Nuevos medios digitales y fin de la obligatoriedad de máquina fiscal (ver [SRC-001](../compliance/regulatory-sources.md)) | Proveedor autorizado / imprenta digital / portal SENIAT / máquina fiscal | Integrar por adaptador con un proveedor autorizado; no construir emisión propia | Producto + Legal | R2 | **Pendiente** |
| DEC-003 | Estrategia de identidad | Usuarios multi-organización, MFA, futuro SSO | Identidad propia / proveedor externo (Auth0, Entra) | Identidad propia con estándares abiertos y SSO futuro | Arquitectura + Seguridad | R1 | **Aprobada: identidad propia con estándares abiertos** |
| DEC-004 | Almacenamiento documental | PDFs, adjuntos, evidencias | S3-compatible / disco de VM / base de datos | Objeto S3-compatible con URLs firmadas y cifrado | Arquitectura | R2 | **Pendiente** |
| DEC-005 | Moneda, redondeo y redenominación | Operación multimoneda y posibles cambios de escala | Escala fija / escala por versión de moneda | Escala por versión de moneda (ADR-004); ratificación de negocio | Producto + Contabilidad | R2 | **Resuelta técnicamente**, ratificación pendiente |
| DEC-006 | Planes, precios y empaquetado | Modelo comercial | Por usuario / por plan / híbrido | Validar con pilotos antes de publicar | Dirección + Producto | Lanzamiento R2 | **Pendiente** |
| DEC-007 | Retenciones de IVA e ISLR | Cálculo, comprobantes y libros | Implementar por regla configurable / diferir a R3 | Regla configurable con validación de especialista; R3 para comprobantes | Fiscal + Producto | R2 parcial / R3 | **Pendiente de validación legal** |
| DEC-008 | Retención y residencia de datos | Plazos legales y expectativas del cliente | Retención única / por tipo de dato y jurisdicción | Política por tipo de dato, revisada por legal | Legal + Seguridad | R1/R2 | **Aprobada técnicamente; validación legal pendiente** |
| DEC-009 | Integración bancaria | Conciliación y pagos | API bancaria / archivo / ambos | Archivo en R3; API cuando exista oferta | Producto + Operaciones | R3 | **Pendiente** |
| DEC-010 | Emisión fiscal offline en POS | Legalidad y contingencia | Emisión diferida / contingencia física / bloquear venta | Validar con especialista antes de R4 | Legal + Producto | R4 | **Pendiente de validación legal** |
| DEC-011 | Fórmulas laborales | Prestaciones, utilidades, vacaciones, aportes | Fórmulas internas / motor configurable | Motor configurable con parámetros versionados y validación externa | Laboral + Producto | R5 | **Pendiente de validación legal** |
| DEC-012 | Ajuste por inflación contable | Normativa contable aplicable | Implementar / no implementar / configurable | Configurable y desactivado por defecto hasta validación | Contabilidad | R3 | **Pendiente de validación legal** |
| DEC-013 | Hardware POS y fiscal soportado | Impresoras, gavetas, lectores, máquinas fiscales | Matriz acotada / amplia | Matriz acotada con adaptadores documentados | Producto + Soporte | R4 | **Pendiente** |
| DEC-014 | Motor de cálculo de nómina | Determinismo, versionado, auditoría | Código específico / motor de reglas | Motor determinista con parámetros versionados | Arquitectura + Laboral | R5 | **Pendiente** |
| DEC-015 | BI y reportes avanzados | Consultas pesadas, exportación | Réplica de lectura / almacén de datos / solo exportación | Exportación y réplica de lectura en R6; almacén si el volumen lo exige | Arquitectura + Producto | R6 | **Pendiente** |
| DEC-016 | Licencia y propiedad intelectual | Distribución del producto y del código | Propietario / open core / otro | Definir con dirección legal antes de publicar | Dirección + Legal | Público | **Pendiente** |
| DEC-017 | Resolución de conflictos de sincronización POS | Precio, stock y turno cambian mientras el terminal está offline | Gana el servidor / gana el dispositivo / revisión manual | Revisión manual para stock y precio; catálogo con gana-servidor | Producto + Arquitectura | R4 | **Pendiente** |
| DEC-018 | Recosteo retroactivo de inventario | Compras o correcciones que afectan costos ya vendidos | Sin recosteo / recosteo por período / FIFO | Sin recosteo en R2/R3; evaluar proceso explícito y auditado | Contabilidad + Producto | R3+ | **Pendiente** |
| DEC-019 | Base y momento de liquidación de comisiones | Comisión al facturar o al cobrar | Al facturar / al cobrar / mixto | Validar con ventas; por defecto al cobrar para reducir riesgo | Producto + Comercial | R2 | **Pendiente** |
| DEC-020 | Política de anulación de comprobantes fiscales | Anulación administrativa vs nota de crédito | Solo nota de crédito / anulación validada | Solo reversión documentada hasta validación legal | Legal + Fiscal | R2/R3 | **Pendiente de validación legal** |

### 2.1 Resoluciones del 2026-11-08

| ID | Resolución registrada | Dueño | Evidencia / pendiente |
|---|---|---|---|
| `DEC-001` | **DigitalOcean Droplet**, aprovisionado con **Terraform**. La región primaria queda pendiente; `nyc3` es la candidata inicial por cercanía, sujeta a prueba de latencia. No se creó infraestructura. | @acamposve | Región y prueba de latencia pendientes antes de producción |
| `DEC-003` | **Identidad propia con estándares abiertos**: usuarios, membresías multi-organización, MFA, sesiones y futuro SSO OIDC. Sin proveedor externo en R1. | @acamposve | Implementación en T-006; no se autoaprueba código |
| `DEC-008` | **Retención y residencia por tipo de dato y jurisdicción**. La política técnica queda aprobada; la validación legal sigue pendiente. | @acamposve | Validación legal antes de R2; no se inventan plazos |

Estas resoluciones desbloquean el diseño, pero **no** significan que R1 esté cerrado. `DEC-001` conserva la región abierta y `DEC-008` conserva la validación legal.

**Evidencia de cierre T-002:** [execution-log](../../tasks/evidence/T-002/execution-log.md) · [verification](../../tasks/evidence/T-002/verification.md) · [decision-record](../../tasks/evidence/T-002/decision-record.md).

### 2.2 Responsables y capacidad R1

- **@acamposve** asume la propiedad de las decisiones de Arquitectura, Seguridad, Operaciones y Legal para R1, con validación externa cuando la materia lo exija.
- **Capacidad comprometida:** no cuantificada todavía. No se fijan fechas ni sprints por suposición.
- La aprobación de un PR no sustituye una validación legal, fiscal o contable.

## 3. Reglas para decisiones abiertas

1. Toda decisión nueva se registra con ID secuencial, contexto, opciones, recomendación, dueño y release bloqueado.
2. Una decisión **no se cierra por silencio**: requiere aprobación explícita registrada en este archivo o en un ADR.
3. Si la decisión afecta una materia fiscal o laboral, la aprobación requiere evidencia de un especialista y entrada en [regulatory-sources.md](../compliance/regulatory-sources.md).
4. Las decisiones resueltas se conservan con su resultado y fecha; no se eliminan.
5. El estado `Resuelta técnicamente` no equivale a aprobación de negocio: se indica explícitamente.

## 4. Relación con los requisitos

- `DEC-001`, `DEC-003`, `DEC-008` bloquean R1. `DEC-003` está aprobada; `DEC-001` mantiene la región pendiente; `DEC-008` mantiene la validación legal pendiente.
- `DEC-002`, `DEC-004`, `DEC-005`, `DEC-006`, `DEC-019` bloquean el lanzamiento de R2.
- `DEC-007`, `DEC-009`, `DEC-012`, `DEC-018`, `DEC-020` bloquean partes de R3.
- `DEC-010`, `DEC-013`, `DEC-017` bloquean R4.
- `DEC-011`, `DEC-014` bloquean R5.
- `DEC-015`, `DEC-016` bloquean R6 y la publicación.

Ver el detalle de impacto en [roadmap.md §11](../product/roadmap.md#11-decisiones-que-condicionan-el-roadmap).
