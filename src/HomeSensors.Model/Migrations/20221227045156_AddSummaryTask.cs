using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HomeSensors.Model.Migrations;

/// <inheritdoc />
public partial class AddSummaryTask : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.RenameColumn(
            name: "MinTemperatureLimit",
            table: "TemperatureLocations",
            newName: "MinTemperatureLimitCelsius");

        migrationBuilder.RenameColumn(
            name: "MaxTemperatureLimit",
            table: "TemperatureLocations",
            newName: "MaxTemperatureLimitCelsius");

        migrationBuilder.AddColumn<bool>(
            name: "IsSummary",
            table: "TemperatureReadings",
            type: "bit",
            nullable: false,
            defaultValue: false);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "IsSummary",
            table: "TemperatureReadings");

        migrationBuilder.RenameColumn(
            name: "MinTemperatureLimitCelsius",
            table: "TemperatureLocations",
            newName: "MinTemperatureLimit");

        migrationBuilder.RenameColumn(
            name: "MaxTemperatureLimitCelsius",
            table: "TemperatureLocations",
            newName: "MaxTemperatureLimit");
    }
}
