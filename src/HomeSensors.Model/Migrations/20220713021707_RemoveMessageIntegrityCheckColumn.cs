using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HomeSensors.Model.Migrations;

public partial class RemoveMessageIntegrityCheckColumn : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "MessageIntegrityCheck",
            table: "TemperatureReadings");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "MessageIntegrityCheck",
            table: "TemperatureReadings",
            type: "nvarchar(max)",
            nullable: true);
    }
}
