# T-010 — Code map

## 1. Parameters and preferences

| Artefacto | Ruta | Propósito |
|---|---|---|
| Configuration service | `src/Avila.Platform/Configuration/ConfigurationService.cs` | Effective selection, publish, preferences |
| Setting versions | `platform.setting_versions` | Version, source, vigencia, immutable history |
| Tenant settings | `platform.tenant_settings` | Current organization preferences |

## 2. Approvals

| Artefacto | Ruta | Propósito |
|---|---|---|
| Approval service | `src/Avila.Platform/Approvals/ApprovalService.cs` | Rules, request, decision, threshold gate |
| Rules | `platform.approval_rules` | Type + threshold + required role |
| Requests | `platform.approval_requests` | Requester, amount, status, decision |
| Migration | `src/Avila.Platform/Migrations/20261114202006_AddSettingVersionsAndApprovals.cs` | Tables, RLS, immutable trigger |

## 3. Permissions

| Permission | Roles |
|---|---|
| `administration.settings.manage` | owner, admin |
| `administration.approval.manage` | owner, admin |

## 4. Tests

| Test | File |
|---|---|
| Effective dates, immutability, approval, audit | `tests/Avila.ProductionTests/ConfigurationApprovalTests.cs` |
| Fixture grants/RLS | `tests/Avila.ProductionTests/ProductionFixture.cs` |

## 5. Documentation

| Documento | Change |
|---|---|
| `docs/architecture/security.md` | T-010 parameter/approval implementation |
| `docs/tasks-validation.md` | T-009 verified, T-010 implemented |
| `docs/project-execution-plan.md` | R1 status |

## 6. Boundaries

- No HTTP/UI.
- No notifications.
- No versioned approval rules.
- No regulatory values.
- No module integration yet.
- No shared migration or deployment.
