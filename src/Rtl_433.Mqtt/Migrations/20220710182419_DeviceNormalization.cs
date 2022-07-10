using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Rtl_433.Mqtt.Migrations
{
    public partial class DeviceNormalization : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "TemperatureDeviceId",
                table: "TemperatureReadings",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.CreateTable(
                name: "TemperatureDevices",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DeviceModel = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DeviceId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DeviceChannel = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TemperatureDevices", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TemperatureLocation",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TemperatureLocation", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TemperatureDeviceLocation",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StartTime = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    TemperatureDeviceId = table.Column<long>(type: "bigint", nullable: false),
                    TemperatureLocationId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TemperatureDeviceLocation", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TemperatureDeviceLocation_TemperatureDevices_TemperatureDeviceId",
                        column: x => x.TemperatureDeviceId,
                        principalTable: "TemperatureDevices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TemperatureDeviceLocation_TemperatureLocation_TemperatureLocationId",
                        column: x => x.TemperatureLocationId,
                        principalTable: "TemperatureLocation",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TemperatureReadings_TemperatureDeviceId",
                table: "TemperatureReadings",
                column: "TemperatureDeviceId");

            migrationBuilder.CreateIndex(
                name: "IX_TemperatureDeviceLocation_TemperatureDeviceId",
                table: "TemperatureDeviceLocation",
                column: "TemperatureDeviceId");

            migrationBuilder.CreateIndex(
                name: "IX_TemperatureDeviceLocation_TemperatureLocationId",
                table: "TemperatureDeviceLocation",
                column: "TemperatureLocationId");

            migrationBuilder.AddForeignKey(
                name: "FK_TemperatureReadings_TemperatureDevices_TemperatureDeviceId",
                table: "TemperatureReadings",
                column: "TemperatureDeviceId",
                principalTable: "TemperatureDevices",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TemperatureReadings_TemperatureDevices_TemperatureDeviceId",
                table: "TemperatureReadings");

            migrationBuilder.DropTable(
                name: "TemperatureDeviceLocation");

            migrationBuilder.DropTable(
                name: "TemperatureDevices");

            migrationBuilder.DropTable(
                name: "TemperatureLocation");

            migrationBuilder.DropIndex(
                name: "IX_TemperatureReadings_TemperatureDeviceId",
                table: "TemperatureReadings");

            migrationBuilder.DropColumn(
                name: "TemperatureDeviceId",
                table: "TemperatureReadings");
        }
    }
}
