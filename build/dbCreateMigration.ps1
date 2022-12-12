[CmdletBinding()]
param (
  [Parameter(Mandatory = $true)]
  [string] $MigrationName
)

$originalLocation = Get-Location
$projectRoot = "$PSScriptRoot/../"

try {
  Set-Location -Path $projectRoot
  . ./build/buildSettings.ps1

  $settingsFile = "$serviceProjectFolder/appsettings.Development.json"

  if (-not (Test-Path -Path $settingsFile)) {
    throw "$settingsFile does not exist to get the Connection String from."
  }

  dotnet ef migrations add "$MigrationName" `
    --project "$modelProjectFolder" `
    --startup-project  "$serviceProjectFolder"

} finally {
  Set-Location $originalLocation
}