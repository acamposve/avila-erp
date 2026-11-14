# R1-REM-03 — Verification

**Task:** R1-REM-03 — RLS audit, policy registry, denied-attempt audit
**Status:** `implemented` — awaiting human review. Not `verified`.
**Date:** 2026-11-14

## 1. Acceptance criteria

| Criterio | Evidencia | Resultado |
|---|---|---|
| Registro de políticas por tabla tenant | `platform.rls_policy_registry` seeded from live tenant tables | **Pass** |
| CI detecta RLS/force ausente | `pg_class.relrowsecurity` + `relforcerowsecurity` assertions | **Pass** |
| CI detecta política ausente/incorrecta | `pg_policy` policy name + `app.tenant_id` expression | **Pass** |
| CI detecta tabla no registrada | `information_schema` left join registry; count must be 0 | **Pass** |
| Denegación persiste tras rollback | `SecurityAuditWriter` separate UoW; tests rollback business tx | **Pass** |
| Cross-tenant auditado en tenant objetivo | A→B test; actor/target tenants in payload | **Pass** |
| Sin PII/secretos | Payload only permission/reason/tenant/scope | **Pass** |
| Sin regresión | Full suite 76/76 | **Pass** |

## 2. Test coverage

| Test | What it proves |
|---|---|
| `Live_catalog_has_forced_rls_and_registered_policy_for_every_tenant_table` | Live catalog and registry completeness |
| `Denied_permission_is_audited_after_business_transaction_rolls_back` | Independent audit survives rollback |
| `Cross_tenant_attempt_is_audited_in_target_tenant_after_rollback` | Cross-tenant denial target/source |

Production suite: **57/57**. Full suite: **76/76**.

## 3. Catalog checks

The test fails if any table with `tenant_id` in `admin`, `inventory`, `sales`, `billing` or `platform`:

- lacks a registry row;
- has `relrowsecurity = false`;
- has `relforcerowsecurity = false`;
- lacks policy `tenant_isolation`;
- policy expression does not reference `app.tenant_id`.

## 4. Denied-audit properties

| Property | Implementation |
|---|---|
| Independent connection | New `TenantUnitOfWork` per denial |
| Commit before return | `SecurityAuditWriter` commits its own transaction |
| Actor/source/target | `actor_user_id`, `actorTenantId`, `targetTenantId` |
| Correlation | Caller-supplied or generated |
| Business rollback | Does not remove security event |

## 5. Open gaps

- PostgreSQL does not audit direct `psql` RLS-read denials; app-level denials only.
- Hash chain and audit query/export are REM-10.
- No production migration/role application.
- Pending review.

## 6. Evidence

- [Registry migration](../../../src/Avila.Platform/Migrations/20261114214054_AddRlsPolicyRegistry.cs)
- [Security audit writer](../../../src/Avila.Platform/Authorization/SecurityAuditWriter.cs)
- [Authorization integration](../../../src/Avila.Platform/Authorization/AuthorizationService.cs)
- [RLS tests](../../../tests/Avila.ProductionTests/RlsPolicyTests.cs)
- [Denied-audit tests](../../../tests/Avila.ProductionTests/SecurityAuditTests.cs)
- [Operations doc](../../../docs/operations/rls-audit-and-registry.md)
- [execution-log.md](execution-log.md)

## 7. Conclusion

REM-03 meets its acceptance criteria: live-catalog RLS/policy enforcement is CI-blocking, and denied attempts persist independently of the offending transaction. Ready for review; G1 remains closed.
