# R1-REM-11 — Execution log

- **Fecha:** 2026-11-15
- **Tarea:** R1-REM-11 — CI security scanning and SBOM
- **Preparado por:** agente de IA
- **Responsable/aprobador:** @acamposve
- **Rama:** `feat/r1-rem-11-ci-security`
- **Commit base:** `6a13bdc`
- **Commit de evidencia:** pendiente al redactar; ver PR
- **PR:** pendiente al redactar; se registra al abrir el PR
- **Estado:** `implemented` — pendiente de revisión humana; **no** `verified`

## Comandos ejecutados

| # | Comando | Resultado | Evidencia |
|---|---|---|---|
| 1 | `scripts/ci/check-vulnerabilities.ps1` | **No vulnerable packages found** | [`raw/dependency-audit.txt`](raw/dependency-audit.txt) |
| 2 | CycloneDX tool + `dotnet CycloneDX Avila.slnx` | 75 packages, `bom.json` generado | [`raw/bom.json`](raw/bom.json) |
| 3 | Workflows añadidos | CodeQL + security job | [ci.yml](../../../.github/workflows/ci.yml), [codeql.yml](../../../.github/workflows/codeql.yml) |
| 4 | Comprobación de enlaces Markdown | 0 enlaces roto | Reporte del PR |

## Resultado

- CodeQL C# en PR, push a main y schedule.
- Dependency audit: High/Critical falla; Low/Moderate se reporta.
- Gitleaks con configuración de allowlist para fixtures sintéticos.
- SBOM CycloneDX como artifact `avila-sbom`.
- Sin secretos de producción en workflows.
- No se despliega ni se cambian reglas de protección.
- T-013 no se re-ejecuta; G1 sigue cerrado.

## Bloqueos / pendientes

| Pendiente | Dueño | Estado |
|---|---|---|
| Validación de los workflows en GitHub Actions | @acamposve | Se valida al abrir el PR |
| Firma/attestation del SBOM | R2+ | No implementado |
| Escaneo de imagen/contenedor | R1-REM-11+ | No hay imagen R1 |
| CodeQL para Terraform | R1-REM-11+ | No aplica aún |
| Aprobación de este PR | @acamposve | Pendiente |

## Nota de alcance

El SBOM local se generó con CycloneDX 6.3.1. La ejecución real de CodeQL/Gitleaks ocurre en GitHub Actions; no se simula localmente.
