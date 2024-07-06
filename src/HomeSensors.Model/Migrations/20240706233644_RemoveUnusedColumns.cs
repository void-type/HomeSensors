using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HomeSensors.Model.Migrations;

/// <inheritdoc />
public partial class RemoveUnusedColumns : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "DeviceChannel",
            table: "TemperatureDevice");

        migrationBuilder.DropColumn(
            name: "DeviceId",
            table: "TemperatureDevice");

        migrationBuilder.DropColumn(
            name: "DeviceModel",
            table: "TemperatureDevice");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "DeviceChannel",
            table: "TemperatureDevice",
            type: "nvarchar(max)",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "DeviceId",
            table: "TemperatureDevice",
            type: "nvarchar(max)",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "DeviceModel",
            table: "TemperatureDevice",
            type: "nvarchar(max)",
            nullable: false,
            defaultValue: "");
    }
}
