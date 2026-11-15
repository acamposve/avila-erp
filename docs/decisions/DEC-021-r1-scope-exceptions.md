# DEC-021 — Excepciones de alcance R1

**Fecha:** 2026-11-15
**Estado:** Aprobada por @acamposve
**Alcance:** R1 / G1
**Expira:** Production Readiness Gate

## Decisión

Se aceptan las siguientes excepciones para el cierre de R1. Ninguna autoriza R2, despliegue ni migraciones compartidas.

| Excepción | Motivo | Condición de cierre |
|---|---|---|
| AC-ADM-03 literal: ajuste de stock denegado | Inventory es R2; R1 no tiene stock-adjust | Gate del módulo Inventory (R2) |
| US-PLT-003 planes comerciales/metering | Evitar backoffice SaaS antes del core ERP | Production Readiness Gate |
| US-PLT-004 consola de operador | Idem | Production Readiness Gate |
| AC-PLT-02 auto-onboarding notification | Idem | Production Readiness Gate |
| OTLP collector, Alertmanager receiver, Terraform apply, PITR/WAL, WORM anchor | Pre-cloud placeholders | Production Readiness Gate |

## Patrón de seguridad que sí se cierra en R1

AC-ADM-03 se cierra **funcionalmente** mediante mutaciones de company/branch denegadas:

- `403 Forbidden` server-side;
- cero mutación de estado;
- evento de auditoría de intento denegado persistido.

El literal “stock adjust” queda diferido a Inventory/R2.

## Consecuencias

- G1 puede evaluarse sin abrir R2.
- Las excepciones son nombradas, con dueño y expiración; no son aceptación genérica.
- Cualquier nueva excepción requiere decisión explícita de @acamposve.
- La aprobación de un PR no abre G1 por sí sola.

## Evidencia

- [Matriz R1-REM-14](../../tasks/evidence/R1-REM-14/r1-gate-matrix.md)
- [Caveats operativos](../../tasks/evidence/R1-REM-14/operational-caveats.md)
- [R1-CLOSURE-A](../../tasks/evidence/R1-CLOSURE-A/verification.md)
