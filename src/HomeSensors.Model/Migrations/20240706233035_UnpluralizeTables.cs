using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HomeSensors.Model.Migrations;

/// <inheritdoc />
public partial class UnpluralizeTables : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_TemperatureDevices_TemperatureLocations_TemperatureLocationId",
            table: "TemperatureDevices");

        migrationBuilder.DropForeignKey(
            name: "FK_TemperatureReadings_TemperatureDevices_TemperatureDeviceId",
            table: "TemperatureReadings");

        migrationBuilder.DropForeignKey(
            name: "FK_TemperatureReadings_TemperatureLocations_TemperatureLocationId",
            table: "TemperatureReadings");

        migrationBuilder.DropPrimaryKey(
            name: "PK_TemperatureReadings",
            table: "TemperatureReadings");

        migrationBuilder.DropPrimaryKey(
            name: "PK_TemperatureLocations",
            table: "TemperatureLocations");

        migrationBuilder.DropPrimaryKey(
            name: "PK_TemperatureDevices",
            table: "TemperatureDevices");

        migrationBuilder.RenameTable(
            name: "TemperatureReadings",
            newName: "TemperatureReading");

        migrationBuilder.RenameTable(
            name: "TemperatureLocations",
            newName: "TemperatureLocation");

        migrationBuilder.RenameTable(
            name: "TemperatureDevices",
            newName: "TemperatureDevice");

        migrationBuilder.RenameIndex(
            name: "IX_TemperatureReadings_Time_TemperatureLocationId",
            table: "TemperatureReading",
            newName: "IX_TemperatureReading_Time_TemperatureLocationId");

        migrationBuilder.RenameIndex(
            name: "IX_TemperatureReadings_Time_TemperatureDeviceId_IsSummary",
            table: "TemperatureReading",
            newName: "IX_TemperatureReading_Time_TemperatureDeviceId_IsSummary");

        migrationBuilder.RenameIndex(
            name: "IX_TemperatureReadings_Time",
            table: "TemperatureReading",
            newName: "IX_TemperatureReading_Time");

        migrationBuilder.RenameIndex(
            name: "IX_TemperatureReadings_TemperatureLocationId",
            table: "TemperatureReading",
            newName: "IX_TemperatureReading_TemperatureLocationId");

        migrationBuilder.RenameIndex(
            name: "IX_TemperatureReadings_TemperatureDeviceId",
            table: "TemperatureReading",
            newName: "IX_TemperatureReading_TemperatureDeviceId");

        migrationBuilder.RenameIndex(
            name: "IX_TemperatureReadings_IsSummary",
            table: "TemperatureReading",
            newName: "IX_TemperatureReading_IsSummary");

        migrationBuilder.RenameIndex(
            name: "IX_TemperatureLocations_Name",
            table: "TemperatureLocation",
            newName: "IX_TemperatureLocation_Name");

        migrationBuilder.RenameIndex(
            name: "IX_TemperatureDevices_TemperatureLocationId",
            table: "TemperatureDevice",
            newName: "IX_TemperatureDevice_TemperatureLocationId");

        migrationBuilder.AlterColumn<string>(
            name: "MqttTopic",
            table: "TemperatureDevice",
            type: "nvarchar(450)",
            nullable: false,
            oldClrType: typeof(string),
            oldType: "nvarchar(max)");

        migrationBuilder.AddPrimaryKey(
            name: "PK_TemperatureReading",
            table: "TemperatureReading",
            column: "Id");

        migrationBuilder.AddPrimaryKey(
            name: "PK_TemperatureLocation",
            table: "TemperatureLocation",
            column: "Id");

        migrationBuilder.AddPrimaryKey(
            name: "PK_TemperatureDevice",
            table: "TemperatureDevice",
            column: "Id");

        migrationBuilder.CreateIndex(
            name: "IX_TemperatureDevice_MqttTopic",
            table: "TemperatureDevice",
            column: "MqttTopic",
            unique: true);

        migrationBuilder.AddForeignKey(
            name: "FK_TemperatureDevice_TemperatureLocation_TemperatureLocationId",
            table: "TemperatureDevice",
            column: "TemperatureLocationId",
            principalTable: "TemperatureLocation",
            principalColumn: "Id");

        migrationBuilder.AddForeignKey(
            name: "FK_TemperatureReading_TemperatureDevice_TemperatureDeviceId",
            table: "TemperatureReading",
            column: "TemperatureDeviceId",
            principalTable: "TemperatureDevice",
            principalColumn: "Id",
            onDelete: ReferentialAction.Cascade);

        migrationBuilder.AddForeignKey(
            name: "FK_TemperatureReading_TemperatureLocation_TemperatureLocationId",
            table: "TemperatureReading",
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

        migrationBuilder.DropForeignKey(
            name: "FK_TemperatureReading_TemperatureDevice_TemperatureDeviceId",
            table: "TemperatureReading");

        migrationBuilder.DropForeignKey(
            name: "FK_TemperatureReading_TemperatureLocation_TemperatureLocationId",
            table: "TemperatureReading");

        migrationBuilder.DropPrimaryKey(
            name: "PK_TemperatureReading",
            table: "TemperatureReading");

        migrationBuilder.DropPrimaryKey(
            name: "PK_TemperatureLocation",
            table: "TemperatureLocation");

        migrationBuilder.DropPrimaryKey(
            name: "PK_TemperatureDevice",
            table: "TemperatureDevice");

        migrationBuilder.DropIndex(
            name: "IX_TemperatureDevice_MqttTopic",
            table: "TemperatureDevice");

        migrationBuilder.RenameTable(
            name: "TemperatureReading",
            newName: "TemperatureReadings");

        migrationBuilder.RenameTable(
            name: "TemperatureLocation",
            newName: "TemperatureLocations");

        migrationBuilder.RenameTable(
            name: "TemperatureDevice",
            newName: "TemperatureDevices");

        migrationBuilder.RenameIndex(
            name: "IX_TemperatureReading_Time_TemperatureLocationId",
            table: "TemperatureReadings",
            newName: "IX_TemperatureReadings_Time_TemperatureLocationId");

        migrationBuilder.RenameIndex(
            name: "IX_TemperatureReading_Time_TemperatureDeviceId_IsSummary",
            table: "TemperatureReadings",
            newName: "IX_TemperatureReadings_Time_TemperatureDeviceId_IsSummary");

        migrationBuilder.RenameIndex(
            name: "IX_TemperatureReading_Time",
            table: "TemperatureReadings",
            newName: "IX_TemperatureReadings_Time");

        migrationBuilder.RenameIndex(
            name: "IX_TemperatureReading_TemperatureLocationId",
            table: "TemperatureReadings",
            newName: "IX_TemperatureReadings_TemperatureLocationId");

        migrationBuilder.RenameIndex(
            name: "IX_TemperatureReading_TemperatureDeviceId",
            table: "TemperatureReadings",
            newName: "IX_TemperatureReadings_TemperatureDeviceId");

        migrationBuilder.RenameIndex(
            name: "IX_TemperatureReading_IsSummary",
            table: "TemperatureReadings",
            newName: "IX_TemperatureReadings_IsSummary");

        migrationBuilder.RenameIndex(
            name: "IX_TemperatureLocation_Name",
            table: "TemperatureLocations",
            newName: "IX_TemperatureLocations_Name");

        migrationBuilder.RenameIndex(
            name: "IX_TemperatureDevice_TemperatureLocationId",
            table: "TemperatureDevices",
            newName: "IX_TemperatureDevices_TemperatureLocationId");

        migrationBuilder.AlterColumn<string>(
            name: "MqttTopic",
            table: "TemperatureDevices",
            type: "nvarchar(max)",
            nullable: false,
            oldClrType: typeof(string),
            oldType: "nvarchar(450)");

        migrationBuilder.AddPrimaryKey(
            name: "PK_TemperatureReadings",
            table: "TemperatureReadings",
            column: "Id");

        migrationBuilder.AddPrimaryKey(
            name: "PK_TemperatureLocations",
            table: "TemperatureLocations",
            column: "Id");

        migrationBuilder.AddPrimaryKey(
            name: "PK_TemperatureDevices",
            table: "TemperatureDevices",
            column: "Id");

        migrationBuilder.AddForeignKey(
            name: "FK_TemperatureDevices_TemperatureLocations_TemperatureLocationId",
            table: "TemperatureDevices",
            column: "TemperatureLocationId",
            principalTable: "TemperatureLocations",
            principalColumn: "Id");

        migrationBuilder.AddForeignKey(
            name: "FK_TemperatureReadings_TemperatureDevices_TemperatureDeviceId",
            table: "TemperatureReadings",
            column: "TemperatureDeviceId",
            principalTable: "TemperatureDevices",
            principalColumn: "Id",
            onDelete: ReferentialAction.Cascade);

        migrationBuilder.AddForeignKey(
            name: "FK_TemperatureReadings_TemperatureLocations_TemperatureLocationId",
            table: "TemperatureReadings",
            column: "TemperatureLocationId",
            principalTable: "TemperatureLocations",
            principalColumn: "Id");
    }
}
