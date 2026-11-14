# R1 decisions, risks and findings — T-013

**Base:** `cae8bbd` · **Date:** 2026-11-14

## 1. R1 decisions

| ID | Decision | State | R1 impact | Owner |
|---|---|---|---|---|
| DEC-001 | DigitalOcean + Terraform | **Approved provider; region pending** (`nyc3` candidate) | Blocks production location/latency/residency | @acamposve |
| DEC-003 | Own identity with open standards | **Approved** | Implementation exists; no identity-stack ADR for opaque sessions vs JWT/refresh | @acamposve |
| DEC-008 | Retention/residency by data type/jurisdiction | **Technically approved; legal validation pending** | Blocks production retention/backup policy | @acamposve + Legal |

Supporting evidence: [DEC index](../../../docs/decisions/README.md), [T-002 decision record](../T-002/decision-record.md).

## 2. Foundational ADRs

| ADR | State | R1 note |
|---|---|---|
| ADR-001 modular monolith | Accepted | Enforced by module-boundary tests |
| ADR-002 PostgreSQL | Accepted | PostgreSQL 18 Testcontainers |
| ADR-003 multi-tenancy | Accepted | RLS/UoW implemented |
| ADR-004 money/rounding | Accepted | R1 slice uses decimal; no fiscal calc |
| ADR-005 outbox | Accepted | Not implemented; no jobs in R1 |
| ADR-006 numbering | Accepted | Internal numbering implemented; fiscal pending |
| ADR-007 transaction/tenant context | Accepted technically | UoW/RLS tested |
| ADR-008 concurrency by aggregate | Accepted technically | Numbering/approval tests; broader concurrency pending |
| ADR-009 accounting/cutover | Accepted technically; external validation pending | Not exercised in R1 |

**Gap:** no accepted ADR for the final identity/session stack; `security.md` still references JWT/refresh while the implementation is opaque-session.

## 3. R1 risks

| Risk | Rating | R1 residual | Mitigation/evidence |
|---|---|---|---|
| RSK-006 Cross-tenant data leak | Low probability / Critical impact | **Open residual** | RLS, roles, module tests; no jobs/outbox; complete per-resource matrix absent |
| RSK-008 Key-person dependency | Medium / High | **Open** | Documentation/evidence increased; capacity still unquantified; single decision owner |
| RSK-015 Cloud latency/cost from Venezuela | Medium / Medium | **Open** | Provider chosen; region/latency test not run |
| RSK-018 Technical debt from delivery pressure | Medium / High | **Open** | 7 partial tasks, no P1 closed, many service-only capabilities |

Risk register: [roadmap §10](../../../docs/product/roadmap.md).

## 4. Findings and P1 status

- **0 P1 closed.** T-005/T-008/T-009/T-011/T-012 implement mechanisms but do not close hardening findings.
- R1-specific gaps:
  - no complete tenant export;
  - no audit query/export;
  - no HTTP authorization/error contract;
  - no alerts/dashboards/distributed tracing;
  - no CI security scan;
  - no authorized production restore;
  - identity ADR and MFA enforcement pending;
  - `DEC-001` region and `DEC-008` legal validation pending.
- F-08 (gate evidence matrices) is addressed for **T-013** by this package; other gate tasks remain.

Hardening references:

- [Architecture hardening plan](../../../docs/architecture/architecture-hardening-plan.md)
- [Hardening refinement](../../../docs/architecture/hardening-refinement.md)
- [Tasks validation](../../../docs/tasks-validation.md)

## 5. Gate conclusion

The risk posture and unresolved decisions support **NO-GO**. R1 can be reconsidered only after the FAIL is removed and each PARTIAL is either closed or explicitly accepted by @acamposve with owner and expiry.
