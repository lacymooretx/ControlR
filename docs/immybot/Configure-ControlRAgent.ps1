<#
.SYNOPSIS
    ImmyBot configuration task for ControlR Agent device group assignment.
.DESCRIPTION
    Ensures the device is assigned to a ControlR device group matching the
    ImmyBot tenant name. Creates the group if it doesn't exist.

.PARAMETER Method
    ImmyBot auto-provides: get, test, or set.
.PARAMETER TenantName
    ImmyBot auto-provides the tenant (client) name.
.PARAMETER ControlRServerUrl
    The ControlR server URL.
.PARAMETER ControlRPersonalAccessToken
    A PAT from a TenantAdministrator user in ControlR.
#>
param(
    [Parameter(Mandatory)]
    [string]$Method,

    [Parameter(Mandatory)]
    [string]$TenantName,

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

function Find-DeviceInControlR {
    $computerName = $env:COMPUTERNAME
    $devices = Invoke-ControlRApi -Endpoint '/api/devices'
    $devices | Where-Object { $_.name -eq $computerName } | Select-Object -First 1
}

function Find-GroupByName {
    param([string]$Name)
    $groups = Invoke-ControlRApi -Endpoint '/api/device-groups'
    $groups | Where-Object { $_.name -eq $Name } | Select-Object -First 1
}

function New-GroupByName {
    param([string]$Name)
    $body = @{
        name        = $Name
        description = "Auto-created from ImmyBot tenant: $Name"
        groupType   = 0
        sortOrder   = 0
    } | ConvertTo-Json
    Invoke-ControlRApi -Endpoint '/api/device-groups' -HttpMethod 'POST' -Body $body
}

switch ($Method) {
    'get' {
        $device = Find-DeviceInControlR
        if (-not $device) {
            return @{ Status = 'DeviceNotRegistered'; ComputerName = $env:COMPUTERNAME }
        }
        $group = Find-GroupByName -Name $TenantName
        return @{
            DeviceId       = $device.id
            DeviceName     = $device.name
            CurrentGroupId = $device.deviceGroupId
            TargetGroupId  = if ($group) { $group.id } else { $null }
            TargetGroup    = $TenantName
            InCorrectGroup = ($group -and $device.deviceGroupId -eq $group.id)
        }
    }

    'test' {
        $device = Find-DeviceInControlR
        if (-not $device) {
            Write-Host "Device '$env:COMPUTERNAME' not registered in ControlR yet."
            return $true
        }
        $group = Find-GroupByName -Name $TenantName
        if (-not $group) { return $false }
        if ($device.deviceGroupId -eq $group.id) {
            Write-Host "Device is in correct group '$TenantName'."
            return $true
        }
        Write-Host "Device not in group '$TenantName'."
        return $false
    }

    'set' {
        $device = Find-DeviceInControlR
        if (-not $device) {
            Write-Host "Device '$env:COMPUTERNAME' not registered yet. Will assign on next run."
            return
        }

        $group = Find-GroupByName -Name $TenantName
        if (-not $group) {
            Write-Host "Creating group '$TenantName'..."
            $group = New-GroupByName -Name $TenantName
            Write-Host "Created group (ID: $($group.id))"
        }

        if ($device.deviceGroupId -eq $group.id) {
            Write-Host 'Already in correct group.'
            return
        }

        Write-Host "Assigning '$($device.name)' to group '$TenantName'..."
        Invoke-ControlRApi -Endpoint "/api/device-groups/$($device.id)/group" -HttpMethod 'PUT' -Body "`"$($group.id)`""
        Write-Host 'Done.'
    }
}
