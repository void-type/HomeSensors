$shortAppName = "HomeSensors"
$projectName = "$shortAppName"

$webProjectFolder = "./src/$projectName.Web"
$modelProjectFolder = "./src/$projectName.Data"
$webClientProjectFolder = "./src/ClientApp"
$iisDirectoryProduction = "\\server2\DeployedApps\apps\$projectName.Web"
$webSettingsDirectoryProduction = "\\server2\Servers\AppConfigs\$projectName\Web"
$webReleaseFolder  = './artifacts/dist/release/web'

$serviceProjectFolder = "./src/$projectName.Service"
$serviceDirectoryProduction = "\\server2\DeployedApps\apps\$projectName.Service"
$serviceSettingsDirectoryProduction = "\\server2\Servers\AppConfigs\$projectName\Service"
$serviceReleaseFolder = './artifacts/dist/release/service'
