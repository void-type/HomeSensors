using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HomeSensors.Model.Migrations;

/// <inheritdoc />
public partial class AddCameraSnapshot : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "CameraSnapshot",
            columns: table => new
            {
                Id = table.Column<long>(type: "bigint", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                Name = table.Column<string>(type: "nvarchar(450)", nullable: false),
                SnapshotsPath = table.Column<string>(type: "nvarchar(max)", nullable: false),
                IsHidden = table.Column<bool>(type: "bit", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_CameraSnapshot", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_CameraSnapshot_Name",
            table: "CameraSnapshot",
            column: "Name",
            unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "CameraSnapshot");
    }
}
