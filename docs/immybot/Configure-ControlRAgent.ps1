<#
.SYNOPSIS
    ImmyBot post-install configuration script for ControlR Agent.
.DESCRIPTION
    Assigns the device to a ControlR device group matching the ImmyBot tenant name.
    Creates the group if it doesn't exist. This script calls the ControlR API
    from the target machine using a Personal Access Token.

    This is a Configuration Task (get/test/set) that ensures the device
    is in the correct group.

.PARAMETER Method
    ImmyBot auto-provides: get, test, or set.
.PARAMETER ControlRServerUrl
    The ControlR server URL.
.PARAMETER ControlRPersonalAccessToken
    A PAT from a TenantAdministrator user.
.PARAMETER TenantName
    ImmyBot auto-provides the tenant name. Used as the device group name.
#>
param(
    [Parameter(Mandatory)]
    [string]$Method,

    [Parameter(Mandatory)]
    [string]$ControlRServerUrl,

    [Parameter(Mandatory)]
    [string]$ControlRPersonalAccessToken,

    [Parameter(Mandatory)]
    [string]$TenantName
)

$ErrorActionPreference = 'Stop'
$ControlRServerUrl = $ControlRServerUrl.TrimEnd('/')

function Invoke-ControlRApi {
    param(
        [string]$Endpoint,
        [string]$HttpMethod = 'GET',
        [object]$Body = $null
    )

    $headers = @{
        'x-personal-token' = $ControlRPersonalAccessToken
        'Content-Type'     = 'application/json'
        'Accept'           = 'application/json'
    }

    $params = @{
        Uri     = "$ControlRServerUrl$Endpoint"
        Method  = $HttpMethod
        Headers = $headers
        UseBasicParsing = $true
    }

    if ($Body) {
        $params['Body'] = ($Body | ConvertTo-Json -Depth 10)
    }

    Invoke-RestMethod @params
}

function Get-DeviceGroupForTenant {
    param([string]$GroupName)

    $groups = Invoke-ControlRApi -Endpoint '/api/device-groups'
    return $groups | Where-Object { $_.name -eq $GroupName } | Select-Object -First 1
}

function New-DeviceGroupForTenant {
    param([string]$GroupName)

    return Invoke-ControlRApi -Endpoint '/api/device-groups' -HttpMethod 'POST' -Body @{
        name        = $GroupName
        description = "Auto-created from ImmyBot tenant: $GroupName"
        groupType   = 0
        sortOrder   = 0
    }
}

function Get-ControlRDevice {
    $computerName = $env:COMPUTERNAME
    $devices = Invoke-ControlRApi -Endpoint '/api/devices'

    # Match by computer name
    return $devices | Where-Object { $_.name -eq $computerName } | Select-Object -First 1
}

switch ($Method) {
    'get' {
        $device = Get-ControlRDevice
        if (-not $device) {
            return @{ Status = 'DeviceNotRegistered' }
        }

        $group = Get-DeviceGroupForTenant -GroupName $TenantName

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
        $device = Get-ControlRDevice
        if (-not $device) {
            # Device not registered yet -- nothing to configure
            Write-Host "Device '$env:COMPUTERNAME' not found in ControlR. Skipping group check."
            return $true
        }

        $group = Get-DeviceGroupForTenant -GroupName $TenantName
        if (-not $group) {
            # Group doesn't exist yet -- needs creation
            return $false
        }

        if ($device.deviceGroupId -eq $group.id) {
            Write-Host "Device is in correct group '$TenantName'."
            return $true
        }

        Write-Host "Device is not in group '$TenantName'. Current group ID: $($device.deviceGroupId)"
        return $false
    }

    'set' {
        $device = Get-ControlRDevice
        if (-not $device) {
            Write-Host "Device '$env:COMPUTERNAME' not registered in ControlR yet. Group assignment will happen on next run."
            return
        }

        # Find or create the group
        $group = Get-DeviceGroupForTenant -GroupName $TenantName
        if (-not $group) {
            Write-Host "Creating device group '$TenantName'..."
            $group = New-DeviceGroupForTenant -GroupName $TenantName
            Write-Host "Created group '$TenantName' (ID: $($group.id))"
        }

        if ($device.deviceGroupId -eq $group.id) {
            Write-Host "Device already in correct group."
            return
        }

        # Assign device to group
        Write-Host "Assigning device '$($device.name)' to group '$TenantName'..."
        Invoke-ControlRApi -Endpoint "/api/device-groups/$($device.id)/group" -HttpMethod 'PUT' -Body "`"$($group.id)`""
        Write-Host "Device assigned to group '$TenantName' successfully."
    }
}
