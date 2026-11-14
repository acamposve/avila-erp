# ADR-012 — Sesiones, MFA y recuperación de identidad

- **Estado:** **Aceptado** — 2026-11-14.
- **Decisores:** @acamposve (Arquitectura, Seguridad).
- **Complementa:** ADR-003, DEC-003.
- **Alcance:** identidad propia de R1.

## 1. Contexto

La implementación usa sesiones opacas, mientras que `security.md` describía JWT corto + refresh. Esa divergencia bloquea la revisión de seguridad y la exportación de identidad. Se necesita una decisión única y verificable.

## 2. Decisión

### 2.1 Sesiones opacas en R1

- Token de sesión aleatorio de **256 bits**; solo se persiste su **SHA-256**.
- Expiración absoluta de **8 horas**; expiración por inactividad configurable, por defecto **30 minutos**.
- Revocación inmediata en servidor; cambio de tenant/organización revoca la sesión anterior y emite una nueva.
- No hay refresh token en R1. Una sesión expirada/revocada exige autenticación de nuevo.
- El token viaja solo por `Authorization: Bearer` sobre TLS; no se registra en logs.
- JWT de acceso corto, refresh rotativo, OAuth2/OIDC y SSO quedan para R6 o un ADR posterior; **no se implementan en R1**.

### 2.2 MFA obligatorio

- MFA TOTP es **obligatorio** para `owner`, `admin`, operadores de plataforma y acceso a producción.
- Usuarios sin MFA configurado no reciben sesión con esos roles; reciben un challenge de enrolamiento.
- Códigos de recuperación: aleatorios, hash en reposo, un solo uso, entrega fuera de banda.
- Desactivar/cambiar MFA exige re-autenticación y se audita.

### 2.3 Step-up y cambios sensibles

Operaciones sensibles requieren re-autenticación reciente (máximo 5 minutos):

- cambio de rol/permisos;
- exportación completa del tenant;
- acceso de soporte;
- desactivación de MFA;
- revocación de todas las sesiones.

### 2.4 Recuperación y verificación

- Recuperación de contraseña con token de un solo uso, hash en reposo, expiración corta y respuesta que no revela si el correo existe.
- Verificación de correo obligatoria para aceptar invitaciones y recuperar cuenta.
- Entrega por `IEmailSender`; el adaptador de producción se configura fuera del repositorio; el sink local/Testcontainers no envía correo real.
- Rate limiting por IP, usuario y tenant en login, MFA, recuperación e invitaciones.

### 2.5 Exportación de identidad

La exportación de identidad incluye usuarios/membresías/invitaciones **sin** `password_hash`, `mfa_secret`, códigos de recuperación, tokens de sesión ni hashes de tokens. Es una vista autorizada y auditada, no un dump de tablas.

## 3. Consecuencias

- `security.md` debe describir sesiones opacas, no JWT/refresh.
- La identidad no puede exportarse con el rol `avila_app`; usa el rol `avila_identity` y filtro por membresía.
- El cambio de rol invalida sesiones y exige step-up.
- La recuperación de contraseña y el email quedan como implementación REM-09.

## 4. Referencias

- [Seguridad](../architecture/security.md)
- [ADR-003](ADR-003-multi-tenancy.md)
- [DEC-003](README.md)
