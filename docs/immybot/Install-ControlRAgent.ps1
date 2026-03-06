<#
.SYNOPSIS
    ImmyBot install script for ControlR Agent.
.DESCRIPTION
    Uses the ControlR API to discover tenant info, create a temporary installer key,
    download the agent, and install it. Only requires a server URL and PAT.

.PARAMETER ControlRServerUrl
    The ControlR server URL (e.g., https://control.aspendora.com).
.PARAMETER ControlRPersonalAccessToken
    A PAT from a TenantAdministrator user in ControlR.
#>
param(
    [Parameter(Mandatory)]
    [string]$ControlRServerUrl,

    [Parameter(Mandatory)]
    [string]$ControlRPersonalAccessToken
)

$ErrorActionPreference = 'Stop'
[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
$ControlRServerUrl = $ControlRServerUrl.TrimEnd('/')

function Invoke-ControlRApi {
    param(
        [string]$Endpoint,
        [string]$HttpMethod = 'GET',
        [string]$Body = $null
    )
    $params = @{
        Uri             = "$ControlRServerUrl$Endpoint"
        Method          = $HttpMethod
        Headers         = @{
            'x-personal-token' = $ControlRPersonalAccessToken
            'Content-Type'     = 'application/json'
            'Accept'           = 'application/json'
        }
        UseBasicParsing = $true
    }
    if ($Body) { $params['Body'] = $Body }
    Invoke-RestMethod @params
}

# 1. Get tenant ID from PAT
Write-Host 'Discovering tenant info from API...'
$me = Invoke-ControlRApi -Endpoint '/api/me'
$tenantId = $me.tenantId
Write-Host "Tenant ID: $tenantId"

# 2. Create a single-use installer key
Write-Host 'Creating single-use installer key...'
$keyBody = @{
    keyType     = 2  # UsageBased
    allowedUses = 1
    friendlyName = "ImmyBot-$env:COMPUTERNAME-$(Get-Date -Format 'yyyyMMdd-HHmmss')"
} | ConvertTo-Json
$keyResponse = Invoke-ControlRApi -Endpoint '/api/installer-keys' -HttpMethod 'POST' -Body $keyBody
$keyId = $keyResponse.id
$keySecret = $keyResponse.keySecret
Write-Host "Installer key created: $keyId"

# 3. Download agent
$arch = if ([Environment]::Is64BitOperatingSystem) { 'win-x64' } else { 'win-x86' }
$downloadUrl = "$ControlRServerUrl/downloads/$arch/ControlR.Agent.exe"
$tempPath = Join-Path $env:TEMP 'ControlR.Agent.exe'
Write-Host "Downloading agent from $downloadUrl..."
(New-Object Net.WebClient).DownloadFile($downloadUrl, $tempPath)
if (-not (Test-Path $tempPath)) { throw 'Download failed' }
Write-Host "Downloaded $([math]::Round((Get-Item $tempPath).Length / 1MB, 1)) MB"

# 4. Install
$instanceId = ([System.Uri]$ControlRServerUrl).Authority
$installArgs = "install -s $ControlRServerUrl -i $instanceId -t $tenantId -ks $keySecret -ki $keyId"
Write-Host 'Installing agent...'
$proc = Start-Process -FilePath $tempPath -ArgumentList $installArgs -Wait -PassThru -NoNewWindow
if ($proc.ExitCode -ne 0) { throw "Install exited with code $($proc.ExitCode)" }

# 5. Verify
Start-Sleep -Seconds 5
$svc = Get-Service -Name 'ControlR.Agent*' -ErrorAction SilentlyContinue | Select-Object -First 1
if ($svc -and $svc.Status -eq 'Running') {
    Write-Host "Agent installed and running as '$($svc.Name)'"
} elseif ($svc) {
    Write-Host "Warning: service status is $($svc.Status)"
} else {
    Write-Host 'Warning: service not found yet (may still be starting)'
}

Remove-Item $tempPath -Force -ErrorAction SilentlyContinue
Write-Host 'Install complete.'
