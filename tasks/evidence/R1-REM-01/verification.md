# R1-REM-01 — Verification

**Task:** R1-REM-01 — ADR bundle
**Status:** `implemented` — awaiting human review.
**Date:** 2026-11-14

## 1. Acceptance criteria

| Criterio | Evidencia | Resultado |
|---|---|---|
| ADR-010 define precisión, redondeo y multimoneda | `numeric(19,4)` importes, `19,6` precios/cantidades, `19,8` tasas, `9,6` porcentajes; `AwayFromZero`; moneda base/transaccional/referencia | **Pass** |
| ADR-010 no contradice ADR-004 | Referencia explícita; mismos tipos/redondeo; banker's rechazado | **Pass** |
| ADR-011 define auditoría inmutable y retención | `platform.audit_events`, trigger/grants, hash chain, consulta/exportación, 7 años | **Pass** |
| ADR-012 resuelve sesiones/MFA | Sesiones opacas, MFA obligatorio owner/admin, step-up, recovery | **Pass** |
| ADR-013 resuelve topología/telemetría | DigitalOcean `nyc3` single-region, read-replica readiness, OTel/Prometheus | **Pass** |
| `DEC-001`/`DEC-008` desbloqueados | Baseline en ADR-011/013 y decisiones | **Pass** |
| Sin contradicciones documentales | `security.md`, roadmap, PRD, NFR, índice DEC actualizados; 0 enlaces roto | **Pass** |
| Sin código/despliegue | Diff solo docs | **Pass** |

## 2. Decisions

| ADR | Decision |
|---|---|
| ADR-010 | Amounts `numeric(19,4)`; half-up away from zero; base/transaction/reference currency; rate snapshot |
| ADR-011 | Append-only audit, per-tenant hash chain, query/export, 7-year default, legal hold |
| ADR-012 | Opaque sessions (256-bit, SHA-256 at rest, 8 h + 30 min idle), mandatory MFA, no JWT/refresh in R1 |
| ADR-013 | DigitalOcean/Terraform, `nyc3` single-region, read-replica readiness, OTel/Prometheus/Alertmanager |

## 3. Checks

| Check | Result |
|---|---|
| Markdown links | 0 broken / 141 files |
| ADR count | ADR-001…ADR-013 |
| Code build/tests | Not run; docs-only branch. Main baseline 72/72. |
| G1 status | **Closed** |

## 4. Open items

- @acamposve approval of ADR-010…013.
- Implementation of hash chain, audit query/export, MFA/recovery, OTel/alerts in later REM PRs.
- Identity export must exclude hashes/secrets/tokens.

## 5. Evidence

- [ADR-010](../../../docs/decisions/ADR-010-monetary-precision.md)
- [ADR-011](../../../docs/decisions/ADR-011-immutable-audit.md)
- [ADR-012](../../../docs/decisions/ADR-012-identity-sessions.md)
- [ADR-013](../../../docs/decisions/ADR-013-topology-operations.md)
- [Decision index](../../../docs/decisions/README.md)
- [Security](../../../docs/architecture/security.md)
- [execution-log.md](execution-log.md)

## 6. Conclusion

REM-01 meets its acceptance criteria as a documentation decision bundle. It is ready for human review; it does not change code, close R1, or open G1.
