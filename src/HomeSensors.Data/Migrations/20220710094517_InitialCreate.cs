using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HomeSensors.Data.Migrations
{
    public partial class InitialCreate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TemperatureReadings",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Time = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    DeviceModel = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DeviceId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DeviceChannel = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DeviceBatteryLevel = table.Column<double>(type: "float", nullable: true),
                    DeviceStatus = table.Column<int>(type: "int", nullable: true),
                    MessageIntegrityCheck = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TemperatureCelsius = table.Column<double>(type: "float", nullable: true),
                    Humidity = table.Column<double>(type: "float", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TemperatureReadings", x => x.Id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TemperatureReadings");
        }
    }
}
