using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HomeSensors.Model.Migrations;

/// <inheritdoc />
public partial class RenameCameraSnapshotTableToCamera : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropPrimaryKey(
            name: "PK_CameraSnapshot",
            table: "CameraSnapshot");

        migrationBuilder.RenameTable(
            name: "CameraSnapshot",
            newName: "Camera");

        migrationBuilder.RenameIndex(
            name: "IX_CameraSnapshot_Name",
            table: "Camera",
            newName: "IX_Camera_Name");

        migrationBuilder.AddPrimaryKey(
            name: "PK_Camera",
            table: "Camera",
            column: "Id");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropPrimaryKey(
            name: "PK_Camera",
            table: "Camera");

        migrationBuilder.RenameTable(
            name: "Camera",
            newName: "CameraSnapshot");

        migrationBuilder.RenameIndex(
            name: "IX_Camera_Name",
            table: "CameraSnapshot",
            newName: "IX_CameraSnapshot_Name");

        migrationBuilder.AddPrimaryKey(
            name: "PK_CameraSnapshot",
            table: "CameraSnapshot",
            column: "Id");
    }
}
