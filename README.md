# HomeSensors

This is a simple MQTT receiver that will persist messages to a database.

## Build and Install

You can use the [.NET SDK](https://dot.net/download) to build and run the source.

## Database

This project uses EF Code First.

To create/update the database, set your connection string in `src\HomeSensors.Service\appsettings.Development.json`.

```PowerShell
cd src/HomeSensors.Service
dotnet ef database update --project ./src/HomeSensors.Data/HomeSensors.Data.csproj --startup-project  ./src/HomeSensors.Service/HomeSensors.Service.csproj
```

To add a migration

```PowerShell
dotnet ef migrations add <migration-name> --project ./src/HomeSensors.Data/HomeSensors.Data.csproj --startup-project  ./src/HomeSensors.Service/HomeSensors.Service.csproj
```

## Usage

Best installed as a service.
