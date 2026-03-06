<#
.SYNOPSIS
    ImmyBot detection script for ControlR Agent.
.DESCRIPTION
    Returns the installed version if the ControlR agent is present, or nothing if not.
    Uses $DetectionString (provided by ImmyBot) to match against uninstall registry entries.
#>

$uninstallPaths = @(
    'HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\*',
    'HKLM:\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\*'
)

$entries = Get-ItemProperty $uninstallPaths -ErrorAction SilentlyContinue |
    Where-Object { $_.DisplayName -like "*$DetectionString*" }

foreach ($entry in $entries) {
    if ($entry.DisplayVersion) {
        return $entry.DisplayVersion
    }
}

# Fallback: check service binary version
$service = Get-Service -Name 'ControlR.Agent*' -ErrorAction SilentlyContinue | Select-Object -First 1
if ($service) {
    $scOutput = sc.exe qc $service.Name 2>$null
    $binLine = $scOutput | Where-Object { $_ -match 'BINARY_PATH_NAME' }
    if ($binLine) {
        $exePath = (($binLine -replace '.*:\s*', '') -replace '"', '' -split '\s+')[0].Trim()
        if ($exePath -and (Test-Path $exePath)) {
            $v = (Get-Item $exePath).VersionInfo.FileVersion
            if ($v) { return $v }
        }
    }
    return '0.0.0'
}

return $null
