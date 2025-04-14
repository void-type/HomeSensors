$shortAppName = 'HomeSensors'
$projectName = "$shortAppName"

$webClientProjectFolder = './src/ClientApp'
$testProjectFolder = "./tests/$projectName.Test"
$modelProjectFolder = "./src/$projectName.Model"
$webProjectFolder = "./src/$projectName.Web"
$serviceProjectFolder = "./src/$projectName.Service"

$webReleaseFolder = './artifacts/dist/release/web'
$serviceReleaseFolder = './artifacts/dist/release/service'

$webDirectoryProduction = "\\server2\DeployedApps\apps\$projectName.Web"
$webSettingsProduction = "\\server2\Servers\AppConfigs\$projectName\Web\appsettings.Production.json"

$serviceDirectoryProduction = "\\server2\DeployedApps\apps\$projectName.Service"
$serviceSettingsProduction = "\\server2\Servers\AppConfigs\$projectName\Service\appsettings.Production.json"

$dbMigrationArgs = @(
  '--project', (Resolve-Path "$PSScriptRoot/../$modelProjectFolder" | Select-Object -ExpandProperty Path),
  '--startup-project', (Resolve-Path "$PSScriptRoot/../$webProjectFolder" | Select-Object -ExpandProperty Path)
)
