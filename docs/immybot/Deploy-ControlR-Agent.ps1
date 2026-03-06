#Requires -Modules ImmyBot
<#
.SYNOPSIS
    ImmyBot metascript to deploy ControlR agent with automatic tenant-based device groups.

.DESCRIPTION
    This script deploys the ControlR agent to target computers via ImmyBot.
    It automatically creates a device group in ControlR matching the ImmyBot tenant name
    (if one doesn't already exist) and assigns the device tag for that group.

    The agent includes built-in auto-update (checks every 6 hours via SHA256 hash comparison),
    so devices will stay current without any additional maintenance.

.PARAMETER Method
    ImmyBot auto-provides: get, test, or set.

.PARAMETER ControlRServerUrl
    The ControlR server URL (e.g., https://control.aspendora.com).
    Configure as an ImmyBot Configuration Variable.

.PARAMETER ControlRTenantId
    The ControlR tenant GUID.
    Configure as an ImmyBot Configuration Variable.

.PARAMETER ControlRInstallerKeyId
    The ControlR installer key ID (GUID). Create a Persistent key in ControlR Deploy page.
    Configure as an ImmyBot Configuration Variable.

.PARAMETER ControlRInstallerKeySecret
    The ControlR installer key secret string.
    Configure as an ImmyBot Configuration Variable (Secret).

.PARAMETER ControlRPersonalAccessToken
    A Personal Access Token from a TenantAdministrator user in ControlR.
    Used to create/lookup device groups via the API.
    Configure as an ImmyBot Configuration Variable (Secret).

.PARAMETER TenantName
    ImmyBot auto-provides the tenant (client) name.
#>
param(
    [Parameter(Mandatory)]
    [string]$Method,

    [Parameter(Mandatory)]
    [string]$ControlRServerUrl,

    [Parameter(Mandatory)]
    [string]$ControlRTenantId,

    [Parameter(Mandatory)]
    [string]$ControlRInstallerKeyId,

    [Parameter(Mandatory)]
    [string]$ControlRInstallerKeySecret,

    [Parameter(Mandatory)]
    [string]$ControlRPersonalAccessToken,

    [Parameter(Mandatory)]
    [string]$TenantName
)

$ErrorActionPreference = 'Stop'
$ControlRServerUrl = $ControlRServerUrl.TrimEnd('/')

# --- Helper: Call ControlR API ---
function Invoke-ControlRApi {
    param(
        [string]$Endpoint,
        [string]$Method = 'GET',
        [object]$Body = $null
    )

    $headers = @{
        'x-personal-token' = $ControlRPersonalAccessToken
        'Content-Type'     = 'application/json'
        'Accept'           = 'application/json'
    }

    $params = @{
        Uri     = "$ControlRServerUrl$Endpoint"
        Method  = $Method
        Headers = $headers
    }

    if ($Body) {
        $params['Body'] = ($Body | ConvertTo-Json -Depth 10)
    }

    Invoke-RestMethod @params
}

# --- Helper: Find or create device group for this tenant ---
function Get-OrCreateDeviceGroup {
    param([string]$GroupName)

    # Get all existing groups
    $groups = Invoke-ControlRApi -Endpoint '/api/device-groups'

    # Look for an existing group matching this tenant name
    $existing = $groups | Where-Object { $_.name -eq $GroupName } | Select-Object -First 1

    if ($existing) {
        Write-Host "Found existing device group '$GroupName' (ID: $($existing.id))"
        return $existing
    }

    # Create a new group
    Write-Host "Creating device group '$GroupName'..."
    $newGroup = Invoke-ControlRApi -Endpoint '/api/device-groups' -Method 'POST' -Body @{
        name        = $GroupName
        description = "Auto-created from ImmyBot tenant: $GroupName"
        groupType   = 0
        sortOrder   = 0
    }

    Write-Host "Created device group '$GroupName' (ID: $($newGroup.id))"
    return $newGroup
}

switch ($Method) {
    'get' {
        # Check if ControlR agent service is installed on the target
        $result = Invoke-ImmyCommand {
            $service = Get-Service -Name 'ControlR.Agent*' -ErrorAction SilentlyContinue
            if ($service) {
                @{
                    Installed   = $true
                    ServiceName = $service.Name
                    Status      = $service.Status.ToString()
                }
            }
            else {
                # Also check for the agent executable
                $agentPaths = @(
                    "$env:ProgramFiles\ControlR\ControlR.Agent.exe",
                    "$env:ProgramFiles (x86)\ControlR\ControlR.Agent.exe",
                    "/usr/local/bin/ControlR/ControlR.Agent"
                )
                $found = $agentPaths | Where-Object { Test-Path $_ } | Select-Object -First 1
                @{
                    Installed = [bool]$found
                    AgentPath = $found
                    Status    = if ($found) { 'FileExists' } else { 'NotInstalled' }
                }
            }
        }
        return $result
    }

    'test' {
        # Test if agent is installed and running
        $result = Invoke-ImmyCommand {
            $service = Get-Service -Name 'ControlR.Agent*' -ErrorAction SilentlyContinue
            if ($service -and $service.Status -eq 'Running') {
                return $true
            }
            return $false
        }
        return $result
    }

    'set' {
        # Ensure the device group exists for this tenant
        $group = Get-OrCreateDeviceGroup -GroupName $TenantName

        # Download and install on the target
        $serverUrl = $using:ControlRServerUrl
        $tenantId = $using:ControlRTenantId
        $keyId = $using:ControlRInstallerKeyId
        $keySecret = $using:ControlRInstallerKeySecret
        $instanceId = ([System.Uri]$serverUrl).Authority

        Invoke-ImmyCommand {
            $url = $using:serverUrl
            $tid = $using:tenantId
            $kid = $using:keyId
            $ks = $using:keySecret
            $iid = $using:instanceId

            $downloadUrl = "$url/downloads/win-x64/ControlR.Agent.exe"
            $tempPath = "$env:TEMP\ControlR.Agent.exe"

            Write-Host "Downloading ControlR agent from $downloadUrl..."
            [Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
            (New-Object Net.WebClient).DownloadFile($downloadUrl, $tempPath)

            if (-not (Test-Path $tempPath)) {
                throw "Failed to download ControlR agent"
            }

            $fileSize = (Get-Item $tempPath).Length
            Write-Host "Downloaded $([math]::Round($fileSize / 1MB, 1)) MB"

            $installArgs = "install -s $url -i $iid -t $tid -ks $ks -ki $kid"
            Write-Host "Installing ControlR agent..."
            $process = Start-Process -FilePath $tempPath -ArgumentList $installArgs -Wait -PassThru -NoNewWindow
            if ($process.ExitCode -ne 0) {
                throw "ControlR agent install exited with code $($process.ExitCode)"
            }

            Write-Host "ControlR agent installed successfully"
        }

        # After install, wait a moment for the device to register, then assign to group
        Start-Sleep -Seconds 15

        # Get the device name from the target to find it in ControlR
        $computerName = Invoke-ImmyCommand { $env:COMPUTERNAME }

        try {
            # Search for the device in ControlR
            $devices = Invoke-ControlRApi -Endpoint '/api/devices' -Method 'GET'
            $device = $devices | Where-Object { $_.name -eq $computerName } | Select-Object -First 1

            if ($device -and $group.id) {
                Write-Host "Assigning device '$computerName' to group '$TenantName'..."
                Invoke-ControlRApi -Endpoint "/api/device-groups/$($device.id)/group" -Method 'PUT' -Body $group.id
                Write-Host "Device assigned to group successfully"
            }
            elseif (-not $device) {
                Write-Host "Warning: Device '$computerName' not yet registered in ControlR. Group assignment will be retried on next run."
            }
        }
        catch {
            Write-Host "Warning: Could not assign device to group: $_"
            Write-Host "The device will appear ungrouped in ControlR. You can assign it manually or re-run this task."
        }

        return $true
    }
}
