# R1-REM-14 — Operational caveats

**Base:** `2591a4e` · **Date:** 2026-11-15
**Purpose:** transparent release-decision inputs; none is hidden by the 12/12 gate-criteria result.

## 1. Pre-cloud / deployment

| Caveat | Impact | Owner | Disposition |
|---|---|---|---|
| No Terraform apply, no DigitalOcean resources, no production DB | R1 is code/rehearsal only | @acamposve | Pre-cloud placeholder; deployment not authorized |
| No server/container image published | No artifact promotion | Plataforma | R2/deployment decision |
| No CD deployment job | CI proves build/tests/security, not rollout | Plataforma | Intentional R1 restriction |
| RPO 24h / RTO 4h provisional | No production SLA measured | Operaciones | Rehearse in authorized environment before launch |

## 2. Observability

| Caveat | Impact | Owner | Disposition |
|---|---|---|---|
| OTLP collector not provisioned | Traces/metrics are emitted/configured but not stored in a live backend | Operaciones | Pre-cloud; endpoint via `OTEL_EXPORTER_OTLP_ENDPOINT` |
| No Grafana dashboards | No operator visual triage | Operaciones | R2/ops |
| Alertmanager receiver is a localhost webhook placeholder | No paging/on-call delivery | Operaciones | Drill validates mechanics, not delivery |
| Metrics allowlist is exact IP, not CIDR | Gateway/NAT changes need config update | Seguridad | R2/gateway |

## 3. Backup / recovery

| Caveat | Impact | Owner | Disposition |
|---|---|---|---|
| Restore is Testcontainers-only | No production-volume timing or encryption path | Operaciones | Authorized drill required |
| No PITR/WAL replay | RPO is backup-level, not point-in-time | Operaciones | R2/ops |
| Roles are cluster-level in the same container | Separate role/cluster restore untested | Operaciones | Documented |
| No object-storage encryption/retention proof | Compliance evidence incomplete | Operaciones | Pre-cloud |

## 4. Audit / RLS

| Caveat | Impact | Owner | Disposition |
|---|---|---|---|
| Legacy rows `hash_version=0` | Pre-chain events are not hash-verified | Seguridad | Historical; no retroactive backfill |
| No WORM/external anchor/asymmetric signature | Chain is tamper-evident, not tamper-proof against DB owner | Seguridad | R2+ |
| Audit cursor is offset-based | Inserts between pages can shift results | Plataforma | Repeat until manifest for exports |
| RLS DDL changes are migration/PR-reviewed, not DB-event-audited | Policy changes lack independent DDL audit | Seguridad | Accepted R1 caveat |
| Production role provisioning not applied to shared DB | Script reviewed, environment step pending | Plataforma | Deployment gate |

## 5. Product/story residuals

- US-ADM-001: company/branch **edit** absent.
- US-PLT-003: commercial plans/E2E metering absent.
- US-PLT-004: operator console absent (feature flags exist).
- 8 ACs remain PARTIAL: HTTP denial/audit breadth, suspension guard coverage, plan/module activation, auto-onboarding notification, fiscal document version, end-to-end OTel correlation.

These are **release-readiness conditions**, not gate-criteria failures. They require explicit acceptance or closure before production release.

## 6. Security/operational notes

- Password reset/verification email uses a synchronous sender; outbox is R2.
- Auth rate limiting is per-instance/in-memory; no distributed limiter.
- No WebAuthn/new-device notification.
- SMTP real delivery is not exercised in CI.
- Export is complete for R1 modules (PLT/ADM + identity/audit); R2 modules are out of scope.

## 7. What is explicitly NOT authorized

- Opening G1 automatically.
- Production deployment or shared migrations.
- Production restore/backup execution.
- Starting R2 work.
- Accepting exceptions on the owner's behalf.
