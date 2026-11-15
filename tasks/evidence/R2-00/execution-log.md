# R2-00 — Governance and plan

- **Date:** 2026-11-15
- **Branch:** `docs/r2-governance-plan`
- **Base:** `b0925cc`
- **Status:** implemented; G1 OPEN; no R2 code

## Delivered

- G1 OPEN recorded in `backlog/releases.md`.
- R2 ADR baseline frozen (ADR-001…013).
- Module boundaries and outbox event catalogue.
- Two approved guardrails:
  - realized FX captures document + settlement rates and emits `fx.realized`;
  - sales order reservations vs delivery physical deduction/COGS, no double deduction.
- Milestones/PR sequence R2-00…R2-13.

## Evidence

- [R2 plan](../../../docs/architecture/r2-execution-plan.md)
- [backlog/releases.md](../../../backlog/releases.md)

## Boundaries

- No implementation, no migration, no deployment.
- R2-01/02 are separate atomic PRs.
