<#
.SYNOPSIS
    ImmyBot detection script for ControlR Agent.
.DESCRIPTION
    Returns the installed version if the ControlR agent is installed, or nothing if not.
    ImmyBot uses this to determine if installation/update is needed.
#>

# Check the uninstall registry key first (most reliable)
$uninstallPaths = @(
    'HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\ControlR',
    'HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\ControlR (*)'
)

foreach ($path in $uninstallPaths) {
    $keys = Get-Item -Path $path -ErrorAction SilentlyContinue
    if ($keys) {
        foreach ($key in @($keys)) {
            $version = $key.GetValue('DisplayVersion')
            if ($version) {
                return $version
            }
        }
    }
}

# Fallback: check the service and binary directly
$service = Get-Service -Name 'ControlR.Agent*' -ErrorAction SilentlyContinue | Select-Object -First 1
if ($service) {
    # Try to get version from the binary
    $scOutput = sc.exe qc $service.Name 2>$null
    $binPath = ($scOutput | Where-Object { $_ -match 'BINARY_PATH_NAME' }) -replace '.*:\s*', '' -replace '"', ''
    $exePath = ($binPath -split '\s+' | Select-Object -First 1).Trim()

    if ($exePath -and (Test-Path $exePath)) {
        $fileVersion = (Get-Item $exePath).VersionInfo.FileVersion
        if ($fileVersion) {
            return $fileVersion
        }
    }

    # Service exists but can't determine version
    return '0.0.0'
}

# Not installed
return $null
