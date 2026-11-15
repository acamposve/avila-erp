# R1-CLOSURE-B — Verification

**PR B:** `feat/r1-fiscal-trace-closure`
**Base:** `2251923` (after PR A)
**Status:** implemented; awaiting review. **G1 remains CLOSED**, R2 **FROZEN**.

## 1. Acceptance criteria

| Criterion | Evidence | Result |
|---|---|---|
| Immutable `FiscalRuleVersion` | Table, append-only trigger, unique tenant/key/version | **PASS** |
| Immutable `FiscalDocumentSnapshot` | Table, append-only trigger, unique snapshot | **PASS** |
| Effective-date selection | `GetEffectiveAsync`; overlap validation; test | **PASS** |
| Historical snapshot capture | `CaptureSnapshotAsync`; rule version/id retained | **PASS** |
| Audit trail | `fiscal.rule.published`, `fiscal.snapshot.captured` | **PASS** |
| Data-driven values | Opaque JSON; no tax rates/legal values in code | **PASS** |
| RLS/tenant isolation | Forced RLS + policy registry + app grants | **PASS** |
| `ActivitySource("Avila.Jobs")` | Registered in API tracing | **PASS** |
| HTTP parent → job trace | Job span shares parent trace ID; test | **PASS** |
| Zero tenant/user/PII tags | Test rejects tenant/user tags; only `job.name`/`correlation.id` | **PASS** |
| Full suite | **117/117** (19 spike + 98 production) | **PASS** |

## 2. Key tests

- `Fiscal_rule_versions_are_immutable_and_snapshot_retains_version`
  - publish v1, effective selection, snapshot capture, audit events, overlap rejection.
- `Job_span_has_parent_trace_and_no_tenant_or_user_tags`
  - parent trace propagation, `job.name`, no tenant/user tags.

## 3. Story/AC delta

| Rollup | Before B | After B | Notes |
|---|---:|---:|---|
| Stories PASS | 15/17 | **15/17 + 2 accepted deferrals** | US-PLT-003/004 |
| AC PASS | 11/15 | **13/15 + 2 accepted deferrals** | AC-PLT-01/02 |
| Gate criteria | 12/12 | **12/12** | unchanged |

## 4. Open caveats

- No live OTLP collector/dashboards; test uses in-memory `ActivityListener`.
- No production restore/PITR/WORM/Terraform apply.
- Fiscal foundation does not implement tax calculation or document emission; those remain R2/Billing.
- Deferred items expire at Production Readiness Gate.

## 5. Evidence

- [Fiscal service](../../../src/Avila.Platform/Fiscal/FiscalVersionService.cs)
- [Platform model](../../../src/Avila.Platform/Persistence/PlatformDbContext.cs)
- [Migration](../../../src/Avila.Platform/Migrations/20261115115048_AddFiscalVersionFoundation.cs)
- [Job telemetry](../../../src/Avila.Platform/Jobs/JobTelemetry.cs)
- [Tests](../../../tests/Avila.ProductionTests/FiscalTraceTests.cs)
- [execution-log.md](execution-log.md)
- [final stories/AC](stories-ac-matrix.md)
- [G1 sign-off](g1-signoff-package.md)
