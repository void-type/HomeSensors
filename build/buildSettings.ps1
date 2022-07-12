$shortAppName = "HomeSensors"
$projectName = "$shortAppName"

$serviceProjectFolder = "./src/$projectName.Service"

$serviceDirectoryProduction = "\\server2\DeployedApps\apps\$projectName.Service"
$settingsDirectoryProduction = "\\server2\Servers\AppConfigs\$projectName"

$serviceDirectoryTest = "\\server2\DeployedApps\apps\$($projectName).Service-Test"
$settingsDirectoryTest = "$settingsDirectoryProduction"
