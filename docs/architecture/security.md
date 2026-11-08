# Seguridad — Modelo, controles y verificación

**Versión:** 1.0 · **Fecha:** 2026-11-08 · **Estado:** línea base

## 1. Principios

1. **Denegación por defecto.** Todo recurso requiere autorización explícita.
2. **Mínimo privilegio.** Roles, scopes y accesos temporales acotados.
3. **Defensa en profundidad.** Validación en UI, API, aplicación, base de datos y almacenamiento.
4. **El servidor es la autoridad.** La UI oculta, el servidor decide.
5. **Aislamiento multi-tenant verificable.** Ver [multi-tenancy.md](multi-tenancy.md).
6. **Trazabilidad.** Operaciones sensibles auditadas e inmutables.
7. **Seguridad desde el diseño.** Threat model antes de implementar flujos críticos.

## 2. Modelo de amenazas (STRIDE)

| Amenaza | Escenario | Control |
|---|---|---|
| **S**poofing | Robo de token o credenciales | TLS, tokens cortos, refresh rotativo, MFA, detección de sesión anómala |
| **T**ampering | Modificación de factura o asiento | Inmutabilidad, permisos, auditoría, hashes de integridad |
| **R**epudiation | Usuario niega una operación | Auditoría con actor, fecha, IP, user-agent y correlación |
| **I**nformation disclosure | Fuga entre tenants o PII en logs | RLS, validación de contexto, enmascarado, cifrado |
| **D**enial of service | Abuso de API o job pesado | Rate limiting, cuotas por tenant, timeouts, límites de payload |
| **E**levation of privilege | IDOR o rol mal asignado | Autorización por recurso, scopes, revisión de permisos, pruebas |

Riesgos específicos del dominio:

| Riesgo | Impacto | Mitigación |
|---|---|---|
| Acceso cruzado entre tenants | Crítico | RLS + pruebas + contratos |
| Manipulación de numeración fiscal | Crítico | Secuencia transaccional + auditoría |
| Alteración de documentos contabilizados | Crítico | Inmutabilidad + triggers/permisos |
| Acceso indebido a salarios o datos bancarios | Alto | Cifrado de campo, permisos, enmascarado |
| Fuga de datos por exportación | Alto | Autorización, auditoría, marca de agua |
| Fuerza bruta / credential stuffing | Alto | Rate limit, bloqueo, MFA, detección |
| Dependencia comprometida | Alto | SBOM, escaneo, fijación de versiones |

## 3. Autenticación

- **Contraseñas:** hasher de ASP.NET Core Identity (PBKDF2-HMAC) con iteraciones revisadas; política de longitud mínima 12, sin requisitos de composición arbitrarios, verificación contra listas de contraseñas comprometidas cuando sea viable. Argon2id se evalúa en `DEC-003`.
- **MFA:** TOTP obligatorio para administradores, operadores de plataforma y acceso a producción; opcional para el resto.
- **Sesiones:** access token JWT de ≤ 15 min; refresh token rotativo, revocable y ligado a dispositivo; detección de reutilización.
- **Bloqueo:** backoff progresivo y bloqueo temporal tras intentos fallidos; notificación al usuario.
- **Recuperación:** enlace de un solo uso con expiración corta; nunca revela si el correo existe.
- **Invitaciones:** token firmado, expiración, un solo uso, aceptación con verificación de correo.
- **SSO (R6):** OIDC/SAML por organización, con mapeo de roles y aprovisionamiento SCIM.

## 4. Autorización

- **Modelo:** RBAC con permisos granulares y alcance (tenant, empresa, sucursal, almacén).
- **Permiso:** `<módulo>.<recurso>.<acción>` (por ejemplo `sales.invoice.issue`, `inventory.stock.adjust`).
- **Roles base:** Dueño, Administrador, Contador, Vendedor, Almacenista, Cajero, Auditor, Solo lectura. Los roles son plantillas; los permisos efectivos se calculan por membresía.
- **Denegación por defecto:** si un permiso no está concedido, se rechaza.
- **Alcance:** un permiso sin alcance válido no habilita la acción (por ejemplo, vendedor de la sucursal A no factura en B).
- **Segregación de funciones:** quien aprueba un descuento no debe ser quien lo solicita; quien cierra caja no debe autorizar su propia diferencia.
- **Verificación:** en el endpoint y en el caso de uso; nunca solo en la UI.
- **Cambios de rol:** auditados, con notificación y revalidación de sesiones.

## 5. Protección de datos

### Clasificación

| Nivel | Ejemplos | Controles |
|---|---|---|
| Público | Catálogo público, contenido de ayuda | Integridad |
| Interno | Configuración, reportes agregados | Autorización |
| Confidencial | Facturas, clientes, proveedores, inventario | Autorización + auditoría |
| Restringido | Salarios, datos bancarios, documentos de identidad, salud | Cifrado de campo + acceso acotado + enmascarado |

### Controles

- Cifrado en tránsito (TLS 1.2+, HSTS) y en reposo (AES-256).
- Campos restringidos cifrados a nivel de aplicación cuando el riesgo lo exija (documento de identidad, cuenta bancaria, datos de nómina).
- Enmascarado en UI y logs (`****1234`).
- Retención y borrado según `DEC-008`.
- Exportación autorizada y auditada.
- Sin datos reales en ambientes de desarrollo o pruebas.

### Privacidad

- Inventario de tratamientos y base legal por tipo de dato: **PENDIENTE DE VALIDACIÓN LEGAL** (Venezuela).
- Minimización: solo los datos necesarios.
- Derechos del titular: acceso, rectificación, exportación y supresión cuando aplique; procedimiento por definir con legal.
- Proveedores externos: evaluación de transferencia internacional de datos.

## 6. Seguridad de aplicación

| Control | Implementación |
|---|---|
| Validación de entrada | Esquemas en servidor; límites de tamaño y tipo |
| Inyección SQL | Consultas parametrizadas / EF Core; prohibido concatenar SQL |
| XSS | Escapado por React; CSP estricta; sin `dangerouslySetInnerHTML` sin sanitización |
| CSRF | Tokens en cookies SameSite; API con Bearer no usa cookies de sesión |
| CORS | Lista blanca por ambiente; sin `*` con credenciales |
| Cabeceras | HSTS, CSP, `X-Content-Type-Options`, `Referrer-Policy`, `Permissions-Policy` |
| Rate limiting | Por IP, usuario, tenant y endpoint sensible |
| Archivos | Validación de tipo real, tamaño, antivirus, URL firmada |
| Errores | Problem Details sin trazas internas ni datos sensibles |
| Dependencias | Versiones fijadas, escaneo, SBOM |

## 7. Seguridad de API e integraciones

- Autenticación de usuarios con JWT; integraciones con **OAuth2 client credentials** o API keys con scopes y rotación.
- Cada credencial tiene tenant, scopes, cuota y expiración.
- Idempotencia obligatoria en comandos que crean documentos o pagos.
- Webhooks firmados con HMAC-SHA256, timestamp y tolerancia; reintentos con idempotencia.
- Límites de tasa por credencial y por tenant; 429 con `Retry-After`.
- Registro de llamadas con `correlation_id`, sin secretos ni payload sensible.
- Proveedores externos evaluados por seguridad y contrato; sin acceso a la base de datos.

## 8. Secretos y claves

- Almacén de secretos del proveedor cloud; inyección en runtime.
- Sin secretos en repositorio, imágenes, logs, tests ni documentación.
- Rotación periódica y ante incidente.
- Claves de firma de tokens con `kid` y rotación gradual.
- Cifrado de campos con claves versionadas y separadas por ambiente.
- Acceso a secretos auditado y limitado.

## 9. Auditoría

Se registra en `platform.audit_log` (append-only):

- Autenticación: login, logout, MFA, fallos, bloqueo.
- Autorización: cambios de rol, permisos, membresías.
- Negocio: emisión, anulación, reversión, ajustes, cambios de precio, cierre de período, cambios de tasa.
- Soporte: acceso, lectura, escritura, motivo y ticket.
- Configuración: parámetros fiscales/laborales, feature flags, planes.

Campos mínimos: `audit_id`, `tenant_id`, `actor_id`, `actor_type`, `action`, `entity_type`, `entity_id`, `before`, `after` (enmascarado), `ip`, `user_agent`, `correlation_id`, `occurred_at`, `reason`.

La auditoría no se modifica ni se borra por usuarios; su retención se define con legal.

## 10. Ciclo de vida seguro

| Etapa | Control |
|---|---|
| Requisito | Criterio de seguridad y threat model para flujos críticos |
| Diseño | Revisión de arquitectura y de datos |
| Desarrollo | Revisión de pares, análisis estático, sin secretos |
| CI | SAST, escaneo de dependencias e imagen, pruebas de autorización |
| Prueba | Pruebas de aislamiento, autorización y abuso |
| Despliegue | Aprobación, configuración endurecida, verificación |
| Operación | Monitoreo, alertas, parches, revisión de accesos |
| Incidente | Respuesta, contención, comunicación, postmortem |

**Antes de R2:** prueba de penetración externa y revisión de configuración cloud. **Antes de producción:** checklist ASVS nivel 2.

## 11. Respuesta a incidentes

1. **Detección:** alerta, reporte interno o externo.
2. **Clasificación:** severidad (crítica si hay fuga entre tenants o exposición de datos restringidos).
3. **Contención:** revocar sesiones/credenciales, aislar, preservar evidencia.
4. **Erradicación y recuperación:** corregir, restaurar, verificar.
5. **Notificación:** según obligaciones legales y contractuales (validar con legal).
6. **Postmortem:** sin culpa, con acciones y plazo.

Canal privado de reporte: definido en `CONTRIBUTING.md` (responsable de seguridad; `PENDIENTE` de asignación).

## 12. Verificación

| Control | Evidencia |
|---|---|
| Aislamiento | Batería AC-SEC-01 en CI |
| Autorización | Pruebas por rol y alcance |
| Sesiones/MFA | Pruebas de expiración, revocación y segundo factor |
| Inyección/XSS | SAST + pruebas específicas |
| Secretos | Escaneo en CI |
| Dependencias | SBOM + escaneo |
| Infraestructura | Revisión de configuración y CIS benchmarks |
| Penetración | Informe externo antes de R2 |

## 13. Pendientes

- `DEC-003`: identidad y MFA.
- `DEC-008`: retención y residencia.
- Base legal de tratamiento de datos personales: **PENDIENTE DE VALIDACIÓN LEGAL**.
- Asignación del responsable de seguridad: `PENDIENTE`.

## 14. Documentos relacionados

- [Multi-tenancy](multi-tenancy.md)
- [Arquitectura](architecture-overview.md)
- [Base de datos](database-design.md)
- [Cumplimiento](../compliance/venezuela-tax.md)
- [AGENTS.md](../../AGENTS.md)
