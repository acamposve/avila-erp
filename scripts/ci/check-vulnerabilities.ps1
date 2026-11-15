param(
    [string]$Solution = "Avila.slnx"
)

$ErrorActionPreference = "Stop"
$raw = & dotnet list $Solution package --vulnerable --include-transitive --format json 2>$null | Out-String
if ($LASTEXITCODE -ne 0) {
    throw "dotnet list package failed."
}

$data = $raw | ConvertFrom-Json
$findings = @()
foreach ($project in @($data.projects)) {
    foreach ($framework in @($project.frameworks)) {
        if ($null -eq $framework) {
            continue;
        }

        $packages = @(@($framework.topLevelPackages) + @($framework.transitivePackages)) |
            Where-Object { $null -ne $_ }
        foreach ($package in $packages) {
            $vulnerabilities = @($package.vulnerabilities) | Where-Object { $null -ne $_ }
            foreach ($vulnerability in $vulnerabilities) {
                $findings += [pscustomobject]@{
                    Project = $project.path
                    Package = $package.id
                    Version = $package.resolvedVersion
                    Severity = $vulnerability.severity
                    Advisory = $vulnerability.advisoryurl
                }
            }
        }
    }
}

if ($findings.Count -eq 0) {
    Write-Output "No vulnerable packages found."
    exit 0
}

$findings | Format-Table -AutoSize | Out-String -Width 240 | Write-Output
$blocking = @($findings | Where-Object { $_.Severity -match '^(High|Critical)$' })
if ($blocking.Count -gt 0) {
    Write-Error "$($blocking.Count) high/critical vulnerable package(s) found."
    exit 1
}

Write-Output "Only low/moderate vulnerability findings; build continues."
