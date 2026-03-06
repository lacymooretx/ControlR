<#
.SYNOPSIS
    ImmyBot post-install script for ControlR Agent device group assignment.
.DESCRIPTION
    Assigns the device to a ControlR device group matching the tenant name.
    Creates the group if it doesn't exist. Gets the tenant name from the API.

.PARAMETER ControlRServerUrl
    The ControlR server URL.
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

# Get tenant name from the API
$me = Invoke-ControlRApi -Endpoint '/api/me'
$TenantName = $me.tenantName
if (-not $TenantName) {
    Write-Host 'Warning: Tenant name not set in ControlR. Skipping group assignment.'
    return
}

# Find device
$computerName = $env:COMPUTERNAME
$devices = Invoke-ControlRApi -Endpoint '/api/devices'
$device = $devices | Where-Object { $_.name -eq $computerName } | Select-Object -First 1

if (-not $device) {
    Write-Host "Device '$computerName' not registered yet. Will assign on next run."
    return
}

# Find or create group
$groups = Invoke-ControlRApi -Endpoint '/api/device-groups'
$group = $groups | Where-Object { $_.name -eq $TenantName } | Select-Object -First 1

if (-not $group) {
    Write-Host "Creating group '$TenantName'..."
    $body = @{
        name        = $TenantName
        description = "Auto-created from ControlR tenant: $TenantName"
        groupType   = 0
        sortOrder   = 0
    } | ConvertTo-Json
    $group = Invoke-ControlRApi -Endpoint '/api/device-groups' -HttpMethod 'POST' -Body $body
    Write-Host "Created group (ID: $($group.id))"
}

# Assign device to group
if ($device.deviceGroupId -eq $group.id) {
    Write-Host "Device already in correct group '$TenantName'."
    return
}

Write-Host "Assigning '$($device.name)' to group '$TenantName'..."
Invoke-ControlRApi -Endpoint "/api/device-groups/$($device.id)/group" -HttpMethod 'PUT' -Body "`"$($group.id)`""
Write-Host 'Done.'
