# T-009 — Code map

## 1. Numbering

| Artefacto | Ruta | Propósito |
|---|---|---|
| Number service | `src/Avila.Platform/Numbering/DocumentNumberService.cs` | Row-locked sequence, prefix + 8 digits |
| Sequence table | `platform.document_sequences` | Tenant/scope/type unique sequence |
| Sales integration | `src/Modules/Sales/Avila.Modules.Sales/SalesModule.cs` | Quote/order numbers |

## 2. Idempotency

| Artefacto | Ruta | Propósito |
|---|---|---|
| Idempotency service | `src/Avila.Platform/Idempotency/IdempotencyService.cs` | Insert/replay/conflict/complete |
| Idempotency table | `platform.idempotency_keys` | Unique tenant + operation + key |
| Sales flow | `src/Avila.Application/SalesFlowService.cs` | Optional Idempotency-Key |
| Migration | `src/Avila.Platform/Migrations/20261114200618_AddDocumentNumberingAndIdempotency.cs` | Tables + RLS |

## 3. Tests

| Test | File |
|---|---|
| Scope, rollback, 10k, replay, conflict, quote/order | `tests/Avila.ProductionTests/NumberingIdempotencyTests.cs` |
| Fixture grants/RLS | `tests/Avila.ProductionTests/ProductionFixture.cs` |

## 4. Documentation

| Documento | Change |
|---|---|
| `docs/architecture/architecture-hardening-plan.md` | P1-07 production progress |
| `docs/architecture/hardening-refinement.md` | T-009 production evidence |
| `docs/tasks-validation.md` | T-008 verified, T-009 implemented |

## 5. Boundaries

- No fiscal numbering/series administration.
- No HTTP Idempotency-Key mapping.
- No idempotency for other commands.
- No cleanup/retention policy.
- No shared migration or deployment.
