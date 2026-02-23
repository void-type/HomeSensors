using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HomeSensors.Model.Migrations;

/// <inheritdoc />
public partial class AddWaterLeakDeviceInactiveLimitMinutes : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<int>(
            name: "InactiveLimitMinutes",
            table: "WaterLeakDevice",
            type: "int",
            nullable: false,
            defaultValue: 90);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "InactiveLimitMinutes",
            table: "WaterLeakDevice");
    }
}
