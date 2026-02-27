using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DAL.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GameConfigurations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    BoardWidth = table.Column<int>(type: "INTEGER", nullable: false),
                    BoardHeight = table.Column<int>(type: "INTEGER", nullable: false),
                    WinCondition = table.Column<int>(type: "INTEGER", nullable: false),
                    IsCylindrical = table.Column<bool>(type: "INTEGER", nullable: false),
                    P1Type = table.Column<int>(type: "INTEGER", nullable: false),
                    P2Type = table.Column<int>(type: "INTEGER", nullable: false),
                    Player1Name = table.Column<string>(type: "TEXT", nullable: false),
                    Player2Name = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GameConfigurations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GameStates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    ConfigurationId = table.Column<Guid>(type: "TEXT", nullable: false),
                    BoardSerialized = table.Column<string>(type: "TEXT", nullable: false),
                    IsNextPlayerX = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GameStates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GameStates_GameConfigurations_ConfigurationId",
                        column: x => x.ConfigurationId,
                        principalTable: "GameConfigurations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GameStates_ConfigurationId",
                table: "GameStates",
                column: "ConfigurationId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GameStates");

            migrationBuilder.DropTable(
                name: "GameConfigurations");
        }
    }
}
