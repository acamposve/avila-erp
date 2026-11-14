# T-008 — Code map

## 1. Authorization

| Artefacto | Ruta | Propósito |
|---|---|---|
| Permission catalog | `src/Avila.Platform/Authorization/AuthorizationService.cs` | Permissions and role templates |
| Authorization service | `src/Avila.Platform/Authorization/AuthorizationService.cs` | Tenant/scope checks, assign scope |
| Access scopes | `platform.access_scopes` | Company/branch/warehouse scope rows |

## 2. Audit

| Artefacto | Ruta | Propósito |
|---|---|---|
| Audit writer | `src/Avila.Platform/Authorization/AuditService.cs` | Actor/before/after/correlation insert |
| Audit table | `platform.audit_events` | JSONB, RLS, append-only trigger |
| Migration | `src/Avila.Platform/Migrations/20261114195019_AddAuthorizationAuditSupport.cs` | Tables, RLS, trigger |

## 3. Support access

| Artefacto | Ruta | Propósito |
|---|---|---|
| Support service | `src/Avila.Platform/Authorization/SupportAccessService.cs` | Grant/validate/revoke + audit |
| Support table | `platform.support_access_grants` | Ticket, justification, expiry, read-only |

## 4. Tests

| Test | File |
|---|---|
| RBAC, audit, support, tenant isolation | `tests/Avila.ProductionTests/AuthorizationAuditTests.cs` |
| Fixture grants/migrations | `tests/Avila.ProductionTests/ProductionFixture.cs` |

## 5. Documentation

| Documento | Change |
|---|---|
| `docs/architecture/security.md` | T-008 authorization/audit/support implementation |
| `docs/tasks-validation.md` | T-007 verified, T-008 implemented |
| `docs/project-execution-plan.md` | Phase 0 status |

## 6. Boundaries

- No HTTP business endpoint.
- No custom role CRUD.
- No denied-attempt audit transaction.
- No support console.
- No production roles.
- No shared migration or deployment.
