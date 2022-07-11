$shortAppName = "HomeSensors"
$projectName = "$shortAppName"

$serviceProjectFolder = "./src/$projectName"

$serviceDirectoryProduction = "\\server2\DeployedApps\apps\$projectName"
$settingsDirectoryProduction = "\\server2\Servers\AppConfigs\$projectName"

$serviceDirectoryTest = "\\server2\DeployedApps\apps\$($projectName)-Test"
$settingsDirectoryTest = "$settingsDirectoryProduction"
