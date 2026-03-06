<#
.SYNOPSIS
    ImmyBot uninstall script for ControlR Agent.
.DESCRIPTION
    Finds and runs the agent's built-in uninstall command.

.PARAMETER ControlRServerUrl
    The ControlR server URL. Used to determine the instance ID.
#>
param(
    [Parameter(Mandatory)]
    [string]$ControlRServerUrl
)

$ErrorActionPreference = 'Stop'
$instanceId = ([System.Uri]$ControlRServerUrl.TrimEnd('/')).Authority

# Find the agent binary
$agentExe = $null
$candidates = @(
    (Join-Path $env:ProgramFiles "ControlR\$instanceId\ControlR.Agent.exe"),
    (Join-Path $env:ProgramFiles 'ControlR\ControlR.Agent.exe')
)

foreach ($path in $candidates) {
    if (Test-Path $path) { $agentExe = $path; break }
}

# Fallback: get path from service config
if (-not $agentExe) {
    $svc = Get-Service -Name 'ControlR.Agent*' -ErrorAction SilentlyContinue | Select-Object -First 1
    if ($svc) {
        $scOutput = sc.exe qc $svc.Name 2>$null
        $binLine = $scOutput | Where-Object { $_ -match 'BINARY_PATH_NAME' }
        if ($binLine) {
            $exe = (($binLine -replace '.*:\s*', '') -replace '"', '' -split '\s+')[0].Trim()
            if ($exe -and (Test-Path $exe)) { $agentExe = $exe }
        }
    }
}

if (-not $agentExe) {
    Write-Host 'ControlR agent not found. Nothing to uninstall.'
    return
}

$uninstallArgs = "uninstall"
if ($instanceId) { $uninstallArgs += " -i $instanceId" }

Write-Host "Uninstalling: $agentExe $uninstallArgs"
$proc = Start-Process -FilePath $agentExe -ArgumentList $uninstallArgs -Wait -PassThru -NoNewWindow
if ($proc.ExitCode -ne 0) {
    Write-Host "Warning: uninstall exited with code $($proc.ExitCode)"
}

Start-Sleep -Seconds 3
$svc = Get-Service -Name 'ControlR.Agent*' -ErrorAction SilentlyContinue
if (-not $svc) {
    Write-Host 'Agent uninstalled successfully.'
} else {
    Write-Host "Warning: service still exists (status: $($svc.Status)). Reboot may be required."
}
