# T-008 — Verification

**Task:** [T-008 — Permisos, auditoría y soporte](../../T-008-authorization-audit.md)
**Status:** `verified` — approved by @acamposve; PR #11 merged 2026-11-14 (`e0302c7`).
**Date:** 2026-11-14

## 1. Acceptance criteria

| Criterio de T-008 | Evidencia | Resultado |
|---|---|---|
| Permiso denegado sin efectos | Member without scope; denied quote permission; 0 quotes | **Pass** |
| Separación de tenants | Cross-tenant principal denied; audit and scope RLS; tenant B sees no tenant A audit | **Pass** |
| Registro actor/antes/después/correlación | Audit test with actor, before/after JSON, correlation `corr-1` | **Pass** |
| Protección ante modificación de auditoría | App role UPDATE → `42501`; trigger append-only; value unchanged | **Pass** |
| Soporte con ticket, justificación, expiración y mínimo privilegio | Grant/read-only/expiry/revoke test + audit events | **Pass** |
| RBAC con scope empresa/sucursal/almacén | Member allowed only for assigned company; warehouse/branch scope model | **Pass** |

## 2. Test coverage

| Test | What it proves |
|---|---|
| `Member_without_scope_is_denied_without_side_effects` | Deny by default; no quote row |
| `Member_with_company_scope_is_allowed_only_for_that_company` | Scope isolation; stock adjust denied |
| `Audit_event_is_tenant_scoped_and_append_only` | RLS, actor/JSON/correlation, mutation rejected |
| `Support_access_requires_ticket_and_expires_and_is_audited` | Ticket/justification, expiry, revoke, audit |
| `Cross_tenant_principal_is_denied` | Principal tenant must match UoW tenant |

Production suite: **35/35**. Full suite: **54/54**.

## 3. Permission model

| Role | Permissions |
|---|---|
| `owner` | All tenant permissions |
| `admin` | All tenant permissions |
| `member` | Read company/stock/quote; create quote; **no** stock adjust, user manage or support |

A member needs an explicit `AccessScope` row (`company|branch|warehouse`) for scoped operations. `owner`/`admin` are tenant-wide.

> The role templates are static in code. Custom role CRUD and role-permission persistence remain T-008/T-010 work.

## 4. Audit model

| Property | Implementation |
|---|---|
| Table | `platform.audit_events` |
| Fields | tenant, actor, action, entity, before/after JSONB, correlation, timestamp |
| RLS | Forced by `app.tenant_id` |
| Append-only | Trigger rejects UPDATE/DELETE; `avila_app` has only SELECT/INSERT |
| Test | App-role UPDATE → `42501`; original action unchanged |

## 5. Support model

| Property | Implementation |
|---|---|
| Table | `platform.support_access_grants` |
| Required | ticket + justification + future expiry |
| Default | `is_read_only = true` |
| Revocation | `revoked_at` + audit event |
| Validation | unexpired and not revoked |

## 6. Open gaps

- No HTTP 403 path/business endpoint; authorization is service-level.
- Denied attempts are not audited in a separate transaction.
- No custom roles/CRUD; permissions are static templates.
- No support console or platform-operator role; `support_user_id` is not tied to a platform membership.
- No audit query/export UI or cryptographically tamper-evident chain.
- Production role grants are still deployment work.
- No P1 closed.

## 7. Evidence

- [AuthorizationService](../../../src/Avila.Platform/Authorization/AuthorizationService.cs)
- [AuditService](../../../src/Avila.Platform/Authorization/AuditService.cs)
- [SupportAccessService](../../../src/Avila.Platform/Authorization/SupportAccessService.cs)
- [Platform migration](../../../src/Avila.Platform/Migrations/20261114195019_AddAuthorizationAuditSupport.cs)
- [Tests](../../../tests/Avila.ProductionTests/AuthorizationAuditTests.cs)
- [execution-log.md](execution-log.md)

## 8. Conclusion

T-008 meets its acceptance criteria at the service/database level: deny-by-default RBAC with scopes, tenant isolation, append-only audit with before/after/correlation, and time-boxed read-only support access are implemented and tested. It is **verified**. HTTP enforcement and denied-attempt audit remain open.
