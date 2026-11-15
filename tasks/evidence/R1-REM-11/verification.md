# R1-REM-11 — Verification

**Task:** R1-REM-11 — CI security and SBOM
**Status:** `implemented` — awaiting human review. Not `verified`.
**Date:** 2026-11-15

## 1. Acceptance criteria

| Criterio | Evidencia | Resultado |
|---|---|---|
| CodeQL/SAST integrated | `.github/workflows/codeql.yml` | **Pass (workflow)** |
| Dependency vulnerability audit | Script; zero vulnerable packages | **Pass** |
| High/Critical fails build | Script exits 1 on High/Critical | **Pass (code)** |
| Secret scanning | Gitleaks action + config | **Pass (workflow)** |
| CycloneDX/SPDX SBOM | CycloneDX `bom.json`, 75 packages | **Pass** |
| No secrets in workflows | review; no credentials | **Pass** |
| Existing CI preserved | build/test/clean migrations job unchanged | **Pass** |

## 2. Evidence

| Check | Result |
|---|---|
| Dependency audit | `No vulnerable packages found.` |
| SBOM | 75 packages, 88,439 bytes |
| CodeQL | C# on PR/main/schedule |
| Gitleaks | history scan with synthetic allowlist |
| Workflow syntax | validated by GitHub on PR |

## 3. Security notes

- Security job has `contents: read` only.
- CodeQL job has `security-events: write` only.
- No application secrets are passed to the security job.
- Gitleaks allowlist is limited to tests/evidence and known synthetic passwords.

## 4. Open gaps

- CodeQL/Gitleaks results depend on GitHub execution; not run locally.
- SBOM is not signed/attested.
- No container image scan.
- No Terraform/IaC scan.
- Pending review.

## 5. Evidence

- [CI workflow](../../../.github/workflows/ci.yml)
- [CodeQL workflow](../../../.github/workflows/codeql.yml)
- [Gitleaks config](../../../.gitleaks.toml)
- [Vulnerability script](../../../scripts/ci/check-vulnerabilities.ps1)
- [Operations doc](../../../docs/operations/ci-security.md)
- [execution-log.md](execution-log.md)

## 6. Conclusion

REM-11 meets its acceptance criteria at workflow/script level: CodeQL, dependency audit, secret scanning and CycloneDX SBOM are integrated; high/critical dependency findings fail the build. G1 remains closed.
