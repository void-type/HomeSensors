using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HomeSensors.Model.Migrations;

/// <inheritdoc />
public partial class ThermostatAction : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "ThermostatAction",
            columns: table => new
            {
                Id = table.Column<long>(type: "bigint", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                EntityId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                State = table.Column<string>(type: "nvarchar(max)", nullable: true),
                LastChanged = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                LastUpdated = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ThermostatAction", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_ThermostatAction_EntityId_LastChanged",
            table: "ThermostatAction",
            columns: new[] { "EntityId", "LastChanged" },
            unique: true,
            filter: "[EntityId] IS NOT NULL AND [LastChanged] IS NOT NULL")
            .Annotation("SqlServer:Include", new[] { "LastUpdated", "State" });
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "ThermostatAction");
    }
}
