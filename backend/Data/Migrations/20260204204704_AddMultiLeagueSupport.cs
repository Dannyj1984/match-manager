using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FairPlay.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddMultiLeagueSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUsers_Players_PlayerId",
                table: "AspNetUsers");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_PlayerId",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "PlayerId",
                table: "AspNetUsers");

            migrationBuilder.AddColumn<Guid>(
                name: "LeagueId",
                table: "Players",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "UserId",
                table: "Players",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "LeagueId",
                table: "Matches",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "Location",
                table: "Matches",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsSuperAdmin",
                table: "AspNetUsers",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "Leagues",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Sport = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    MaxTeams = table.Column<int>(type: "integer", nullable: false),
                    Location = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    CreatedByUserId = table.Column<string>(type: "text", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedById = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Leagues", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Leagues_AspNetUsers_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "LeagueMemberships",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    LeagueId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    Role = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    JoinedDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LeagueMemberships", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LeagueMemberships_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_LeagueMemberships_Leagues_LeagueId",
                        column: x => x.LeagueId,
                        principalTable: "Leagues",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Players_LeagueId",
                table: "Players",
                column: "LeagueId");

            migrationBuilder.CreateIndex(
                name: "IX_Players_UserId",
                table: "Players",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Matches_LeagueId",
                table: "Matches",
                column: "LeagueId");

            migrationBuilder.CreateIndex(
                name: "IX_LeagueMemberships_LeagueId_UserId",
                table: "LeagueMemberships",
                columns: new[] { "LeagueId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LeagueMemberships_UserId",
                table: "LeagueMemberships",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Leagues_CreatedById",
                table: "Leagues",
                column: "CreatedById");

            migrationBuilder.AddForeignKey(
                name: "FK_Matches_Leagues_LeagueId",
                table: "Matches",
                column: "LeagueId",
                principalTable: "Leagues",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Players_AspNetUsers_UserId",
                table: "Players",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Players_Leagues_LeagueId",
                table: "Players",
                column: "LeagueId",
                principalTable: "Leagues",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Matches_Leagues_LeagueId",
                table: "Matches");

            migrationBuilder.DropForeignKey(
                name: "FK_Players_AspNetUsers_UserId",
                table: "Players");

            migrationBuilder.DropForeignKey(
                name: "FK_Players_Leagues_LeagueId",
                table: "Players");

            migrationBuilder.DropTable(
                name: "LeagueMemberships");

            migrationBuilder.DropTable(
                name: "Leagues");

            migrationBuilder.DropIndex(
                name: "IX_Players_LeagueId",
                table: "Players");

            migrationBuilder.DropIndex(
                name: "IX_Players_UserId",
                table: "Players");

            migrationBuilder.DropIndex(
                name: "IX_Matches_LeagueId",
                table: "Matches");

            migrationBuilder.DropColumn(
                name: "LeagueId",
                table: "Players");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "Players");

            migrationBuilder.DropColumn(
                name: "LeagueId",
                table: "Matches");

            migrationBuilder.DropColumn(
                name: "Location",
                table: "Matches");

            migrationBuilder.DropColumn(
                name: "IsSuperAdmin",
                table: "AspNetUsers");

            migrationBuilder.AddColumn<Guid>(
                name: "PlayerId",
                table: "AspNetUsers",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_PlayerId",
                table: "AspNetUsers",
                column: "PlayerId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUsers_Players_PlayerId",
                table: "AspNetUsers",
                column: "PlayerId",
                principalTable: "Players",
                principalColumn: "Id");
        }
    }
}
