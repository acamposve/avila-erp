# T-028 — POS, sincronización y hardware

**Fase:** R4 · **Depende de:** [T-027](T-027-retail-decisions.md); decisiones pertinentes aprobadas.

## Trabajo
- Implementar venta, turnos y cierre Z, persistencia offline protegida, sincronización idempotente y resolución de conflictos aprobada.
- Integrar facturación POS y hardware de la matriz aprobada; completar seriales/códigos de barras.

## Aceptación y evidencia
- Probar desconexión, reinicio y reintentos sin pérdida ni duplicados, cierre conciliado y fiscalidad según política validada; probar hardware autorizado.
- Trazabilidad: `EP-POS-01`, `EP-POS-02`, `EP-INV-05`, `AC-POS-01`…`04`; [POS](../docs/domain/point-of-sale.md).

## Flujo obligatorio
Crear una rama nueva `feature/t-028-retail-delivery` desde `main`. Al terminar y validar, hacer commit Conventional Commits, push y crear un PR con evidencia; solicitar aprobación a **@acamposve**. No aprobar ni fusionar el propio PR.
