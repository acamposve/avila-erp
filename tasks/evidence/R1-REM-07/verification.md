# R1-REM-07 — Verification

**Task:** R1-REM-07 — Authorized tenant export endpoint
**Status:** `implemented` — awaiting human review. Not `verified`.
**Date:** 2026-11-15

## 1. Acceptance criteria

| Criterio | Evidencia | Resultado |
|---|---|---|
| Owner/admin only | Handler role guard; owner 200/member 403 tests | **Pass** |
| Member 403 | `Member_export_is_forbidden_and_does_not_start_stream` | **Pass** |
| Tenant from authenticated context only | `SessionPrincipal` + UoW match; query/route override 400 | **Pass** |
| Suspended tenant can export | Lifecycle suspend + endpoint 200 test | **Pass** |
| Requested/completed/failed audit | `ExportAuditWriter`; action assertions | **Pass** |
| Stream NDJSON + SHA manifest | `StreamNdjsonAsync`; NDJSON assertions | **Pass** |
| No unbounded buffering | One line + one record/canonical bytes at a time; contributors stream | **Pass (design/test)** |
| Failed export audit | Throwing contributor; 500 + failed event | **Pass** |
| No regression | 96/96 | **Pass** |

## 2. Test coverage

| Test | What it proves |
|---|---|
| `Owner_export_streams_ndjson_and_audits_requested_and_completed` | 200, NDJSON, manifest, audit |
| `Member_export_is_forbidden_and_does_not_start_stream` | 403, no stream |
| `Caller_supplied_tenant_override_is_rejected` | 400 override |
| `Suspended_tenant_can_still_export` | Portability during suspension |
| `Failed_export_is_audited_and_returns_500_when_stream_not_started` | Failed audit |
| `Ndjson_stream_is_deterministic_and_manifest_hash_is_stable` | Deterministic manifest |
| `Streaming_uses_async_records_without_materializing_a_section` | Streaming contract |

Production suite: **77/77**. Full suite: **96/96**.

## 3. Streaming/audit design

- `ITenantExportStreamContributor` returns `IAsyncEnumerable<TenantExportRecord>`.
- `TenantExportRegistry.StreamNdjsonAsync` writes header, section, record, section_end, manifest lines.
- Per-line buffer only; canonical record bytes; incremental section hash.
- `ExportAuditWriter` uses independent UoW/connection/transaction; requested before stream, completed after manifest, failed on exception.
- `Content-Type: application/x-ndjson`; `Cache-Control: no-store`.
- Suspended tenants are not passed through `TenantAccessGuard`.

## 4. Open gaps

- No HTTP server E2E with a real login; handler tested with HttpContext and real DB.
- No size quota, resumable cursor, zip, detached signature or async download.
- Slow-client/cancellation audit is best-effort.
- Identity/admin/audit contributors are from REM-06 (stacked).
- Pending review.

## 5. Evidence

- [Endpoint](../../../src/Avila.Api/Exporting/TenantExportEndpoint.cs)
- [Export audit writer](../../../src/Avila.Platform/Exporting/ExportAuditWriter.cs)
- [Streaming registry](../../../src/Avila.Platform/Exporting/TenantExportRegistry.cs)
- [Endpoint tests](../../../tests/Avila.ProductionTests/ExportEndpointTests.cs)
- [Streaming tests](../../../tests/Avila.ProductionTests/TenantExportStreamingTests.cs)
- [Operations doc](../../../docs/operations/export-endpoint.md)
- [execution-log.md](execution-log.md)

## 6. Conclusion

REM-07 meets its acceptance criteria at handler/service level: owner/admin-only export, no tenant override, suspended read/export, independent requested/completed/failed audit, and bounded NDJSON streaming with SHA-256 manifest. Ready for review; G1 remains closed.
