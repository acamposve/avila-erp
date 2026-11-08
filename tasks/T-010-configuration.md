# T-010 — Parámetros versionados y aprobaciones

**Fase:** R1 · **Depende de:** [T-008](T-008-authorization-audit.md).

## Trabajo
- Implementar parámetros con vigencia, fuente e historial inmutable, y preferencias de organización.
- Implementar flujos de aprobación por tipo y monto, autorizados y auditados.
- Mantener cualquier valor fiscal o laboral no validado como `PENDIENTE DE VALIDACIÓN LEGAL / CONTABLE`.

## Aceptación y evidencia
- Probar selección por vigencia, preservación del histórico, auditoría y rechazo de operaciones que requieren aprobación ausente.
- Trazabilidad: `US-ADM-009`, `US-ADM-012`, `AC-FIS-01`, `AC-VEN-02`; [fuentes regulatorias](../docs/compliance/regulatory-sources.md).

## Flujo obligatorio
Crear una rama nueva `feature/t-010-configuration` desde `main`. Al terminar y validar, hacer commit Conventional Commits, push y crear un PR con evidencia; solicitar aprobación a **@acamposve**. No aprobar ni fusionar el propio PR.
