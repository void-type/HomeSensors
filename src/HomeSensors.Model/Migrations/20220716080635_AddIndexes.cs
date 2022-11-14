using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HomeSensors.Model.Migrations;

public partial class AddIndexes : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AlterColumn<string>(
            name: "Name",
            table: "TemperatureLocations",
            type: "nvarchar(450)",
            nullable: false,
            oldClrType: typeof(string),
            oldType: "nvarchar(max)");

        migrationBuilder.CreateIndex(
            name: "IX_TemperatureReadings_Time",
            table: "TemperatureReadings",
            column: "Time");

        migrationBuilder.CreateIndex(
            name: "IX_TemperatureLocations_Name",
            table: "TemperatureLocations",
            column: "Name",
            unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_TemperatureReadings_Time",
            table: "TemperatureReadings");

        migrationBuilder.DropIndex(
            name: "IX_TemperatureLocations_Name",
            table: "TemperatureLocations");

        migrationBuilder.AlterColumn<string>(
            name: "Name",
            table: "TemperatureLocations",
            type: "nvarchar(max)",
            nullable: false,
            oldClrType: typeof(string),
            oldType: "nvarchar(450)");
    }
}
