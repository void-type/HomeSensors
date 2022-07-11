using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Rtl_433.Mqtt.Migrations
{
    public partial class DeviceNormalization2 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TemperatureDevices_TemperatureLocations_CurrentTemperatureLocationId",
                table: "TemperatureDevices");

            migrationBuilder.DropForeignKey(
                name: "FK_TemperatureReadings_TemperatureDevices_TemperatureDeviceId",
                table: "TemperatureReadings");

            migrationBuilder.DropColumn(
                name: "DeviceChannel",
                table: "TemperatureReadings");

            migrationBuilder.DropColumn(
                name: "DeviceId",
                table: "TemperatureReadings");

            migrationBuilder.DropColumn(
                name: "DeviceModel",
                table: "TemperatureReadings");

            migrationBuilder.AlterColumn<long>(
                name: "TemperatureDeviceId",
                table: "TemperatureReadings",
                type: "bigint",
                nullable: false,
                defaultValue: 0L,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldNullable: true);

            migrationBuilder.AlterColumn<long>(
                name: "CurrentTemperatureLocationId",
                table: "TemperatureDevices",
                type: "bigint",
                nullable: true,
                oldClrType: typeof(long),
                oldType: "bigint");

            migrationBuilder.AddForeignKey(
                name: "FK_TemperatureDevices_TemperatureLocations_CurrentTemperatureLocationId",
                table: "TemperatureDevices",
                column: "CurrentTemperatureLocationId",
                principalTable: "TemperatureLocations",
                principalColumn: "Id");

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
                name: "FK_TemperatureDevices_TemperatureLocations_CurrentTemperatureLocationId",
                table: "TemperatureDevices");

            migrationBuilder.DropForeignKey(
                name: "FK_TemperatureReadings_TemperatureDevices_TemperatureDeviceId",
                table: "TemperatureReadings");

            migrationBuilder.AlterColumn<long>(
                name: "TemperatureDeviceId",
                table: "TemperatureReadings",
                type: "bigint",
                nullable: true,
                oldClrType: typeof(long),
                oldType: "bigint");

            migrationBuilder.AddColumn<string>(
                name: "DeviceChannel",
                table: "TemperatureReadings",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeviceId",
                table: "TemperatureReadings",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeviceModel",
                table: "TemperatureReadings",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<long>(
                name: "CurrentTemperatureLocationId",
                table: "TemperatureDevices",
                type: "bigint",
                nullable: false,
                defaultValue: 0L,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_TemperatureDevices_TemperatureLocations_CurrentTemperatureLocationId",
                table: "TemperatureDevices",
                column: "CurrentTemperatureLocationId",
                principalTable: "TemperatureLocations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TemperatureReadings_TemperatureDevices_TemperatureDeviceId",
                table: "TemperatureReadings",
                column: "TemperatureDeviceId",
                principalTable: "TemperatureDevices",
                principalColumn: "Id");
        }
    }
}
