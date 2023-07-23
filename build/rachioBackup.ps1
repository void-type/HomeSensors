# https://rachio.readme.io/reference/getting-started
# Note that rate limit is 1,700 calls per day, or just over one per minute.

[CmdletBinding()]
param (
  [Parameter(Mandatory = $true)]
  [string]$ApiKey
)

$headers = @{
  'Authorization' = "Bearer $ApiKey"
  'Accept' = 'application/json'
}

$response = Invoke-RestMethod -Uri 'https://api.rach.io/1/public/person/info' -Method GET -Headers $headers
[string]$personId = $response.id

$response = Invoke-RestMethod -Uri "https://api.rach.io/1/public/person/$personId" -Method GET -Headers $headers
$devices = $response.devices
$devices | ConvertTo-Json -Depth 100 | Out-File "./rachio-devices.$personId.json"

# Get the past events of a device (can only do one month at a time)
$allEvents = @()

foreach ($device in $devices) {
  $endTime = [DateTimeOffset]::Now
  $startTime = $endTime.AddMonths(-1)

  while ($startTime.ToUnixTimeMilliseconds() -ge $device.createDate) {
    $response = Invoke-RestMethod -Uri "https://api.rach.io/1/public/device/$firstDeviceId/event?startTime=$($startTime.ToUnixTimeMilliseconds())&endTime=$($endTime.ToUnixTimeMilliseconds())" -Method GET -Headers $headers
    $allEvents += $response

    Write-Host "Found $($response.Count) events for the month of $($startTime)"

    $endTime = $startTime.AddMilliseconds(-1)
    $startTime = $endTime.AddMonths(-1)
  }
}

$allEvents | ConvertTo-Json -Depth 100 | Out-File "./rachio-events.$personId.json"
