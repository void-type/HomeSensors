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
$webSettingsDirectoryProduction = "\\server2\Servers\AppConfigs\$projectName\Web"

$serviceDirectoryProduction = "\\server2\DeployedApps\apps\$projectName.Service"
$serviceSettingsDirectoryProduction = "\\server2\Servers\AppConfigs\$projectName\Service"
