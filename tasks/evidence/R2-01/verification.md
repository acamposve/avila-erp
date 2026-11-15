# R2-01 — Foundations

- **Base:** R2-00 branch
- **Status:** implemented; PR stacked on R2-00
- **Tests:** full suite **119/119 PASS**; R2FoundationTests 2/2

## Delivered

- `Money`, `Quantity`, `Rate`, `CurrencyPair` in SharedKernel.
- Half-away-from-zero `Money.Round`.
- `CurrencyPair` document/settlement rates + realized difference.
- Transactional outbox table/service (`platform.outbox_events`), forced RLS/registry/grant.
- Outbox test proves tenant-scoped transactional enqueue.

## Evidence

- [Financial primitives](../../../src/Avila.SharedKernel/FinancialPrimitives.cs)
- [Outbox writer](../../../src/Avila.Platform/Outbox/OutboxWriter.cs)
- [Migration](../../../src/Avila.Platform/Migrations/20261115121900_AddOutboxEvents.cs)
- [Tests](../../../tests/Avila.ProductionTests/R2FoundationTests.cs)
