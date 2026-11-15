param(
    [string]$Container = "avila-am-drill",
    [int]$Port = 19093,
    [string]$Image = "prom/alertmanager:latest"
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$config = Join-Path $root "ops/monitoring/alertmanager"

try {
    docker run --rm -d --name $Container -p "${Port}:9093" `
        -v "${config}:/etc/alertmanager" $Image `
        --config.file=/etc/alertmanager/alertmanager.yml | Out-Null
    if ($LASTEXITCODE -ne 0) {
        throw "Could not start Alertmanager drill."
    }

    $ready = $false
    for ($attempt = 0; $attempt -lt 30; $attempt++) {
        Start-Sleep -Milliseconds 500
        docker exec $Container wget -q -O- http://localhost:9093/-/ready 2>$null | Out-Null
        if ($LASTEXITCODE -eq 0) {
            $ready = $true
            break
        }
    }

    if (-not $ready) {
        throw "Alertmanager did not become ready."
    }

    docker exec $Container amtool alert add DrillTest `
        --alertmanager.url=http://localhost:9093 `
        --annotation=summary="Synthetic alert drill" | Out-Null
    if ($LASTEXITCODE -ne 0) {
        throw "amtool alert add failed."
    }

    $alerts = docker exec $Container wget -q -O- http://localhost:9093/api/v2/alerts
    if ($LASTEXITCODE -ne 0 -or $alerts -notmatch "DrillTest") {
        throw "Drill alert was not observed."
    }

    Write-Output "Alertmanager drill PASS"
}
finally {
    docker rm -f $Container 2>$null | Out-Null
}
