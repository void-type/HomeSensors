$originalLocation = Get-Location
$projectRoot = "$PSScriptRoot/../"

try {
  Set-Location -Path $projectRoot
  . ./build/buildSettings.ps1

  dotnet ef database update `
    --project "$modelProjectFolder" `
    --startup-project  "$serviceProjectFolder"

} finally {
  Set-Location $originalLocation
}
