param(
    [string]$Image = "prom/prometheus:latest"
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$rules = Join-Path $root "ops/monitoring/prometheus"

docker run --rm --entrypoint promtool -v "${rules}:/etc/prometheus" $Image `
    test rules /etc/prometheus/alerts_test.yml
if ($LASTEXITCODE -ne 0) {
    throw "promtool test rules failed."
}

docker run --rm --entrypoint amtool -v "$(Join-Path $root 'ops/monitoring/alertmanager'):/etc/alertmanager" `
    prom/alertmanager:latest check-config /etc/alertmanager/alertmanager.yml
if ($LASTEXITCODE -ne 0) {
    throw "amtool check-config failed."
}

Write-Output "promtool and amtool checks passed."
