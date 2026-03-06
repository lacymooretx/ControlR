<#
.SYNOPSIS
    ImmyBot install script for ControlR Agent.
.DESCRIPTION
    Downloads and installs the ControlR agent, connecting it to the specified server and tenant.
    The agent includes built-in auto-update (checks every 6 hours).

.PARAMETER ControlRServerUrl
    The ControlR server URL (e.g., https://control.aspendora.com).
.PARAMETER ControlRTenantId
    The ControlR tenant GUID.
.PARAMETER ControlRInstallerKeyId
    The installer key ID (GUID). Create a Persistent key in ControlR Deploy page.
.PARAMETER ControlRInstallerKeySecret
    The installer key secret string.
#>
param(
    [Parameter(Mandatory)]
    [string]$ControlRServerUrl,

    [Parameter(Mandatory)]
    [string]$ControlRTenantId,

    [Parameter(Mandatory)]
    [string]$ControlRInstallerKeyId,

    [Parameter(Mandatory)]
    [string]$ControlRInstallerKeySecret
)

$ErrorActionPreference = 'Stop'
$ControlRServerUrl = $ControlRServerUrl.TrimEnd('/')

# Determine architecture
if ([Environment]::Is64BitOperatingSystem) {
    $downloadUrl = "$ControlRServerUrl/downloads/win-x64/ControlR.Agent.exe"
} else {
    $downloadUrl = "$ControlRServerUrl/downloads/win-x86/ControlR.Agent.exe"
}

$tempPath = Join-Path $env:TEMP 'ControlR.Agent.exe'

# Download
Write-Host "Downloading ControlR agent from $downloadUrl..."
[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
(New-Object Net.WebClient).DownloadFile($downloadUrl, $tempPath)

if (-not (Test-Path $tempPath)) {
    throw 'Failed to download ControlR agent'
}

$fileSize = [math]::Round((Get-Item $tempPath).Length / 1MB, 1)
Write-Host "Downloaded $fileSize MB"

# Build instance ID from server authority
$instanceId = ([System.Uri]$ControlRServerUrl).Authority

# Install
$installArgs = "install -s $ControlRServerUrl -i $instanceId -t $ControlRTenantId -ks $ControlRInstallerKeySecret -ki $ControlRInstallerKeyId"
Write-Host "Installing ControlR agent..."
$process = Start-Process -FilePath $tempPath -ArgumentList $installArgs -Wait -PassThru -NoNewWindow
if ($process.ExitCode -ne 0) {
    throw "ControlR agent install exited with code $($process.ExitCode)"
}

# Verify service is running
Start-Sleep -Seconds 5
$service = Get-Service -Name 'ControlR.Agent*' -ErrorAction SilentlyContinue | Select-Object -First 1
if ($service -and $service.Status -eq 'Running') {
    Write-Host "ControlR agent installed and running as service '$($service.Name)'"
} elseif ($service) {
    Write-Host "Warning: Service '$($service.Name)' exists but status is $($service.Status)"
} else {
    Write-Host 'Warning: Service not found after install. It may still be starting.'
}

# Clean up
Remove-Item $tempPath -Force -ErrorAction SilentlyContinue
Write-Host 'Install complete.'
