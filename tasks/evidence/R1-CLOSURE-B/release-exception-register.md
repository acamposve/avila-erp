# R1 release exception register

**Decision:** DEC-021 · **Owner:** @acamposve · **Expiry:** Production Readiness Gate
**Base:** PR B

| Exception | Scope | Owner | Expiry | Status |
|---|---|---|---|---|
| US-PLT-003 commercial plans/metering | Deferred SaaS backoffice | @acamposve | Production Readiness Gate | Accepted |
| US-PLT-004 operator console | Deferred SaaS backoffice | @acamposve | Production Readiness Gate | Accepted |
| AC-PLT-01 plan/module activation | Deferred with US-PLT-003 | @acamposve | Production Readiness Gate | Accepted |
| AC-PLT-02 auto-onboarding notification | Deferred notification only | @acamposve | Production Readiness Gate | Accepted |
| AC-ADM-03 literal stock adjust | Inventory/R2 gate | @acamposve | Inventory module gate | Accepted |
| OTLP collector | Pre-cloud | @acamposve | Production Readiness Gate | Accepted |
| Alertmanager receiver/paging | Pre-cloud | @acamposve | Production Readiness Gate | Accepted |
| Terraform apply | Pre-cloud | @acamposve | Production Readiness Gate | Accepted |
| PITR/WAL | Pre-cloud | @acamposve | Production Readiness Gate | Accepted |
| WORM/external audit anchor | R2+ | @acamposve | Production Readiness Gate | Accepted |

No exception authorizes R2, deployment, shared migrations or production restore.
