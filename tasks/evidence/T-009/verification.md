# T-009 — Verification

**Task:** [T-009 — Numeración e idempotencia transaccional](../../T-009-numbering-idempotency.md)
**Status:** `verified` — approved by @acamposve; PR #12 merged 2026-11-14 (`14ee40b`).
**Date:** 2026-11-14

## 1. Acceptance criteria

| Criterio de T-009 | Evidencia | Resultado |
|---|---|---|
| 10.000 emisiones concurrentes sin duplicados ni huecos | 10k test, 64-way concurrency; 10,000 distinct from 1 to 10,000 | **Pass** |
| Ámbitos empresa/sucursal aislados | Company A 1–3, company B starts at 1; branch sequence independent | **Pass** |
| Rollback correcto | Reserve 2, rollback, next reserve returns 2 | **Pass** |
| Misma clave = un solo efecto | Sequential/concurrent replay tests; one company/quote | **Pass** |
| Clave distinta con cuerpo distinto = 409/conflict | `IDEMPOTENCY_KEY_REUSED` | **Pass** |
| Cotización/pedido numerados; sin GUID | `Q-00000001`, `O-00000001` | **Pass** |

## 2. Test coverage

| Test | What it proves |
|---|---|
| `Numbers_are_gapless_and_isolated_by_company_and_branch` | Scope isolation and increments |
| `Rollback_does_not_consume_a_number` | Transactional sequence |
| `Ten_thousand_concurrent_numbers_are_unique_and_gapless` | 10k unique/gapless |
| `Same_idempotency_key_replays_without_duplicating_rows` | Replay, one effect |
| `Same_key_with_different_request_is_rejected` | Request hash conflict |
| `Concurrent_same_key_executes_one_effect` | Concurrent duplicate |
| `Quote_and_order_numbers_replace_guid_placeholders` | Production integration |

Production suite: **42/42**. Full suite: **61/61**.

## 3. Numbering

| Property | Implementation |
|---|---|
| Table | `platform.document_sequences` |
| Key | tenant + `scope_type` + `scope_id` + `document_type` |
| Scope | `company` or `branch` |
| Lock | `SELECT ... FOR UPDATE` on the sequence row |
| Number | prefix + 8-digit value |
| Rollback | Increment is in the caller transaction; rollback does not consume |
| Fiscal | **Not** fiscal numbering; quote/order only |

## 4. Idempotency

| Property | Implementation |
|---|---|
| Table | `platform.idempotency_keys` |
| Unique | tenant + operation + key |
| Request hash | SHA-256 of normalized request |
| In-flight | same transaction insert; concurrent duplicate waits |
| Completed | stored JSON replay |
| Different body | `IDEMPOTENCY_KEY_REUSED` conflict |
| Rollback | key row and business writes roll back together |

## 5. Open gaps

- No fiscal invoice numbering or administrable series.
- 10k test uses 64 workers, not 10k physical connections; no pool tuning/load report.
- Idempotency is integrated only in `sales-flow`; other critical commands pending.
- No HTTP `Idempotency-Key` header or 409 mapping.
- No cleanup/retention policy for completed keys.
- No branch-scope integration test through Sales (only direct sequence scope).
- No P1 closed.

## 6. Evidence

- [DocumentNumberService](../../../src/Avila.Platform/Numbering/DocumentNumberService.cs)
- [IdempotencyService](../../../src/Avila.Platform/Idempotency/IdempotencyService.cs)
- [Migration](../../../src/Avila.Platform/Migrations/20261114200618_AddDocumentNumberingAndIdempotency.cs)
- [Sales integration](../../../src/Modules/Sales/Avila.Modules.Sales/SalesModule.cs)
- [Tests](../../../tests/Avila.ProductionTests/NumberingIdempotencyTests.cs)
- [execution-log.md](execution-log.md)

## 7. Conclusion

T-009 meets its acceptance criteria at the service/database level: transactional gapless numbering, company/branch scope isolation, rollback, 10k emissions under bounded concurrency, and idempotent replay/conflict behavior are tested. It is **verified**. Fiscal numbering and HTTP mapping remain open.
