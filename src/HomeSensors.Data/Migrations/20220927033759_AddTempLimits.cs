using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HomeSensors.Data.Migrations
{
    public partial class AddTempLimits : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "MaxTemperatureLimit",
                table: "TemperatureLocations",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "MinTemperatureLimit",
                table: "TemperatureLocations",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsRetired",
                table: "TemperatureDevices",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MaxTemperatureLimit",
                table: "TemperatureLocations");

            migrationBuilder.DropColumn(
                name: "MinTemperatureLimit",
                table: "TemperatureLocations");

            migrationBuilder.DropColumn(
                name: "IsRetired",
                table: "TemperatureDevices");
        }
    }
}
