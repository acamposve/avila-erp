# Task Evidence Validation — 2026-11-08

**Scope:** all 37 tasks in [`tasks/`](../tasks/README.md).
**Question:** does each task generate **logs** and **documents in the code**?
**Verdict at validation time:** **No — 0 of 37 tasks were fully validated.**

> **Update 2026-11-08 (T-005):** T-001…T-004 are **`verified`** after PRs #3, #5, #6 and #7 merged. T-005 has a production implementation and test evidence, and is `implemented`, pending review. Verified count: **4/37**. The validation snapshot below is preserved, with the T-001…T-005 rows updated.

This is not a statement that the code is worthless. It is a statement that the repository does not contain the artifacts needed to prove each task was executed, reviewed, and verified.

---

## 1. Validation criteria

A task was considered validated only if all five conditions were true:

1. The task file exists with work, acceptance, evidence and flow.
2. A production implementation exists (not only a spike or a document).
3. A reproducible test or verification exists and is linked to the task.
4. A **task execution log** exists in the repository, with date, commit, commands, environment and result.
5. A **task-specific document** exists in the code, linking implementation, tests and acceptance criteria.

Tasks whose purpose is a decision, review or gate may substitute a decision record or evidence matrix for production code, but still need the log and output document.

---

## 2. Method

- Read all 37 task files (`T-001`…`T-037`) and [`tasks/README.md`](../tasks/README.md).
- Inspected `src/`, `tests/`, `docs/`, `backlog/` and migrations.
- Searched for task references (`T-0xx`) in `.cs` files: **none**.
- Searched for application logging (`ILogger`, `Serilog`, `OpenTelemetry`, `ActivitySource`, `Meter`) in `src/`: **none**.
- Searched for per-task evidence directories: **none**.
- Ran the full test suite: **21/21 passed** (19 spike + 2 production), but the tests are not mapped to task IDs.
- Checked Git state: `tasks/` and `docs/project-execution-plan.md` are **untracked**; `docs/README.md` is modified.

---

## 3. Findings

| ID | Finding | Evidence |
|---|---|---|
| F-01 | **At validation time, no task had a committed execution log.** | No per-task log existed. T-001…T-005 now have one; 5/37. Root `test-output*.log` files are ignored and not task-specific. |
| F-02 | **At validation time, no task had a task-specific output document in code.** | `tasks/T-*.md` are inputs, not evidence. T-001…T-005 now have verification/decision documents; 5/37. |
| F-03 | **The application has no structured logging/observability.** | Zero matches for `ILogger`, `Serilog`, `OpenTelemetry`, `ActivitySource` or `Meter` in `src/`. |
| F-04 | **At baseline: 11 tasks had partial code, 6 had docs/decision material, and 20 had not started. T-001…T-004 are now verified and T-005 implemented; 2 remain docs-only.** | See matrix. |
| F-05 | **The task plan was not committed at validation time.** | `tasks/` and `docs/project-execution-plan.md` were untracked; this change adds them to Git. `docs/README.md` was modified. |
| F-06 | **The original task definitions did not require a log artifact or code document.** | The new [`tasks/evidence/README.md`](../tasks/evidence/README.md) requires `execution-log.md`, `verification.md` and `code-map.md`; T-001 and T-002 now follow it. |
| F-07 | **Tests are not mapped to task IDs.** | 21 tests pass, but no test class or `dotnet test` filter references `T-0xx`. |
| F-08 | **Gate tasks have no evidence matrices.** | T-013, T-022, T-026, T-029, T-032 and T-037 have no committed matrix or result log. |

---

## 4. Summary

| Status | Count | Tasks |
|---|---:|---|
| Fully verified | **4** | T-001, T-002, T-003, T-004 |
| Implemented, evidence pending review | 1 | T-005 |
| Partial code | 10 | T-007, T-009, T-012, T-014, T-015, T-017, T-019, T-020, T-024, T-025 |
| Docs/decision only, pending | 2 | T-018, T-023 |
| Not started | 20 | T-006, T-008, T-010, T-011, T-013, T-016, T-021, T-022, T-026, T-027, T-028, T-029, T-030, T-031, T-032, T-033, T-034, T-035, T-036, T-037 |
| Task-specific logs | **5** | T-001…T-005 |
| Task-specific documents | **5** | T-001…T-005 |
| Structured application logs | **0** | — |

---

## 5. Task matrix

Legend: `—` = not applicable or none; `No` = no task-specific artifact.

| Task | Phase | Code state | Test evidence | Execution log | Task output doc | Validated |
|---|---|---|---|---|---|---|
| T-001 Baseline | 0 | Docs + evidence | 21/21 | **Yes** | **Yes** | **Yes — verified** |
| T-002 Decisions | 0 | Decision evidence | Decision register | **Yes** | **Yes** | **Yes — verified** |
| T-003 Hardening backlog | 0 | Hardening evidence | Hardening matrix | **Yes** | **Yes** | **Yes — verified** |
| T-004 R5 dependency | 0 | R5 review | Document comparison | **Yes** | **Yes** | **Yes — verified** |
| T-005 Tenancy/UoW/RLS | R1 | Production tenancy | 19 production tests | **Yes** | **Yes** | **No — implemented** |
| T-006 Identity | R1 | Not started | — | No | No | **No** |
| T-007 Provisioning | R1 | Partial: company/party | Production slice | No | No | **No** |
| T-008 Authorization/audit | R1 | Not started | — | No | No | **No** |
| T-009 Numbering/idempotency | R1 | Spike only | T-10/T-11/T-18 | No | No | **No** |
| T-010 Configuration/approvals | R1 | Not started | — | No | No | **No** |
| T-011 Platform lifecycle | R1 | Not started | — | No | No | **No** |
| T-012 Operations | R1 | Partial: `/health` + CI | Health smoke + CI | No | No | **No** |
| T-013 R1 gate | R1 | Not started | — | No | No | **No** |
| T-014 Catalog/stock | R2 | Partial: items only | Production slice | No | No | **No** |
| T-015 Inventory controls | R2 | Spike only | T-01/T-02 | No | No | **No** |
| T-016 Purchasing | R2 | Not started | — | No | No | **No** |
| T-017 Sales | R2 | Partial: quote/order | Production slice | No | No | **No** |
| T-018 Fiscal decisions | R2 | Docs, pending | — | No | No | **No** |
| T-019 Billing | R2 | Partial: draft only | Production slice | No | No | **No** |
| T-020 Treasury | R2 | Spike only | T-03 | No | No | **No** |
| T-021 Import/reports | R2 | Not started | — | No | No | **No** |
| T-022 R2 gate | R2 | Not started | — | No | No | **No** |
| T-023 Accounting validation | R3 | Docs, pending | — | No | No | **No** |
| T-024 Accounting posting | R3 | Spike only | T-05/T-06/T-14/T-15/T-19 | No | No | **No** |
| T-025 Finance cutover | R3 | Spike only | T-12 | No | No | **No** |
| T-026 R3 gate | R3 | Not started | — | No | No | **No** |
| T-027 Retail decisions | R4 | Not started | — | No | No | **No** |
| T-028 Retail delivery | R4 | Not started | — | No | No | **No** |
| T-029 R4 gate | R4 | Not started | — | No | No | **No** |
| T-030 Payroll validation | R5 | Not started | — | No | No | **No** |
| T-031 Payroll delivery | R5 | Not started | — | No | No | **No** |
| T-032 R5 gate | R5 | Not started | — | No | No | **No** |
| T-033 Enterprise decisions | R6 | Not started | — | No | No | **No** |
| T-034 Integrations | R6 | Not started | — | No | No | **No** |
| T-035 Production/CRM | R6 | Not started | — | No | No | **No** |
| T-036 Enterprise platform | R6 | Not started | — | No | No | **No** |
| T-037 R6 gate | R6 | Not started | — | No | No | **No** |

---

## 6. What is actually present

### Present and passing

- 19 spike tests: stock/transfer, payments, UoW rollback, RLS/pool, platform RLS/schema, numbering, posting, cutover, journal balance, idempotency, period close.
- 2 production tests: company/party/item/quote/order/invoice draft and tenant isolation.
- Migrations with forced RLS for `admin`, `inventory`, `sales`, `billing`.
- Health endpoint.

### Missing for every task

- A committed log with date, commit, command, output and result.
- A task-specific document linking code, tests and acceptance criteria.
- Structured application logs with `tenant_id`, `correlation_id` and operation context.
- A mapping from task ID to test filter, test class or evidence file.
- Gate matrices for R1–R6.

---

## 7. Recommended evidence standard

For each task, create a committed folder:

```text
tasks/evidence/T-XXX/
├── execution-log.md       # date, commit, environment, commands, outputs, result
├── verification.md        # acceptance criteria → evidence, with links
└── code-map.md            # files, migrations, tests, PR/commit
```

Minimum content:

- `task: T-XXX`
- `commit` / `PR`
- `environment` (PostgreSQL version, container image, .NET SDK)
- `commands` executed
- `result` (pass/fail, counts)
- `artifacts` (test output path, screenshots, query results)
- `known gaps` and `blocked by`

For code tasks, also add `ILogger` structured logs with:

```text
TaskId? no — use operation, module, tenant_id, correlation_id
```

Do not log task IDs in production code unless they add diagnostic value. The task-to-code link belongs in `code-map.md`, not in production log payloads.

---

## 8. Recommended remediation order

1. **Commit `tasks/` and `docs/project-execution-plan.md`** so the validation is reproducible from Git. *(Done in this change.)*
2. Add the evidence template and require it in [`tasks/README.md`](../tasks/README.md). *(Done in this change: [`tasks/evidence/README.md`](../tasks/evidence/README.md).)*
3. Add structured logging (`ILogger`, OpenTelemetry) to the production host and module services. *(Pending authorization.)*
4. Choose one task as an exemplar: **T-005** (UoW/RLS) is the best candidate because production code and tests already exist.
5. Create `tasks/evidence/T-005/` with execution log, verification and code map; map T-04/T-07/T-08/T-09b/T-16.
6. Repeat for T-007, T-014, T-017, T-019.
7. Do not start T-006 until T-005 has a task-specific evidence package.
8. Gate tasks must produce matrices, not only summaries.

---

## 9. Validation run log

```text
Date:        2026-11-08
Tasks read:  37
Code search: no ILogger/Serilog/OpenTelemetry/ActivitySource/Meter in src/
Task refs in .cs: none
Test command: dotnet test Avila.slnx
Result:      21 passed at validation time; current suite 37/37
Task logs found: 5 — T-001…T-005
Task documents found: 5 — T-001…T-005
Git:         at validation time tasks/ and docs/project-execution-plan.md were untracked; committed with this change
```

The test result is valid evidence for the implementation that exists. It is not evidence that every task met its acceptance criteria at validation time, because no task-specific mapping existed then. T-001…T-004 are now verified; T-005 is implemented pending review.

---

## 10. Conclusion

At validation time, the repository had meaningful code, tests and architecture documents, but it did **not** satisfy the requirement that every task generates logs and documents in the code. The gap was structural: task definitions did not require those artifacts, the application has no structured logging, and no task evidence package had been produced.

The first five packages now exist: T-001…T-004 verified and T-005 implemented. No P1 is closed; T-005 is the first production-code evidence package.

---

## 11. Framework status added after this validation

- The task plan, project execution plan and this report are committed.
- [`tasks/evidence/README.md`](../tasks/evidence/README.md) defines the required evidence package and status model.
- [`tasks/README.md`](../tasks/README.md) requires the package for every task.
- **T-001…T-004 are `verified`; T-005 is `implemented`, pending review.** Verified count: **4/37**.
- T-005 adds application code; the framework itself remains documentation/evidence.
- T-006 remains blocked until T-005 is verified and the identity ADR is approved.
