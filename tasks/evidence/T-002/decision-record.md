# T-002 — Decision record

**Date:** 2026-11-08
**Owner:** @acamposve
**Task:** [T-002](../../T-002-decisions.md)
**Status:** implemented; pending review

This record captures the decisions needed by R1. It is not an ADR and does not authorize implementation by itself.

## DEC-001 — Cloud provider and primary region

- **Decision:** DigitalOcean Droplet, provisioned with Terraform.
- **Status:** provider and mechanism approved; primary region open.
- **Candidate region:** `nyc3`, subject to latency measurement from Venezuela.
- **Why:** managed PostgreSQL, predictable cost and proximity; Terraform keeps infrastructure reproducible.
- **Not decided:** final region, droplet size, backup region, SLA and data-residency confirmation.
- **Impact if wrong:** latency, cost, backup/restore and residency.
- **Next evidence:** latency test, Terraform plan, cost estimate.
- **Owner:** @acamposve.

## DEC-003 — Identity strategy

- **Decision:** own identity with open standards.
- **Scope:** users, multi-organization memberships, MFA, sessions, revocation and future OIDC/SSO.
- **Not selected:** Auth0/Entra as the R1 identity provider.
- **Why:** control over data, no vendor lock-in, alignment with the security model and future SSO.
- **Not decided:** concrete library/stack, token format, password policy, MFA factors and session lifetime.
- **Required before T-006:** ADR for the identity stack and a threat model update.
- **Owner:** @acamposve.

## DEC-008 — Retention and residency

- **Decision:** retention and residency by data type and jurisdiction.
- **Status:** technical policy approved; legal validation pending.
- **Why:** different records have different legal retention periods; a single policy would be arbitrary.
- **Not decided:** exact periods, deletion/legal-hold procedure, subprocessors and cross-border transfer basis.
- **Required before R2:** legal review with sources recorded in [regulatory-sources.md](../../../docs/compliance/regulatory-sources.md).
- **Owner:** @acamposve; external legal validation required.

## Ownership and capacity

- @acamposve owns the R1 decisions above.
- Capacity is **not quantified**; no dates or sprint commitments are claimed.
- A PR approval is not a legal, fiscal or accounting validation.

## Evidence

- [Decision register](../../../docs/decisions/README.md)
- [Execution plan](../../../docs/project-execution-plan.md)
- [Execution log](execution-log.md)
- [Verification](verification.md)
- PR #4 (merged): https://github.com/acamposve/avila-erp/pull/4
