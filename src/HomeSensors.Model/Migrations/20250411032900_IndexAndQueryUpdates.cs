using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HomeSensors.Model.Migrations;

/// <inheritdoc />
public partial class IndexAndQueryUpdates : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_TemperatureReading_IsSummary",
            table: "TemperatureReading");

        migrationBuilder.DropIndex(
            name: "IX_TemperatureReading_TemperatureLocationId",
            table: "TemperatureReading");

        migrationBuilder.DropIndex(
            name: "IX_TemperatureReading_Time",
            table: "TemperatureReading");

        migrationBuilder.DropIndex(
            name: "IX_TemperatureReading_Time_TemperatureDeviceId_IsSummary",
            table: "TemperatureReading");

        migrationBuilder.DropIndex(
            name: "IX_TemperatureReading_Time_TemperatureLocationId",
            table: "TemperatureReading");

        migrationBuilder.CreateIndex(
            name: "IX_TemperatureReading_IsSummary_TemperatureDeviceId_Time",
            table: "TemperatureReading",
            columns: new[] { "IsSummary", "TemperatureDeviceId", "Time" })
            .Annotation("SqlServer:Include", new[] { "TemperatureCelsius", "Humidity", "TemperatureLocationId" });

        migrationBuilder.CreateIndex(
            name: "IX_TemperatureReading_TemperatureLocationId_Time",
            table: "TemperatureReading",
            columns: new[] { "TemperatureLocationId", "Time" })
            .Annotation("SqlServer:Include", new[] { "TemperatureCelsius", "Humidity" });

        migrationBuilder.CreateIndex(
            name: "IX_TemperatureReading_Time",
            table: "TemperatureReading",
            column: "Time",
            filter: "[IsSummary] = 0")
            .Annotation("SqlServer:Include", new[] { "TemperatureLocationId", "TemperatureCelsius", "Humidity" });

        migrationBuilder.CreateIndex(
            name: "IX_Category_Order",
            table: "Category",
            column: "Order");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_TemperatureReading_IsSummary_TemperatureDeviceId_Time",
            table: "TemperatureReading");

        migrationBuilder.DropIndex(
            name: "IX_TemperatureReading_TemperatureLocationId_Time",
            table: "TemperatureReading");

        migrationBuilder.DropIndex(
            name: "IX_TemperatureReading_Time",
            table: "TemperatureReading");

        migrationBuilder.DropIndex(
            name: "IX_Category_Order",
            table: "Category");

        migrationBuilder.CreateIndex(
            name: "IX_TemperatureReading_IsSummary",
            table: "TemperatureReading",
            column: "IsSummary");

        migrationBuilder.CreateIndex(
            name: "IX_TemperatureReading_TemperatureLocationId",
            table: "TemperatureReading",
            column: "TemperatureLocationId");

        migrationBuilder.CreateIndex(
            name: "IX_TemperatureReading_Time",
            table: "TemperatureReading",
            column: "Time");

        migrationBuilder.CreateIndex(
            name: "IX_TemperatureReading_Time_TemperatureDeviceId_IsSummary",
            table: "TemperatureReading",
            columns: new[] { "Time", "TemperatureDeviceId", "IsSummary" });

        migrationBuilder.CreateIndex(
            name: "IX_TemperatureReading_Time_TemperatureLocationId",
            table: "TemperatureReading",
            columns: new[] { "Time", "TemperatureLocationId" });
    }
}
