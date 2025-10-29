using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ChampBot.Migrations
{
    /// <inheritdoc />
    public partial class Init : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    TelegramUserid = table.Column<long>(type: "INTEGER", nullable: false),
                    ChatId = table.Column<long>(type: "INTEGER", nullable: false),
                    SteamId64 = table.Column<ulong>(type: "INTEGER", nullable: false),
                    DotaAccountId32 = table.Column<int>(type: "INTEGER", nullable: false),
                    ChallengeActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    ChallengeStartedUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    GoalWins = table.Column<int>(type: "INTEGER", nullable: false),
                    WinsSoFar = table.Column<int>(type: "INTEGER", nullable: false),
                    LossesSoFar = table.Column<int>(type: "INTEGER", nullable: false),
                    RemainingWins = table.Column<int>(type: "INTEGER", nullable: false),
                    LastSeenMatchId = table.Column<long>(type: "INTEGER", nullable: false),
                    PendingAction = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
