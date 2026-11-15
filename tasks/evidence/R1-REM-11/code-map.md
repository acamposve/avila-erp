# R1-REM-11 — Code map

| Artifact | Path | Purpose |
|---|---|---|
| CI | `.github/workflows/ci.yml` | Security job: audit, secrets, SBOM |
| CodeQL | `.github/workflows/codeql.yml` | C# SAST |
| Gitleaks | `.gitleaks.toml` | Secret scan allowlist |
| Audit script | `scripts/ci/check-vulnerabilities.ps1` | High/Critical gate |
| SBOM | `tasks/evidence/R1-REM-11/raw/bom.json` | CycloneDX artifact |
| Docs | `docs/operations/ci-security.md` | Controls/limits |

## Boundaries

- No deployment/image publishing.
- No signed SBOM.
- No Terraform scan.
- No G1 opening.
