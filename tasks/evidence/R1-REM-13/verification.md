# R1-REM-13 — Verification

**Task:** R1-REM-13 — Full-schema backup/restore rehearsal
**Status:** `implemented` — awaiting human review. Not `verified`.
**Date:** 2026-11-15

## 1. Acceptance criteria

| Criterio | Evidencia | Resultado |
|---|---|---|
| Automated CI full dump/restore | `FullRestoreRehearsalTests` in suite | **Pass** |
| Live multi-tenant PostgreSQL | Testcontainers PostgreSQL 18 | **Pass** |
| Two synthetic tenants | Companies/branches/parties/audit | **Pass** |
| RLS policies intact | `pg_class`/`pg_policy` restored check | **Pass** |
| Table counts | 1 company per tenant; 2 audit events | **Pass** |
| Audit hash chain verifiable | `AuditIntegrityService` after restore | **Pass** |
| No regression | 110/110 | **Pass** |

## 2. Test flow

```text
seed A/B → pg_dump -Fc → createdb → pg_restore
→ counts → RLS/force/policy → tenant A read → audit chain
```

Production suite: **91/91**. Full suite: **110/110**.

## 3. Open gaps

- Testcontainers only; no production/volume drill.
- No PITR/WAL/object-storage encryption.
- Roles are cluster-level in the same container; not a separate role restore.
- RPO/RTO provisional.
- Pending review.

## 4. Evidence

- [Test](../../../tests/Avila.ProductionTests/FullRestoreRehearsalTests.cs)
- [Operations doc](../../../docs/operations/full-restore-rehearsal.md)
- [execution-log.md](execution-log.md)

## 5. Conclusion

REM-13 meets its acceptance criteria: an automated full-schema dump/restore with two tenants preserves counts, RLS policies and the audit hash chain. Ready for review; G1 remains closed.
