using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HomeSensors.Model.Migrations;

/// <inheritdoc />
public partial class AddIndexes2 : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateIndex(
            name: "IX_TemperatureReadings_Time_TemperatureDeviceId_IsSummary",
            table: "TemperatureReadings",
            columns: new[] { "Time", "TemperatureDeviceId", "IsSummary" });

        migrationBuilder.CreateIndex(
            name: "IX_TemperatureReadings_Time_TemperatureLocationId",
            table: "TemperatureReadings",
            columns: new[] { "Time", "TemperatureLocationId" });
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_TemperatureReadings_Time_TemperatureDeviceId_IsSummary",
            table: "TemperatureReadings");

        migrationBuilder.DropIndex(
            name: "IX_TemperatureReadings_Time_TemperatureLocationId",
            table: "TemperatureReadings");
    }
}
