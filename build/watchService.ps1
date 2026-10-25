$originalLocation = Get-Location
$projectRoot = "$PSScriptRoot/../"

try {
  Set-Location -Path $projectRoot
  . ./build/buildSettings.ps1

  Set-Location $serviceProjectFolder

  # Work around dotnet watch crashing on NBGV's shared PrivateP2PCaching.proj (dotnet/sdk#54552).
  $env:NBGV_CacheMode = 'None'

  dotnet watch

} finally {
  Set-Location $originalLocation
}
