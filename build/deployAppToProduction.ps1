# Run this script as a server administrator from the scripts directory
[CmdletBinding(SupportsShouldProcess = $true, ConfirmImpact = "High")]
param()

Push-Location -Path "$PSScriptRoot/../"
. ./build/buildSettings.ps1

$releaseFolder = './artifacts/dist/release'

try {
  if (-not (Test-Path -Path $releaseFolder)) {
    throw 'No artifacts to deploy. Run build.ps1 before deploying.'
  }

  if ($PSCmdlet.ShouldProcess("$serviceDirectoryProduction", "Deploy $shortAppName to Production.")) {
    ROBOCOPY "$releaseFolder" "$serviceDirectoryProduction" /MIR
    Copy-Item -Path "$settingsDirectoryProduction\*" -Include "*.Production.json" -Recurse -Destination $serviceDirectoryProduction
  }

} finally {
  Pop-Location
}

Write-Host
Write-Host 'Be sure to restart the service on the server to pickup the new binaries.'
