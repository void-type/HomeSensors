using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HomeSensors.Model.Migrations;

/// <inheritdoc />
public partial class AddDeviceLatestIndex : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_TemperatureReading_TemperatureDeviceId",
            table: "TemperatureReading");

        migrationBuilder.CreateIndex(
            name: "IX_TemperatureReadings_DeviceLatest",
            table: "TemperatureReading",
            columns: new[] { "TemperatureDeviceId", "Time" })
            .Annotation("SqlServer:Include", new[] { "TemperatureCelsius", "Humidity", "DeviceBatteryLevel", "TemperatureLocationId" });
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_TemperatureReadings_DeviceLatest",
            table: "TemperatureReading");

        migrationBuilder.CreateIndex(
            name: "IX_TemperatureReading_TemperatureDeviceId",
            table: "TemperatureReading",
            column: "TemperatureDeviceId");
    }
}
