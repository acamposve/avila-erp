# R1-REM-10 — Verification

**Task:** R1-REM-10 — Audit integrity, query and export
**Status:** `implemented` — awaiting human review. Not `verified`.
**Date:** 2026-11-15

## 1. Acceptance criteria

| Criterio | Evidencia | Resultado |
|---|---|---|
| Per-tenant SHA-256 chain | `previous_hash`/`event_hash`; valid chain test | **Pass** |
| Detect tampering | Tampered action → invalid | **Pass** |
| Detect sequence gap | Deleted middle event → previous mismatch | **Pass** |
| Query cursor pagination | 3 events, limit 2 + next page | **Pass** |
| NDJSON export | `type:"audit"` + manifest | **Pass** |
| Tenant isolation | A page/export excludes B | **Pass** |
| `administration.audit.read` | Owner permission path; member denied by permission model | **Pass** |
| Audit-of-audit | `audit.queried` + `audit.exported` asserted | **Pass** |
| Verify endpoint service | `AuditIntegrityService` test | **Pass** |
| No regression | 109/109 | **Pass** |

## 2. Test coverage

| Test | What it proves |
|---|---|
| `Chain_verifies_and_detects_tampering_or_deletion` | Valid chain + tamper |
| `Delete_breaks_the_chain` | Gap detection |
| `Query_paginates_and_export_is_tenant_scoped` | Pagination/export/isolation |
| `Audit_query_and_export_record_audit_of_audit` | Endpoint + audit-of-audit |

Production suite: **90/90**. Full suite: **109/109**.

## 3. Chain design

- Advisory transaction lock per tenant.
- Canonical before/after JSON.
- Microsecond timestamp truncation to match PostgreSQL.
- `hash_version=0` legacy rows excluded; new rows v1.
- Hash includes tenant, actor, action, entity, before/after, correlation, occurred_at, previous_hash.

## 4. Open gaps

- Legacy rows not backfilled.
- Offset cursor can shift after inserts.
- Last-event deletion undetectable by chain alone.
- No asymmetric signing/external anchor/WORM.
- Audit endpoint handler tested with `HttpContext`, not full HTTP login E2E.
- Pending review.

## 5. Evidence

- [Audit hash](../../../src/Avila.Platform/Authorization/AuditHash.cs)
- [Integrity service](../../../src/Avila.Platform/Authorization/AuditIntegrityService.cs)
- [Audit query/export](../../../src/Avila.Platform/Authorization/AuditIntegrityService.cs)
- [Endpoints](../../../src/Avila.Api/Audit/AuditEndpoints.cs)
- [Tests](../../../tests/Avila.ProductionTests/AuditIntegrityTests.cs)
- [Endpoint tests](../../../tests/Avila.ProductionTests/AuditEndpointTests.cs)
- [Operations doc](../../../docs/operations/audit-integrity.md)
- [execution-log.md](execution-log.md)

## 6. Conclusion

REM-10 meets its acceptance criteria: per-tenant hash chain, tamper/gap detection, cursor query, scoped NDJSON export, audit-of-audit and permission are implemented and tested. G1 remains closed.
