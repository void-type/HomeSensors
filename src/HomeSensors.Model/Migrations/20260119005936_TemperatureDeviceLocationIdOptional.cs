using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HomeSensors.Model.Migrations
{
    /// <inheritdoc />
    public partial class TemperatureDeviceLocationIdOptional : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TemperatureDevice_TemperatureLocation_TemperatureLocationId",
                table: "TemperatureDevice");

            migrationBuilder.AlterColumn<long>(
                name: "TemperatureLocationId",
                table: "TemperatureDevice",
                type: "bigint",
                nullable: true,
                oldClrType: typeof(long),
                oldType: "bigint");

            migrationBuilder.AddForeignKey(
                name: "FK_TemperatureDevice_TemperatureLocation_TemperatureLocationId",
                table: "TemperatureDevice",
                column: "TemperatureLocationId",
                principalTable: "TemperatureLocation",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TemperatureDevice_TemperatureLocation_TemperatureLocationId",
                table: "TemperatureDevice");

            migrationBuilder.AlterColumn<long>(
                name: "TemperatureLocationId",
                table: "TemperatureDevice",
                type: "bigint",
                nullable: false,
                defaultValue: 0L,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_TemperatureDevice_TemperatureLocation_TemperatureLocationId",
                table: "TemperatureDevice",
                column: "TemperatureLocationId",
                principalTable: "TemperatureLocation",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
