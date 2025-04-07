using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CosmicBot.Migrations
{
    /// <inheritdoc />
    public partial class PlayerBirthdays : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "Birthday",
                table: "PlayerStats",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Birthday",
                table: "PlayerStats");
        }
    }
}
