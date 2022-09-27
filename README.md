# HomeSensors

This is a solution to gather data from various sensors and display it.

HomeSensors.Service - This is a simple MQTT receiver that will persist messages to a database. Can be expanded to reliably ingest other data in the future.

HomeSensors.Web - A web app for data analysis and entry. Exposes a web API and SignalR endpoints for clients.

## Build tools

You can use the [.NET SDK](https://dot.net/download) and [Node.js LTS](https://nodejs.org/) to build and run the source.

## Database

This project uses Entity Framework Code First.

To create/update the database, set your connection string in `src\HomeSensors.Service\appsettings.Development.json` and run the following.

```PowerShell
dotnet ef database update --project ./src/HomeSensors.Data/HomeSensors.Data.csproj --startup-project  ./src/HomeSensors.Service/HomeSensors.Service.csproj
```

To add a migration after updating the database context or models, run the following.

```PowerShell
dotnet ef migrations add <migration-name> --project ./src/HomeSensors.Data/HomeSensors.Data.csproj --startup-project  ./src/HomeSensors.Service/HomeSensors.Service.csproj
```

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
