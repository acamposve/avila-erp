# R1-REM-14 — Verification

**Task:** R1-REM-14 — Re-run T-013 R1 Gate against main
**Base:** `2591a4e`
**Status:** gate evidence complete; **G1 remains closed pending sign-off**
**Date:** 2026-11-15

## 1. Commands and results

| Check | Command | Result |
|---|---|---|
| Full suite | `dotnet test Avila.slnx` | **114/114 PASS** (19 spike + 95 production) |
| Production suite | `dotnet test tests/Avila.ProductionTests/...` | **95/95 PASS** |
| Link check | 198 Markdown files | **0 broken** |
| API smoke | `/health`, `/health/live`, `/health/ready`, `/metrics` | **200** each |
| Main CI | GitHub Actions run 43707940523 | **success 1m12s** |
| CodeQL main | GitHub Actions run 43707940503 | **success 2m18s** |
| Merge state | PRs #27/#28/#29 | **MERGED** into `2591a4e` |

Raw evidence: [tests](raw/test-output.txt), [API](raw/api-smoke.txt), [links](raw/link-check.txt), [CI](raw/ci.txt), [git](raw/git-log.txt).

## 2. Gate matrix result

| Status | Count |
|---|---:|
| PASS | **12** |
| PARTIAL | 0 |
| FAIL | 0 |

All 12 original criteria have merged implementation + automated evidence. The prior FAIL (complete export) is closed; the prior eight PARTIALs are closed at the stated R1 code/rehearsal level.

## 3. Story/AC result

| Rollup | PASS | PARTIAL | FAIL |
|---|---:|---:|---:|
| 17 R1 stories | 14 | 3 | 0 |
| 15 R1 acceptance criteria | 7 | 8 | 0 |

This is the **release-readiness caveat**: gate criteria 12/12 does not mean every story/AC is done. See [stories-ac-matrix.md](stories-ac-matrix.md).

## 4. CI security evidence

- Main CI: build, tests, clean migrations.
- CodeQL: success on main.
- Security job: dependency audit, Gitleaks, CycloneDX SBOM (PR #28).
- No deployment step was added.

## 5. Operational caveats

See [operational-caveats.md](operational-caveats.md). The most material:

- RPO/RTO provisional 24h/4h; restore is Testcontainers-only.
- No OTLP collector/dashboards/production paging.
- No production deployment, Terraform apply or shared DB migration.
- Audit legacy rows `hash_version=0`; no WORM/external anchor.
- RLS DDL is migration/PR-reviewed, not independently DDL-event-audited.
- Three stories and eight ACs remain PARTIAL.

## 6. Conclusion

**Gate-criteria assessment: 12/12 PASS.**
**G1: remains CLOSED.**
**Release: conditional; explicit @acamposve sign-off required.**

No G1 opening, deployment or R2 work was performed.
