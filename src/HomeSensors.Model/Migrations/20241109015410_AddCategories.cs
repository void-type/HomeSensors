using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HomeSensors.Model.Migrations;

/// <inheritdoc />
public partial class AddCategories : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<long>(
            name: "CategoryId",
            table: "TemperatureLocation",
            type: "bigint",
            nullable: true);

        migrationBuilder.AddColumn<bool>(
            name: "IsHidden",
            table: "TemperatureLocation",
            type: "bit",
            nullable: false,
            defaultValue: false);

        migrationBuilder.CreateTable(
            name: "Category",
            columns: table => new
            {
                Id = table.Column<long>(type: "bigint", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                Name = table.Column<string>(type: "nvarchar(450)", nullable: false),
                Order = table.Column<int>(type: "int", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Category", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_TemperatureLocation_CategoryId",
            table: "TemperatureLocation",
            column: "CategoryId");

        migrationBuilder.CreateIndex(
            name: "IX_Category_Name",
            table: "Category",
            column: "Name",
            unique: true);

        migrationBuilder.AddForeignKey(
            name: "FK_TemperatureLocation_Category_CategoryId",
            table: "TemperatureLocation",
            column: "CategoryId",
            principalTable: "Category",
            principalColumn: "Id");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_TemperatureLocation_Category_CategoryId",
            table: "TemperatureLocation");

        migrationBuilder.DropTable(
            name: "Category");

        migrationBuilder.DropIndex(
            name: "IX_TemperatureLocation_CategoryId",
            table: "TemperatureLocation");

        migrationBuilder.DropColumn(
            name: "CategoryId",
            table: "TemperatureLocation");

        migrationBuilder.DropColumn(
            name: "IsHidden",
            table: "TemperatureLocation");
    }
}
