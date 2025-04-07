using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CosmicBot.Migrations
{
    /// <inheritdoc />
    public partial class Counting : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Counts",
                columns: table => new
                {
                    CountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GuildId = table.Column<decimal>(type: "decimal(20,0)", nullable: false),
                    LastUserId = table.Column<decimal>(type: "decimal(20,0)", nullable: true),
                    Count = table.Column<decimal>(type: "decimal(20,0)", nullable: false),
                    Record = table.Column<decimal>(type: "decimal(20,0)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Counts", x => x.CountId);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Counts");
        }
    }
}
