using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HomeSensors.Data.Migrations
{
    public partial class DeviceNormalization1 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "TemperatureDeviceId",
                table: "TemperatureReadings",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "TemperatureLocationId",
                table: "TemperatureReadings",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "TemperatureLocations",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TemperatureLocations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TemperatureDevices",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DeviceModel = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DeviceId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DeviceChannel = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CurrentTemperatureLocationId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TemperatureDevices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TemperatureDevices_TemperatureLocations_CurrentTemperatureLocationId",
                        column: x => x.CurrentTemperatureLocationId,
                        principalTable: "TemperatureLocations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TemperatureReadings_TemperatureDeviceId",
                table: "TemperatureReadings",
                column: "TemperatureDeviceId");

            migrationBuilder.CreateIndex(
                name: "IX_TemperatureReadings_TemperatureLocationId",
                table: "TemperatureReadings",
                column: "TemperatureLocationId");

            migrationBuilder.CreateIndex(
                name: "IX_TemperatureDevices_CurrentTemperatureLocationId",
                table: "TemperatureDevices",
                column: "CurrentTemperatureLocationId");

            migrationBuilder.AddForeignKey(
                name: "FK_TemperatureReadings_TemperatureDevices_TemperatureDeviceId",
                table: "TemperatureReadings",
                column: "TemperatureDeviceId",
                principalTable: "TemperatureDevices",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_TemperatureReadings_TemperatureLocations_TemperatureLocationId",
                table: "TemperatureReadings",
                column: "TemperatureLocationId",
                principalTable: "TemperatureLocations",
                principalColumn: "Id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TemperatureReadings_TemperatureDevices_TemperatureDeviceId",
                table: "TemperatureReadings");

            migrationBuilder.DropForeignKey(
                name: "FK_TemperatureReadings_TemperatureLocations_TemperatureLocationId",
                table: "TemperatureReadings");

            migrationBuilder.DropTable(
                name: "TemperatureDevices");

            migrationBuilder.DropTable(
                name: "TemperatureLocations");

            migrationBuilder.DropIndex(
                name: "IX_TemperatureReadings_TemperatureDeviceId",
                table: "TemperatureReadings");

            migrationBuilder.DropIndex(
                name: "IX_TemperatureReadings_TemperatureLocationId",
                table: "TemperatureReadings");

            migrationBuilder.DropColumn(
                name: "TemperatureDeviceId",
                table: "TemperatureReadings");

            migrationBuilder.DropColumn(
                name: "TemperatureLocationId",
                table: "TemperatureReadings");
        }
    }
}
