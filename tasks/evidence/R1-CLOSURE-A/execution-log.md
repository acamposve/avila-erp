# R1-CLOSURE-A — Execution log

- **Fecha:** 2026-11-15
- **Tarea:** PR A — US-ADM-001 + HTTP denial/audit/suspension closure
- **Rama:** `feat/r1-adm-write-surface`
- **Base:** `4508426`
- **Preparado por:** agente de IA
- **Responsable/aprobador:** @acamposve
- **Estado:** `implemented` — pendiente de revisión; **G1 CLOSED**, R2 frozen

## Comandos ejecutados

| # | Comando | Resultado | Evidencia |
|---|---|---|---|
| 1 | `dotnet test Avila.slnx` | **115/115 PASS** (19 spike + 96 producción) | [`raw/test-output.txt`](raw/test-output.txt) |
| 2 | `AdministrationHttpE2eTests` | 1/1; login MFA, company/branch create/update, 409, 403, 423 | [`raw/test-output.txt`](raw/test-output.txt) |
| 3 | Build | 0 warnings / 0 errors | [`raw/environment.txt`](raw/environment.txt) |
| 4 | Link check | 0 broken | [`raw/link-check.txt`](raw/link-check.txt) |

## Resultado

- `POST/PATCH /api/v1/administration/companies`; `POST/PATCH /api/v1/administration/branches`; `POST /api/v1/administration/parties`.
- `Version` optimistic concurrency: stale update → **409**.
- Create/update audit with before/after, actor, tenant, correlation.
- Member mutation → **403** + persisted `authorization.denied`.
- Missing/cross-tenant ID → **404** + independent `security.resource.not_found`.
- `TenantWriteGuardMiddleware`:
  - POST/PUT/PATCH/DELETE under `/api/v1` only;
  - auth and safe reads/exports exempt;
  - suspended tenant → **423 Locked** before handler;
  - `tenant.write.blocked` audit.
- Migration `AddAdministrationVersion`.
- AC-ADM-03 security pattern closed with denied company/branch mutation; literal stock adjust deferred to Inventory/R2 via `DEC-021`.

## Pendiente / no incluido

| Pendiente | Dueño | Estado |
|---|---|---|
| `US-PLT-003/004`, AC-PLT-01/02 | @acamposve | Excepción DEC-021 |
| AC-FIS-01 / AC-OBS-01 | PR B | Pendiente |
| Literal stock adjust | Inventory/R2 | Diferido; R2 FROZEN |
| Live collector/Terraform/PITR/WORM | Production Readiness Gate | Excepción DEC-021 |
| Aprobación de este PR | @acamposve | Pendiente |
