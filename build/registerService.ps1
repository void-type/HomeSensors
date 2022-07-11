# Run locally on server to register a service after first deployment.

# Use configOverrides to pass configuration secrets that override appsettings.json and environment variables.
# Use colon for a delimiter, not double underscore.
# $configOverrides = "--ConnectionStrings:MyConnString `"Server=...`""

# Production
$serviceName = "HomeSensors"
$serviceDisplayName = 'HomeSensors'
$serviceDescription = 'Listens for data on an MQTT server and stores it.'
$configOverrides = ''
$environmentName = 'Producton'

# Test
# $serviceName = "HomeSensorsTest"
# $serviceDisplayName = 'HomeSensors Test'
# $serviceDescription = 'Listens for data on an MQTT server and stores it.'
# $configOverrides = ''
# $environmentName = 'Test'

$serviceDirectory = "G:\DeployedApps\apps\$serviceName"

# # We could run this job as a domain service account if we need to access network resources like shares.
# $serviceAccount = 'some_domain_account$'

# # Allow the Service account to read the service binaries. Not needed if service runs as localsystem.
# $acl = Get-Acl $serviceDirectory
# $aclRuleArgs = "System", "Read,Write,ReadAndExecute", "ContainerInherit,ObjectInherit", "None", "Allow"
# $accessRule = New-Object System.Security.AccessControl.FileSystemAccessRule($aclRuleArgs)
# $acl.SetAccessRule($accessRule)
# $acl | Set-Acl $serviceDirectory

# Create the service to run as LocalSystem if no credential supplied.
(Get-WmiObject -Class Win32_Service -Filter "Name='$serviceName'").Delete()

$serviceParams = @{
  'Name'           = $serviceName
  'DisplayName'    = $serviceDisplayName
  'BinaryPathName' = "`"$serviceDirectory/HomeSensors.exe`" --environment $environmentName $configOverrides"
  'StartupType'    = 'Automatic'
  'Description'    = $serviceDescription
}

New-Service @serviceParams

# Set the job to run with a service account, default is LocalSystem
if ($null -ne $serviceAccount) {
  (Get-WmiObject -Class Win32_Service -Filter "Name='$serviceName'").Change($null, $null, $null, $null, $null, $null, "$env:USERDOMAIN\$serviceAccount", $null, $null, $null, $null)
}

# Start the service
Start-Service -Name $serviceName
