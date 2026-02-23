using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HomeSensors.Model.Migrations;

/// <inheritdoc />
public partial class AddWaterLeakDeviceAndEmailRecipient : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "EmailRecipient",
            columns: table => new
            {
                Id = table.Column<long>(type: "bigint", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                Email = table.Column<string>(type: "nvarchar(450)", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_EmailRecipient", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "WaterLeakDevice",
            columns: table => new
            {
                Id = table.Column<long>(type: "bigint", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                MqttTopic = table.Column<string>(type: "nvarchar(450)", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_WaterLeakDevice", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_EmailRecipient_Email",
            table: "EmailRecipient",
            column: "Email",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_WaterLeakDevice_MqttTopic",
            table: "WaterLeakDevice",
            column: "MqttTopic",
            unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "EmailRecipient");

        migrationBuilder.DropTable(
            name: "WaterLeakDevice");
    }
}
