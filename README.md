# HomeSensors

A solution to gather data from various sensors and display it. Built using ASP.NET Core 7 and Vue 3.

## Features

HomeSensors.Service - A service for performing scheduled or continuous actions.

* Gathers temperature data from an external MQTT feed using MQTTnet. There are scripts to setup an SDR, RTL_433 and Mosquitto on a Raspberry PI.
* Performs temperature limit checks per-location. Will notify via email when limit is exceeded.
* Compresses historical data (> 30 days ago) into 5 minute intervals. This can save significant space as some of these sensors can poll every few seconds.

HomeSensors.Web - A web app for working with the data.

* Shows live current readings on a dashboard via SignalR.
* Time series line graph with selectable date range and locations using Chart.js.
* Time series also populates a table showing min, max and average temps of each location queried.
* API data is cached using LazyCache.
* Web API is automatically documented using Swashbuckle and Swagger.

ClientApp - A browser frontend for HomeSensors.Web.
* Uses swagger-typescript-api to generate an API client from the swagger endpoint.
* Settings under the user name for temperature unit, show/hide humidity, and dark theme.
* Responsive UI made with Bootstrap grid.
* See screenshots [here](docs/screenshots.md).

## Build tools

You can use the [.NET SDK](https://dot.net/download) and [Node.js LTS](https://nodejs.org/) to build and run the source.

## Database

This project uses Entity Framework Code First.

Use the `dbApplyMigration.ps1` script to create or update the database.

Use the `dbCreateMigration.ps1` script to create a migration after you make changes to the DbContext or models.

## Deploy initial

Run the rtl_433.sh script by hand, section-by-section to setup RTL_433 as a service and an MQTT server. This can be on a remote device. I'm using a Raspberry Pi with Nooelec NESDR Mini 2+.

Run ./build/build.ps1.

Run the web and service deploy scripts from the code repo to get the code and settings on an app server with .NET, SQL Server and IIS. For the deployment scripts to work, you'll need the file shares for the deployed files and settings. I use Windows File Shares. You can adjust these paths in the buildSettings.ps1 script.

Use the database migration section above to deploy a new database.

Run the registerService script on the server to get the service going to read the MQTT feed.

Make an IIS website pointing to the web app folder.

## Deploy updates

Run ./build/build.ps1

Use the database migration section above to deploy any migrations needed. Be sure to run these in a test environment in case there's data loss!

Run the following  on the service server as an administrator. This will stop the service and pause so you can deploy, then it will restart the service.

```PowerShell
Get-Service HomeSensors | Stop-Service; pause; Get-Service HomeSensors | Start-Service
```

Run the web and service deploy scripts from the code repo to get the code and settings on the app server.

Hit enter on your "Get-Service" prompt to start the service. The web app should come online after the deployment.
