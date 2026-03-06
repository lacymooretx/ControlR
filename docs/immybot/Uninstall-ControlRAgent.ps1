<#
.SYNOPSIS
    ImmyBot uninstall script for ControlR Agent.
.DESCRIPTION
    Uninstalls the ControlR agent by running its built-in uninstall command.

.PARAMETER ControlRServerUrl
    The ControlR server URL. Used to determine instance ID for multi-instance installs.
#>
param(
    [Parameter(Mandatory)]
    [string]$ControlRServerUrl
)

$ErrorActionPreference = 'Stop'
$ControlRServerUrl = $ControlRServerUrl.TrimEnd('/')

$instanceId = ([System.Uri]$ControlRServerUrl).Authority

# Find the agent executable from the install directory
$installDirs = @(
    (Join-Path $env:ProgramFiles "ControlR\$instanceId"),
    (Join-Path $env:ProgramFiles 'ControlR')
)

$agentExe = $null
foreach ($dir in $installDirs) {
    $candidate = Join-Path $dir 'ControlR.Agent.exe'
    if (Test-Path $candidate) {
        $agentExe = $candidate
        break
    }
}

if (-not $agentExe) {
    # Try getting path from the service
    $service = Get-Service -Name 'ControlR.Agent*' -ErrorAction SilentlyContinue | Select-Object -First 1
    if ($service) {
        $scOutput = sc.exe qc $service.Name 2>$null
        $binPath = ($scOutput | Where-Object { $_ -match 'BINARY_PATH_NAME' }) -replace '.*:\s*', '' -replace '"', ''
        $exePath = ($binPath -split '\s+' | Select-Object -First 1).Trim()
        if ($exePath -and (Test-Path $exePath)) {
            $agentExe = $exePath
        }
    }
}

if (-not $agentExe) {
    Write-Host 'ControlR agent not found. Nothing to uninstall.'
    return
}

# Run the built-in uninstall command
$uninstallArgs = "uninstall"
if ($instanceId) {
    $uninstallArgs += " -i $instanceId"
}

Write-Host "Uninstalling ControlR agent: $agentExe $uninstallArgs"
$process = Start-Process -FilePath $agentExe -ArgumentList $uninstallArgs -Wait -PassThru -NoNewWindow
if ($process.ExitCode -ne 0) {
    Write-Host "Warning: Uninstall exited with code $($process.ExitCode)"
}

# Verify removal
Start-Sleep -Seconds 3
$service = Get-Service -Name 'ControlR.Agent*' -ErrorAction SilentlyContinue
if (-not $service) {
    Write-Host 'ControlR agent uninstalled successfully.'
} else {
    Write-Host "Warning: Service '$($service.Name)' still exists (status: $($service.Status)). A reboot may be required."
}
