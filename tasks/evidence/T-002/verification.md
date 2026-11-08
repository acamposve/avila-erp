# T-002 — Verification

**Task:** [T-002 — Preparar decisiones bloqueantes de R1](../../T-002-decisions.md)
**Status:** `verified` — approved by @acamposve; PR #5 merged 2026-11-08 (`ab30745`).
**Date:** 2026-11-08

## 1. Acceptance criteria

| Criterio de T-002 | Evidencia | Resultado |
|---|---|---|
| Cada decisión tiene responsable, opciones, impacto y estado verificable en el registro | `DEC-001`, `DEC-003`, `DEC-008` en [`docs/decisions/README.md`](../../../docs/decisions/README.md); dueño @acamposve; impacto R1/R2 | **Pass** |
| Si alguna sigue pendiente, se documenta el bloqueo; no se selecciona una alternativa por silencio | `DEC-001` región y `DEC-008` validación legal están marcadas como pendientes, con dueño y alcance | **Pass** |

T-002 prepara y registra decisiones. No cierra R1, no implementa T-006 y no sustituye la validación externa.

## 2. Decision register

| Decisión | Resolución registrada | Dueño | Impacto | Estado | Evidencia |
|---|---|---|---|---|---|
| `DEC-001` | DigitalOcean Droplet; aprovisionamiento con Terraform. Región primaria pendiente; `nyc3` candidata sujeta a latencia. | @acamposve | R1 | **Aprobada en proveedor/mecanismo; región abierta** | PR #4; registro de decisiones |
| `DEC-003` | Identidad propia con estándares abiertos: membresías multi-organización, MFA, sesiones, futuro OIDC/SSO. Sin IdP externo en R1. | @acamposve | R1 | **Aprobada** | PR #4; registro de decisiones |
| `DEC-008` | Retención y residencia por tipo de dato y jurisdicción. | @acamposve | R1/R2 | **Aprobada técnicamente; validación legal pendiente** | PR #4; registro de decisiones |

## 3. Options and impact

- `DEC-001` considered Azure Brazil South, AWS sa-east-1/us-east-1, DigitalOcean and a local provider. DigitalOcean + Terraform was selected; the region remains an evidence-based decision, not an assumption.
- `DEC-003` considered own identity versus an external IdP (Auth0/Entra). Own identity with open standards was selected to preserve control, data residency and future SSO; the concrete library/stack still requires an ADR before T-006.
- `DEC-008` considered a single retention policy versus per-data-type/jurisdiction. Per-data-type was selected; legal validation remains mandatory before R2.

## 4. Ownership and capacity

| Area | Owner | Status |
|---|---|---|
| Decision ownership R1 | @acamposve | Named |
| Architecture / Security / Operations / Legal decisions | @acamposve | Named as decision owner |
| Capacity | @acamposve | **Not quantified** |

Ownership is resolved. Capacity is not, so no sprint dates or delivery commitments are claimed.

## 5. Open items

| Item | Owner | Impact | Status |
|---|---|---|---|
| `DEC-001` primary region and latency test | @acamposve | R1 infrastructure | Open |
| `DEC-008` legal retention/residency validation | @acamposve + Legal external | R1/R2 | Open |
| Identity stack ADR (library, token format, session model) | @acamposve + Architecture | T-006 | Open |
| R1 capacity commitment | @acamposve | Planning | Open |
| Terraform apply / cloud provisioning | @acamposve | R1 environment | Not authorized; not executed |

## 6. What was not tested

- No infrastructure was provisioned; no Terraform plan/apply was run.
- No identity, MFA or session implementation; that is T-006.
- No legal review of retention periods; no values were invented.
- No load/latency test for DigitalOcean regions.
- No code or migration change, so the 21/21 baseline is unchanged.

## 7. Evidence

- [`../../../docs/decisions/README.md`](../../../docs/decisions/README.md)
- [`../../../docs/project-execution-plan.md`](../../../docs/project-execution-plan.md)
- [`../../T-002-decisions.md`](../../T-002-decisions.md)
- [`../T-001/raw/pr-state.txt`](../T-001/raw/pr-state.txt)
- [`decision-record.md`](decision-record.md)

## 8. Conclusion

T-002 meets its acceptance criteria: every R1 decision has an owner, options, impact and status, and the remaining gaps are explicit. It is **verified**. `DEC-001` region and `DEC-008` legal validation remain the substantive R1 blockers.
