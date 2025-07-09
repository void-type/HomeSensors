using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HomeSensors.Model.Migrations;

/// <inheritdoc />
public partial class ThermostatActionRename : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropPrimaryKey(
            name: "PK_ThermostatAction",
            table: "ThermostatAction");

        migrationBuilder.RenameTable(
            name: "ThermostatAction",
            newName: "HvacAction");

        migrationBuilder.RenameIndex(
            name: "IX_ThermostatAction_EntityId_LastChanged_LastUpdated",
            table: "HvacAction",
            newName: "IX_HvacAction_EntityId_LastChanged_LastUpdated");

        migrationBuilder.AddPrimaryKey(
            name: "PK_HvacAction",
            table: "HvacAction",
            column: "Id");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropPrimaryKey(
            name: "PK_HvacAction",
            table: "HvacAction");

        migrationBuilder.RenameTable(
            name: "HvacAction",
            newName: "ThermostatAction");

        migrationBuilder.RenameIndex(
            name: "IX_HvacAction_EntityId_LastChanged_LastUpdated",
            table: "ThermostatAction",
            newName: "IX_ThermostatAction_EntityId_LastChanged_LastUpdated");

        migrationBuilder.AddPrimaryKey(
            name: "PK_ThermostatAction",
            table: "ThermostatAction",
            column: "Id");
    }
}
