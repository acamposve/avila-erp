# T-034 — API pública, webhooks y ecosistema

**Fase:** R6 · **Depende de:** [T-033](T-033-enterprise-decisions.md); decisiones aplicables aprobadas.

## Trabajo
- Implementar API versionada con cuotas y sandbox, webhooks firmados y reintentos; integrar e-commerce, documental y BI dentro del alcance aprobado.
- Probar compatibilidad de contratos y un flujo de partner usando solo la API.

## Aceptación y evidencia
- OpenAPI y esquemas versionados validados; pruebas de aislamiento, autorización, firma y reintentos; evidencia de integración de partner.
- Trazabilidad: `EP-INT-04`, `EP-INT-05`, `AC-INT-01`…`03`; [integración](../docs/architecture/integration-strategy.md).

## Flujo obligatorio
Crear una rama nueva `feature/t-034-integrations` desde `main`. Al terminar y validar, hacer commit Conventional Commits, push y crear un PR con evidencia; solicitar aprobación a **@acamposve**. No aprobar ni fusionar el propio PR.
