# Requisitos no funcionales — ÁVILA ERP

**Versión:** 1.0 · **Fecha:** 2026-11-08 · **Estado:** línea base

Los requisitos de este documento son **verificables**. Cada uno define un objetivo medible y un método de verificación. Los valores son metas de ingeniería sujetas a validación con carga real; los cambios se registran en este documento y en las pruebas.

## 1. Perfil de carga de referencia

**CO-1 (Carga Objetivo 1)** — hipótesis de planificación para R2:

| Dimensión | Valor de referencia |
|---|---|
| Organizaciones activas | 1.000 |
| Usuarios registrados | 10.000 |
| Sesiones concurrentes | 500 |
| Documentos fiscales por año | 5.000.000 |
| Movimientos de inventario por año | 20.000.000 |
| Asientos contables por año | 10.000.000 |
| Tamaño de tenant típico | 1 empresa, 1–3 sucursales, 5–20 usuarios |
| Tenant máximo esperado | 10 empresas, 20 sucursales, 200 usuarios |

La arquitectura DEBE soportar **3× CO-1** con escalamiento horizontal y vertical documentado, sin cambio de arquitectura.

## 2. Seguridad (`NFR-SEC`)

| ID | Requisito verificable | Objetivo | Verificación | Rel. |
|---|---|---|---|---|
| NFR-SEC-001 | Autenticación conforme a OWASP ASVS nivel 2 | 100 % de controles L2 aplicables | Auditoría de seguridad + checklist ASVS | R1 |
| NFR-SEC-002 | Contraseñas almacenadas con función de derivación lenta y salt único | Sin contraseñas reversibles | Revisión de código + prueba | R1 |
| NFR-SEC-003 | MFA disponible para todos los usuarios y obligatoria para administradores y operadores | 100 % de roles privilegiados | Prueba de configuración y login | R1 |
| NFR-SEC-004 | Sesiones con tokens de acceso de vida corta y refresh rotativo revocable | ≤ 15 min access token | Prueba de expiración y revocación | R1 |
| NFR-SEC-005 | Cifrado en tránsito con TLS 1.2+ (preferido 1.3) y HSTS | 0 endpoints sin TLS | Escaneo + configuración | R1 |
| NFR-SEC-006 | Cifrado en reposo de base de datos, respaldos y almacenamiento de archivos | AES-256 o equivalente del proveedor | Revisión de infraestructura | R1 |
| NFR-SEC-007 | Sin secretos en repositorio; gestión en almacén de secretos con rotación | 0 secretos detectados | Escaneo en CI | R1 |
| NFR-SEC-008 | Límite de tasa por identidad, tenant y endpoint sensible | 429 ante abuso; sin degradación de otros tenants | Prueba de carga y abuso | R2 |
| NFR-SEC-009 | Remedición de vulnerabilidades: crítica ≤ 7 días, alta ≤ 30 días | 0 críticas abiertas al lanzamiento | Registro de vulnerabilidades | R1+ |
| NFR-SEC-010 | Prueba de penetración externa antes del lanzamiento de R2 | Informe sin hallazgos críticos/altos abiertos | Informe del proveedor | R2 |

## 3. Aislamiento multi-tenant (`NFR-ISO`)

| ID | Requisito verificable | Objetivo | Verificación | Rel. |
|---|---|---|---|---|
| NFR-ISO-001 | Ninguna consulta, comando, reporte, exportación o job puede devolver datos de otro tenant | 0 cruces en batería de pruebas | AC-SEC-01 | R1+ |
| NFR-ISO-002 | RLS activa en todas las tablas de negocio, con rol de aplicación sin `BYPASSRLS` | 100 % de tablas de negocio | Introspección de esquema en CI | R1 |
| NFR-ISO-003 | Toda tabla de negocio tiene `tenant_id NOT NULL` e índice que lo incluye | 100 % | Prueba de esquema automatizada | R1 |
| NFR-ISO-004 | El contexto de tenant se revalida en servidor contra la membresía en cada petición | 100 % de peticiones autenticadas | Prueba de token manipulado | R1 |
| NFR-ISO-005 | Los jobs y eventos conservan el tenant explícito y no comparten estado | 0 jobs sin tenant cuando aplica | Prueba de ejecución concurrente | R1+ |

## 4. Rendimiento (`NFR-PER`)

Medido en servidor, con CO-1, sin caché artificial, con base de datos representativa (≥ 1 M documentos).

| ID | Requisito verificable | Objetivo | Verificación | Rel. |
|---|---|---|---|---|
| NFR-PER-001 | Latencia de lecturas simples de API | p95 ≤ 300 ms, p99 ≤ 800 ms | AC-PERF-01 (k6) | R2 |
| NFR-PER-002 | Latencia de comandos de escritura (sin generación de PDF) | p95 ≤ 500 ms, p99 ≤ 1.200 ms | Prueba de carga | R2 |
| NFR-PER-003 | Emisión de factura completa (impuestos, numeración, snapshot, PDF) | p95 ≤ 1.500 ms | AC-PERF-01, AC-PERF-02 | R2 |
| NFR-PER-004 | Búsqueda de artículos con 50.000 ítems | p95 ≤ 400 ms | Prueba con dataset | R2 |
| NFR-PER-005 | Consulta de kardex de 12 meses paginado | p95 ≤ 2 s | Prueba con dataset | R2 |
| NFR-PER-006 | Frontend: LCP en 4G/equipo medio | p75 ≤ 2,5 s; INP ≤ 200 ms; CLS ≤ 0,1 | Lighthouse + medición real (RUM) | R2 |
| NFR-PER-007 | Venta POS local (sin red) | ≤ 200 ms por ítem y ≤ 500 ms por cobro | Prueba en dispositivo de referencia | R4 |
| NFR-PER-008 | Importación de conciliación bancaria de 100.000 líneas | ≤ 5 min, en segundo plano, con progreso | Prueba de proceso | R3 |
| NFR-PER-009 | Reportes pesados se ejecutan en segundo plano cuando superan 5 s | 100 % de reportes > 5 s asíncronos | Prueba funcional | R3 |

## 5. Disponibilidad y recuperación (`NFR-AVA`)

| ID | Requisito verificable | Objetivo | Verificación | Rel. |
|---|---|---|---|---|
| NFR-AVA-001 | Disponibilidad mensual del servicio | ≥ 99,9 % (excluye mantenimiento anunciado) | Monitoreo sintético + SLO | R2 |
| NFR-AVA-002 | RPO de datos transaccionales | ≤ 15 min | Prueba de restauración a punto en el tiempo | R1 |
| NFR-AVA-003 | RTO del servicio | ≤ 4 h | Simulacro de recuperación | R1 |
| NFR-AVA-004 | Respaldos: PITR 35 días, snapshots diarios 30 días, mensuales 12 meses | 100 % de ventanas cubiertas | Verificación de política y restauración | R1 |
| NFR-AVA-005 | Despliegue sin interrupción perceptible | 0 downtime planificado en horario hábil | Prueba de despliegue azul/verde o rolling | R2 |
| NFR-AVA-006 | Aislamiento de fallos: un job o tenant pesado no degrada a los demás | p95 de otros tenants dentro de NFR-PER | Prueba de contención y cuotas | R2 |
| NFR-AVA-007 | Simulacro de recuperación ante desastre | Al menos 1 por trimestre, con informe | Acta y evidencias | R1+ |

## 6. Escalabilidad (`NFR-SCA`)

| ID | Requisito verificable | Objetivo | Verificación | Rel. |
|---|---|---|---|---|
| NFR-SCA-001 | API sin estado, escalable horizontalmente | 3× CO-1 con ≤ 8 instancias | Prueba de carga y escalado | R2 |
| NFR-SCA-002 | Base de datos con estrategia de crecimiento documentada (índices, particionado, réplica de lectura) | Consultas dentro de NFR-PER a 3× CO-1 | Prueba de volumen + revisión | R2+ |
| NFR-SCA-003 | Procesamiento por tenant con cuotas y aislamiento de colas | 0 tenants bloquean la cola global | Prueba de contención | R2 |
| NFR-SCA-004 | Almacenamiento de documentos con ciclo de vida y límites por plan | Crecimiento controlado y medido | Medición de consumo | R2 |

## 7. Observabilidad (`NFR-OBS`)

| ID | Requisito verificable | Objetivo | Verificación | Rel. |
|---|---|---|---|---|
| NFR-OBS-001 | Trazas distribuidas con OpenTelemetry en API, jobs, base de datos y llamadas externas | ≥ 95 % de operaciones instrumentadas | AC-OBS-01 | R1 |
| NFR-OBS-002 | Correlación entre petición, evento, job y asiento mediante `correlation_id` | 100 % de flujos críticos | Prueba de rastreo end-to-end | R1 |
| NFR-OBS-003 | Logs estructurados sin PII, con nivel y contexto de tenant/usuario | 0 PII en muestreo | Revisión automatizada de logs | R1 |
| NFR-OBS-004 | Métricas RED/USE y dashboards por módulo y tenant (agregado) | Cobertura de todos los módulos activos | Revisión de dashboards | R2 |
| NFR-OBS-005 | Alertas con umbrales y guardias documentadas | 100 % de SLO críticos con alerta | Prueba de alerta | R2 |
| NFR-OBS-006 | Auditoría separada de logs operativos, con retención definida | Consultable y exportable | Prueba de consulta y exportación | R1 |

## 8. Integridad y calidad de datos (`NFR-DAT`)

| ID | Requisito verificable | Objetivo | Verificación | Rel. |
|---|---|---|---|---|
| NFR-DAT-001 | Importes monetarios con `numeric`/`decimal`, nunca punto flotante | 0 columnas `float` para dinero | Introspección de esquema | R1 |
| NFR-DAT-002 | Redondeo determinista y reproducible | 100 % de casos dorados | Pruebas de propiedades y casos | R2 |
| NFR-DAT-003 | Integridad referencial con claves compuestas por tenant | 0 registros huérfanos | Prueba de esquema y datos | R1 |
| NFR-DAT-004 | Numeración fiscal sin duplicados bajo concurrencia | 0 duplicados en 10.000 emisiones concurrentes | AC-PERF-02 | R2 |
| NFR-DAT-005 | Idempotencia de comandos críticos ante reintentos | 1 efecto por clave | Prueba de repetición y fallo de red | R2 |
| NFR-DAT-006 | Conciliación diaria automática entre subledgers y contabilidad | Diferencias = 0 o alerta | Job de conciliación + reporte | R3 |
| NFR-DAT-007 | Migración de datos sin pérdida ni duplicados | 100 % de registros reconciliados | Prueba de migración con dataset | R2 |
| NFR-DAT-008 | Fechas de negocio en `America/Caracas` y marcas de tiempo en UTC | 0 ambigüedades en documentos | Pruebas de borde (medianoche, DST no aplica) | R2 |

## 9. Usabilidad y accesibilidad (`NFR-USA`)

| ID | Requisito verificable | Objetivo | Verificación | Rel. |
|---|---|---|---|---|
| NFR-USA-001 | Interfaz en español (es-VE), con i18n preparada para otros idiomas | 100 % de cadenas traducibles | Revisión + prueba de cambio de idioma | R2 |
| NFR-USA-002 | Accesibilidad WCAG 2.2 nivel AA en flujos principales | 0 fallos críticos | Auditoría axe + prueba manual con teclado/lector | R2 |
| NFR-USA-003 | Diseño responsive desde 360 px hasta escritorio | Flujos completos sin scroll horizontal | Prueba en dispositivos | R2 |
| NFR-USA-004 | Mensajes de error accionables con código y guía de resolución | 100 % de errores de negocio mapeados | Revisión de catálogo de errores | R2 |
| NFR-USA-005 | Un usuario capacitado emite su primera factura en ≤ 30 min | ≥ 90 % de usuarios en prueba | Prueba de usabilidad | R2 |
| NFR-USA-006 | Operación por teclado en POS y pantallas de alta frecuencia | Flujo de venta completo sin mouse | Prueba de usabilidad | R4 |
| NFR-USA-007 | Prevención de pérdida de datos en formularios largos | 0 pérdidas en pruebas de interrupción | Prueba de recuperación de borrador | R2 |

## 10. Mantenibilidad (`NFR-MAI`)

| ID | Requisito verificable | Objetivo | Verificación | Rel. |
|---|---|---|---|---|
| NFR-MAI-001 | Límites de módulo verificados automáticamente | 0 dependencias prohibidas | Prueba de arquitectura en CI | R1 |
| NFR-MAI-002 | Cobertura de reglas e invariantes de dominio | ≥ 90 % de reglas con prueba | Matriz de trazabilidad RF → prueba | R2 |
| NFR-MAI-003 | Build y suite rápida | Build ≤ 10 min; unitarias ≤ 10 min | Medición de CI | R1 |
| NFR-MAI-004 | Migraciones reproducibles desde base vacía | 100 % en CI | Job de migración en limpio | R1 |
| NFR-MAI-005 | Documentación actualizada en el mismo PR que el cambio | 100 % de PRs con impacto documental | Revisión de PR | R1+ |
| NFR-MAI-006 | Dependencias con licencia compatible y mantenimiento activo | 0 dependencias sin revisión | Revisión de dependencias | R1+ |

## 11. Cumplimiento (`NFR-CMP`)

| ID | Requisito verificable | Objetivo | Verificación | Rel. |
|---|---|---|---|---|
| NFR-CMP-001 | Toda regla fiscal o laboral tiene fuente, fecha, jurisdicción y estado | 100 % de reglas | Revisión de [regulatory-sources.md](../compliance/regulatory-sources.md) | R2+ |
| NFR-CMP-002 | Documentos fiscales y asientos conservan inmutabilidad y trazabilidad | 0 modificaciones posteriores | Pruebas negativas + auditoría | R2 |
| NFR-CMP-003 | Retención y eliminación de datos conforme a la validación legal | Política aprobada y aplicada | Revisión de política (`DEC-008`) | R2 |
| NFR-CMP-004 | Exportación completa de datos del tenant en formato abierto | 100 % de entidades exportables | Prueba de exportación y restauración | R1 |
| NFR-CMP-005 | Registro de consentimiento y tratamiento de datos personales | 100 % de tratamientos inventariados | Revisión de privacidad | R2 |
| NFR-CMP-006 | Cambios normativos aplicables sin despliegue de código cuando corresponda | ≥ 80 % de parámetros actualizables por configuración | Prueba de cambio de parámetro | R2 |

## 12. Criterios de verificación

Cada NFR se considera verificado cuando:

1. Existe una prueba automatizada, un informe o una evidencia reproducible.
2. La evidencia está enlazada desde el PR o el release.
3. El resultado cumple el objetivo numérico o, si no se cumple, existe una decisión registrada con mitigación.

Los criterios de aceptación funcionales están en [acceptance-criteria.md](acceptance-criteria.md). La estrategia de prueba está en [testing-strategy.md](../architecture/testing-strategy.md).

## 13. Documentos relacionados

- [PRD](PRD.md)
- [Arquitectura](../architecture/architecture-overview.md)
- [Seguridad](../architecture/security.md)
- [Base de datos](../architecture/database-design.md)
- [Estrategia de pruebas](../architecture/testing-strategy.md)
