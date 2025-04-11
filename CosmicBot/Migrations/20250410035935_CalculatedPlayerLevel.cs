using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CosmicBot.Migrations
{
    /// <inheritdoc />
    public partial class CalculatedPlayerLevel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Level",
                table: "PlayerStats");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Level",
                table: "PlayerStats",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }
    }
}
