# R1-REM-14 — Release sign-off request

**Gate:** G1
**Base:** `2591a4e`
**Gate criteria:** **12/12 PASS**
**Release readiness:** **CONDITIONAL** — 3 stories and 8 ACs remain PARTIAL
**Current G1 state:** **CLOSED**
**Prepared for:** @acamposve
**Prepared by:** AI agent
**Date:** 2026-11-15

## 1. What is being asked

Please make one explicit decision. PR approval alone does not open G1.

### Option A — Conditional G1 release (recommended only if residuals are accepted)

Open G1 for R1 code/rehearsal with the named story/AC and operational exceptions in [operational-caveats.md](operational-caveats.md), each with owner and expiry. This does **not** authorize production deployment by itself.

### Option B — Keep G1 closed until story/AC gaps close

Close the three stories (company/branch edit, commercial plans/usage, operator console) and the eight AC gaps, then re-run this gate.

### Option C — Descope R1

Formally move named stories/ACs out of R1, update `backlog/releases.md` and dependent releases, then re-run the gate.

## 2. Recommendation

**Keep G1 CLOSED until the owner signs Option A or completes Option B.**

Reasons:

- the 12 gate criteria are met at code/rehearsal level;
- the release model also requires stories done and AC evidence, and 3 stories/8 ACs remain partial;
- operational items (production restore, OTLP collector, paging, deployment) are pre-cloud placeholders;
- the 12/12 result is strong evidence for a **conditional code gate**, not a production launch.

## 3. Explicit sign-off

Copy one line into the PR or release record:

```text
G1 decision: APPROVE CONDITIONAL RELEASE — exceptions accepted: <list>; owners: <names>; expiry: <date>.
Approver: @acamposve. Date: <YYYY-MM-DD>.
```

or:

```text
G1 decision: KEEP CLOSED — close gaps: <list>; re-run gate when complete.
Approver: @acamposve. Date: <YYYY-MM-DD>.
```

## 4. What this request does NOT do

- It does not open G1.
- It does not authorize deployment, production migrations or production restore.
- It does not start R2.
- It does not accept exceptions on the owner's behalf.
- It does not change the 12/12 gate-criteria result.

## 5. Evidence package

- [12/12 gate matrix](r1-gate-matrix.md)
- [Stories/AC rollup](stories-ac-matrix.md)
- [Operational caveats](operational-caveats.md)
- [Verification](verification.md)
- [Execution log](execution-log.md)
- [Raw test output](raw/test-output.txt)
- [API smoke](raw/api-smoke.txt)
- [Link check](raw/link-check.txt)
- [CI baseline](raw/ci.txt)
