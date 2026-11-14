param(
    [string]$Image = "postgres:18-alpine",
    [int]$ReadyAttempts = 30
)

$ErrorActionPreference = "Stop"
$suffix = [guid]::NewGuid().ToString("N").Substring(0, 8)
$container = "avila-rto-$suffix"
$password = [guid]::NewGuid().ToString("N")

function Invoke-Sql {
    param(
        [string]$Database,
        [string]$Sql
    )

    docker exec $container psql -U postgres -d $Database -v ON_ERROR_STOP=1 -c $Sql
    if ($LASTEXITCODE -ne 0) {
        throw "psql failed on $Database"
    }
}

try {
    docker run --name $container -e "POSTGRES_PASSWORD=$password" -d $Image | Out-Null
    if ($LASTEXITCODE -ne 0) {
        throw "Could not start disposable PostgreSQL container."
    }

    $ready = $false
    for ($attempt = 0; $attempt -lt $ReadyAttempts; $attempt++) {
        docker exec $container pg_isready -U postgres 2>$null | Out-Null
        if ($LASTEXITCODE -eq 0) {
            $ready = $true
            break
        }

        Start-Sleep -Seconds 1
    }

    if (-not $ready) {
        throw "Disposable PostgreSQL did not become ready."
    }

    $tenantA = "11111111-1111-1111-1111-111111111111"
    $tenantB = "22222222-2222-2222-2222-222222222222"

    Invoke-Sql -Database "postgres" -Sql @"
CREATE ROLE rehearsal_app NOLOGIN;
CREATE SCHEMA ops;
CREATE TABLE ops.rehearsal_notes (
    tenant_id uuid NOT NULL,
    note text NOT NULL
);
ALTER TABLE ops.rehearsal_notes ENABLE ROW LEVEL SECURITY;
ALTER TABLE ops.rehearsal_notes FORCE ROW LEVEL SECURITY;
CREATE POLICY tenant_isolation ON ops.rehearsal_notes
    USING (tenant_id = nullif(current_setting('app.tenant_id', true), '')::uuid)
    WITH CHECK (tenant_id = nullif(current_setting('app.tenant_id', true), '')::uuid);
GRANT USAGE ON SCHEMA ops TO rehearsal_app;
GRANT SELECT, INSERT ON ops.rehearsal_notes TO rehearsal_app;
INSERT INTO ops.rehearsal_notes VALUES ('$tenantA', 'synthetic-a'), ('$tenantB', 'synthetic-b');
"@

    docker exec $container pg_dump -U postgres -d postgres -Fc -f /tmp/avila-rto.dump
    if ($LASTEXITCODE -ne 0) {
        throw "pg_dump failed."
    }

    docker exec $container createdb -U postgres avila_restore
    if ($LASTEXITCODE -ne 0) {
        throw "createdb failed."
    }

    docker exec $container pg_restore -U postgres -d avila_restore /tmp/avila-rto.dump
    if ($LASTEXITCODE -ne 0) {
        throw "pg_restore failed."
    }

    $all = (docker exec $container psql -U postgres -d avila_restore -tAc "SELECT count(*) FROM ops.rehearsal_notes;" | Select-Object -Last 1).Trim()
    $a = (docker exec $container psql -U postgres -d avila_restore -tAc "SET ROLE rehearsal_app; SELECT set_config('app.tenant_id', '$tenantA', false); SELECT count(*) FROM ops.rehearsal_notes;" | Select-Object -Last 1).Trim()
    $b = (docker exec $container psql -U postgres -d avila_restore -tAc "SET ROLE rehearsal_app; SELECT set_config('app.tenant_id', '$tenantB', false); SELECT count(*) FROM ops.rehearsal_notes;" | Select-Object -Last 1).Trim()

    if ($LASTEXITCODE -ne 0) {
        throw "Restored database validation failed."
    }

    if ($all -ne "2" -or $a -ne "1" -or $b -ne "1") {
        throw "Unexpected restored counts: all=$all tenantA=$a tenantB=$b"
    }

    Write-Output "container=$container"
    Write-Output "dump=pg_dump -Fc"
    Write-Output "restore=pg_restore"
    Write-Output "restored_rows_total=$all"
    Write-Output "restored_rows_tenant_a=$a"
    Write-Output "restored_rows_tenant_b=$b"
    Write-Output "result=PASS"
}
finally {
    docker rm -f $container 2>$null | Out-Null
}
