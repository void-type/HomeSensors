using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HomeSensors.Model.Migrations;

/// <inheritdoc />
public partial class RenameCurrentTemperatureLocation : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_TemperatureDevices_TemperatureLocations_CurrentTemperatureLocationId",
            table: "TemperatureDevices");

        migrationBuilder.RenameColumn(
            name: "CurrentTemperatureLocationId",
            table: "TemperatureDevices",
            newName: "TemperatureLocationId");

        migrationBuilder.RenameIndex(
            name: "IX_TemperatureDevices_CurrentTemperatureLocationId",
            table: "TemperatureDevices",
            newName: "IX_TemperatureDevices_TemperatureLocationId");

        migrationBuilder.AddForeignKey(
            name: "FK_TemperatureDevices_TemperatureLocations_TemperatureLocationId",
            table: "TemperatureDevices",
            column: "TemperatureLocationId",
            principalTable: "TemperatureLocations",
            principalColumn: "Id");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_TemperatureDevices_TemperatureLocations_TemperatureLocationId",
            table: "TemperatureDevices");

        migrationBuilder.RenameColumn(
            name: "TemperatureLocationId",
            table: "TemperatureDevices",
            newName: "CurrentTemperatureLocationId");

        migrationBuilder.RenameIndex(
            name: "IX_TemperatureDevices_TemperatureLocationId",
            table: "TemperatureDevices",
            newName: "IX_TemperatureDevices_CurrentTemperatureLocationId");

        migrationBuilder.AddForeignKey(
            name: "FK_TemperatureDevices_TemperatureLocations_CurrentTemperatureLocationId",
            table: "TemperatureDevices",
            column: "CurrentTemperatureLocationId",
            principalTable: "TemperatureLocations",
            principalColumn: "Id");
    }
}
