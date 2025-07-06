using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HomeSensors.Model.Migrations
{
    /// <inheritdoc />
    public partial class NonNullableThermostatAction : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ThermostatAction_EntityId_LastChanged",
                table: "ThermostatAction");

            migrationBuilder.AlterColumn<string>(
                name: "State",
                table: "ThermostatAction",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "LastUpdated",
                table: "ThermostatAction",
                type: "datetimeoffset",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)),
                oldClrType: typeof(DateTimeOffset),
                oldType: "datetimeoffset",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "LastChanged",
                table: "ThermostatAction",
                type: "datetimeoffset",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)),
                oldClrType: typeof(DateTimeOffset),
                oldType: "datetimeoffset",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "EntityId",
                table: "ThermostatAction",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ThermostatAction_EntityId_LastChanged_LastUpdated",
                table: "ThermostatAction",
                columns: new[] { "EntityId", "LastChanged", "LastUpdated" },
                unique: true)
                .Annotation("SqlServer:Include", new[] { "State" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ThermostatAction_EntityId_LastChanged_LastUpdated",
                table: "ThermostatAction");

            migrationBuilder.AlterColumn<string>(
                name: "State",
                table: "ThermostatAction",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "LastUpdated",
                table: "ThermostatAction",
                type: "datetimeoffset",
                nullable: true,
                oldClrType: typeof(DateTimeOffset),
                oldType: "datetimeoffset");

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "LastChanged",
                table: "ThermostatAction",
                type: "datetimeoffset",
                nullable: true,
                oldClrType: typeof(DateTimeOffset),
                oldType: "datetimeoffset");

            migrationBuilder.AlterColumn<string>(
                name: "EntityId",
                table: "ThermostatAction",
                type: "nvarchar(450)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.CreateIndex(
                name: "IX_ThermostatAction_EntityId_LastChanged",
                table: "ThermostatAction",
                columns: new[] { "EntityId", "LastChanged" },
                unique: true,
                filter: "[EntityId] IS NOT NULL AND [LastChanged] IS NOT NULL")
                .Annotation("SqlServer:Include", new[] { "LastUpdated", "State" });
        }
    }
}
