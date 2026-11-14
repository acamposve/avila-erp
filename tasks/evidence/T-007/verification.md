# T-007 — Verification

**Task:** [T-007 — Aprovisionamiento y maestros administrativos](../../T-007-provisioning.md)
**Status:** `verified` — approved by @acamposve; PR #10 merged 2026-11-14 (`8262783`).
**Date:** 2026-11-14

## 1. Acceptance criteria

| Criterio de T-007 | Evidencia | Resultado |
|---|---|---|
| Dos organizaciones sintéticas se aprovisionan sin acceso cruzado | Provisioning tests; branch queries under RLS for A/B | **Pass** |
| Perfil fiscal incompleto bloquea emisión cuando corresponda | Billing gate; negative test with rollback | **Pass** |
| Empresa, sucursal, terceros con roles y perfil fiscal | Company + branch + party existing; provisioning creates company/branch | **Pass** |
| Preferencias iniciales | `platform.tenant_settings` seed (`locale=es-VE`) | **Pass** |
| Trazabilidad `US-PLT-001`, `ADM-001/002/010`, `AC-ADM-01/05` | Tests and story map | **Pass** |

## 2. Test coverage

| Test | What it proves |
|---|---|
| `Two_organizations_are_provisioned_without_cross_access` | Two orgs, company/branch per org, branch RLS, one preference per tenant |
| `Provisioned_administrator_invitation_is_owner_scoped` | Admin invitation accepted; owner membership; login |
| `Incomplete_fiscal_profile_blocks_invoice_and_rolls_back_flow` | Fiscal gate and full flow rollback |

Production suite: **30/30**. Full suite: **49/49**.

## 3. Provisioning flow

```text
avila_platform  -> platform.tenants
avila_app       -> admin.companies + admin.branches (one tenant transaction)
avila_app       -> platform.tenant_settings (locale=es-VE)
avila_identity  -> admin invitation (hashed, 7 days)
```

The platform, tenant and identity stores use separate roles. This is a **staged operation**, not one distributed transaction.

## 4. Fiscal profile gate

`BillingModule.CreateDraftAsync` now requires `CompanyFiscalProfileComplete`.

- Incomplete profile → `DomainException("FISCAL_PROFILE_INCOMPLETE: ...")`.
- `SalesFlowService` owns a single UoW, so the invoice draft failure rolls back company, party, item, quote and order.
- Test verifies zero company rows after rejection.

## 5. Open gaps

- No compensation/rollback if identity invitation fails after the tenant commit.
- No idempotency key for provisioning; retry can leave a tenant without invitation.
- No HTTP provisioning endpoint; no authorization for provisioning yet (T-008).
- No branch/fiscal-profile update or soft-delete.
- No email delivery of the admin invitation.
- Preferences are a single seeded `locale`; no versioned settings (T-010).
- `avila_platform` role and key/secret management are test-only/deployment tasks.
- Third-party fiscal profile beyond `TaxId`/roles is not expanded.

## 6. What was not done

- No T-008 RBAC/audit.
- No fiscal invoice emission.
- No branch update/delete.
- No production role provisioning.
- No shared migration or deployment.

## 7. Evidence

- [TenantProvisioningService](../../../src/Avila.Application/TenantProvisioningService.cs)
- [PlatformTenantProvisioner](../../../src/Avila.Platform/Persistence/PlatformTenantProvisioner.cs)
- [Administration module](../../../src/Modules/Administration/Avila.Modules.Administration/AdministrationModule.cs)
- [Branch migration](../../../src/Modules/Administration/Avila.Modules.Administration/Migrations/20261114193205_AddBranchesAndFiscalProfile.cs)
- [Provisioning tests](../../../tests/Avila.ProductionTests/ProvisioningTests.cs)
- [execution-log.md](execution-log.md)

## 8. Conclusion

T-007 meets its acceptance criteria: two organizations are provisioned with company, branch, preference and admin invitation; branch RLS isolates them; an incomplete fiscal profile blocks invoice draft creation. It is **verified**. The staged atomicity and HTTP authorization gaps must remain visible before production.
