# CI security and SBOM

**Estado:** R1-REM-11 · **Fecha:** 2026-11-15 · **Dueño:** @acamposve

## 1. Pipeline

| Control | Implementación | Falla el build |
|---|---|---|
| CodeQL SAST | `.github/workflows/codeql.yml` (C#) | Sí, hallazgos CodeQL |
| Dependencias vulnerables | `scripts/ci/check-vulnerabilities.ps1` | High/Critical |
| Secretos | `gitleaks/gitleaks-action@v2` + `.gitleaks.toml` | Sí |
| SBOM | CycloneDX tool → `artifacts/sbom/bom.json` | Instalación/generación |
| Build/test/migraciones | `.github/workflows/ci.yml` | Sí |

## 2. Dependencias

El script ejecuta:

```bash
dotnet list Avila.slnx package --vulnerable --include-transitive --format json
```

- Parsea proyectos/frameworks/paquetes/vulnerabilidades.
- Falla si `severity` es `High` o `Critical`.
- Low/Moderate se reportan sin bloquear.
- No usa `|| true` ni ignora errores de restore.

## 3. Secret scanning

- Gitleaks sobre historial (`fetch-depth: 0`).
- `.gitleaks.toml` allowlist **solo** tests/evidencia sintética y contraseñas de prueba conocidas.
- No hay credenciales de producción en el repositorio.
- Si se detecta un secreto real, el job falla.

## 4. SBOM

```bash
dotnet tool install --global CycloneDX
dotnet CycloneDX Avila.slnx -o artifacts/sbom -F Json
```

- Formato CycloneDX JSON.
- Artifact `avila-sbom` con retención de GitHub.
- Generado localmente: 75 paquetes, `bom.json` ~88 KB.

## 5. Permisos

- CodeQL: `contents: read`, `security-events: write`.
- Security job: `contents: read`, sin secrets de aplicación.
- Workflows aprobados explícitamente por @acamposve (REM-11).

## 6. Límites

- CodeQL no cubre análisis de infraestructura/Terraform.
- SBOM de dependencias NuGet, no de imágenes de contenedor.
- Gitleaks allowlist puede ocultar un secreto mal clasificado en tests; revisión manual.
- No hay firma/attestation SLSA del SBOM.
- No hay escaneo de imagen porque R1 no publica imagen.
