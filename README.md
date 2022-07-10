# Rtl_433.Mqtt

This is a simple MQTT receiver that will persist messages to a database.

## Build and Install

You can use the [.NET SDK](https://dot.net/download) to build and run the source.

## Database

Set your connection string in appsettings, make sure you're an admin or can create DBs, then run `dotnet ef database update` to create the DB.

When you need to make a new migration `dotnet ef migrations add DeviceNormalization`.

## Usage

Best installed as a service.
