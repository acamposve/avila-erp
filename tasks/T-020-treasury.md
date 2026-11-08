# T-020 — Caja, pagos y cartera

**Fase:** R2 · **Depende de:** [T-009](T-009-numbering-idempotency.md), [T-017](T-017-sales.md), [T-019](T-019-billing.md).

## Trabajo
- Implementar apertura/cierre de caja, arqueo, cobros y pagos, anticipos, múltiples medios y aplicaciones parciales.
- Asegurar idempotencia y bloqueo por partida para impedir sobreaplicación concurrente; conservar moneda y trazabilidad.

## Aceptación y evidencia
- PostgreSQL real: dos cobros concurrentes de 60 sobre saldo 100: uno confirma y saldo queda en 40; repetir según plan de hardening.
- Probar factura 1.000, cobros 400 y 300, saldo 300 y efecto de caja correcto. `US-TES-001`…`003`, `AC-TES-01`…`03`; [tesorería](../docs/domain/treasury.md).

## Flujo obligatorio
Crear una rama nueva `feature/t-020-treasury` desde `main`. Al terminar y validar, hacer commit Conventional Commits, push y crear un PR con evidencia; solicitar aprobación a **@acamposve**. No aprobar ni fusionar el propio PR.
