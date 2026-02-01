using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HomeSensors.Model.Migrations;

/// <inheritdoc />
public partial class IndexAuditFixes : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_TemperatureReading_IsSummary_TemperatureDeviceId_Time",
            table: "TemperatureReading");

        migrationBuilder.CreateIndex(
            name: "IX_TemperatureReading_IsSummary_TemperatureDeviceId_Time",
            table: "TemperatureReading",
            columns: new[] { "IsSummary", "TemperatureDeviceId", "Time" })
            .Annotation("SqlServer:Include", new[] { "Id", "TemperatureCelsius", "Humidity", "TemperatureLocationId" });

        migrationBuilder.CreateIndex(
            name: "IX_HvacAction_LastUpdated",
            table: "HvacAction",
            column: "LastUpdated")
            .Annotation("SqlServer:Include", new[] { "EntityId", "State", "LastChanged" });
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_TemperatureReading_IsSummary_TemperatureDeviceId_Time",
            table: "TemperatureReading");

        migrationBuilder.DropIndex(
            name: "IX_HvacAction_LastUpdated",
            table: "HvacAction");

        migrationBuilder.CreateIndex(
            name: "IX_TemperatureReading_IsSummary_TemperatureDeviceId_Time",
            table: "TemperatureReading",
            columns: new[] { "IsSummary", "TemperatureDeviceId", "Time" })
            .Annotation("SqlServer:Include", new[] { "TemperatureCelsius", "Humidity", "TemperatureLocationId" });
    }
}
