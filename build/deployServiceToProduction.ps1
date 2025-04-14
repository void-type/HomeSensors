# Get-Service HomeSensors | Stop-Service; pause; Get-Service HomeSensors | Start-Service
# Run this script as a server administrator from the scripts directory
[CmdletBinding(SupportsShouldProcess = $true, ConfirmImpact = 'High')]
param()

Push-Location -Path "$PSScriptRoot/../"
. ./build/buildSettings.ps1

try {
  if (-not (Test-Path -Path $serviceReleaseFolder)) {
    throw 'No artifacts to deploy. Run build.ps1 before deploying.'
  }

  if ($PSCmdlet.ShouldProcess("$serviceDirectoryProduction", "Deploy $shortAppName.Service to Production.")) {
    if (-not (Test-Path -Path $webSettingsProduction)) {
      throw "No settings file found at $webSettingsProduction"
    }

    ROBOCOPY "$serviceReleaseFolder" "$serviceDirectoryProduction" /MIR
    Copy-Item -Path "$serviceSettingsProduction" -Destination $serviceDirectoryProduction
  }

} finally {
  Pop-Location
}

Write-Host
Write-Host 'Be sure to restart the service on the server to pickup the new binaries.'
