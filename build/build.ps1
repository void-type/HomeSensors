[CmdletBinding()]
param(
  [string] $Configuration = 'Release',
  [switch] $SkipFormat,
  [switch] $SkipOutdated,
  [switch] $SkipClient,
  [switch] $SkipTest,
  [switch] $SkipTestReport,
  [switch] $SkipPublish
)

function Stop-OnError([string]$errorMessage) {
  if ($LASTEXITCODE -eq 0) {
    return
  }

  if (-not [string]::IsNullOrWhiteSpace($errorMessage)) {
    Write-Error $errorMessage
  }

  exit $LASTEXITCODE
}

$stopwatch = New-Object System.Diagnostics.Stopwatch
$stopwatch.Start()

$originalLocation = Get-Location
$projectRoot = "$PSScriptRoot/../"

try {
  Set-Location -Path $projectRoot
  . ./build/buildSettings.ps1

  # Clean the artifacts folders
  Remove-Item -Path './artifacts' -Recurse -ErrorAction SilentlyContinue

  # Restore local dotnet tools
  dotnet tool restore
  Stop-OnError

  # Lint and build client
  if (-not $SkipClient) {
    Set-Location -Path $webClientProjectFolder
    npm install --no-audit
    Stop-OnError

    if (-not $SkipFormat) {
      npm run lint
      Stop-OnError
      npm run format
      Stop-OnError 'Formatting the client failed.'
    }

    if (-not $SkipOutdated) {
      npm outdated
      npm audit --omit=dev
      Stop-OnError
    }

    npm run build
    Stop-OnError
  }

  # Build solution
  Set-Location -Path $projectRoot

  if (-not $SkipFormat) {
    $formatOutput = dotnet format --verify-no-changes
    Write-Host $formatOutput

    if ($LASTEXITCODE -ne 0) {
      # Don't stop for obsolete warnings
      $formatOutput = $formatOutput -replace '.*S1133.*', ''
      # Don't stop for  TODO warnings
      $formatOutput = $formatOutput -replace '.*S1135.*', ''

      if (-not [string]::IsNullOrWhiteSpace($formatOutput)) {
        Write-Host "Formatting errors found. Please run 'dotnet format' to fix them."
        exit 1
      }
    }
  }

  dotnet restore
  Stop-OnError

  if (-not $SkipOutdated) {
    dotnet outdated
    dotnet list package --vulnerable --include-transitive
    Stop-OnError
  }

  dotnet build --configuration "$Configuration" --no-restore
  Stop-OnError

  if (-not $SkipTest) {
    # Run tests, gather coverage
    dotnet test "$testProjectFolder" `
      --configuration "$Configuration" `
      --no-build `
      --results-directory './artifacts/testResults' `
      --logger 'trx' `
      --collect:'XPlat Code Coverage'
    Stop-OnError

    if (-not $SkipTestReport) {
      # Generate code coverage report
      dotnet reportgenerator `
        '-reports:./artifacts/testResults/*/coverage.cobertura.xml' `
        '-targetdir:./artifacts/testCoverage' `
        '-reporttypes:HtmlInline_AzurePipelines' `
        '-filefilters:-*.g.cs'
      Stop-OnError
    }
  }

  if (-not $SkipPublish) {
    # Package build
    dotnet publish "$serviceProjectFolder" --configuration "$Configuration" --no-build --output $serviceReleaseFolder
    Stop-OnError

    dotnet publish "$webProjectFolder" --configuration "$Configuration" --no-build --output $webReleaseFolder
    Stop-OnError
  }

  $projectVersion = (dotnet nbgv get-version -f json | ConvertFrom-Json).NuGetPackageVersion

  $stopwatch.Stop()

  Write-Output "`nBuilt $projectName $projectVersion in $($stopwatch.Elapsed)`n"

} finally {
  Set-Location $originalLocation
}
