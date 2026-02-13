using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FairPlay.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPublicLeaguesAndJoinRequests : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsPublic",
                table: "Leagues",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<double>(
                name: "Latitude",
                table: "Leagues",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Longitude",
                table: "Leagues",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Postcode",
                table: "Leagues",
                type: "character varying(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "LeagueJoinRequests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    LeagueId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    RequestedDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    ReviewedDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    ReviewedByUserId = table.Column<string>(type: "text", nullable: true),
                    ReviewedById = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LeagueJoinRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LeagueJoinRequests_AspNetUsers_ReviewedById",
                        column: x => x.ReviewedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_LeagueJoinRequests_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_LeagueJoinRequests_Leagues_LeagueId",
                        column: x => x.LeagueId,
                        principalTable: "Leagues",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LeagueJoinRequests_LeagueId_UserId",
                table: "LeagueJoinRequests",
                columns: new[] { "LeagueId", "UserId" },
                unique: true,
                filter: "\"Status\" = 'Pending'");

            migrationBuilder.CreateIndex(
                name: "IX_LeagueJoinRequests_ReviewedById",
                table: "LeagueJoinRequests",
                column: "ReviewedById");

            migrationBuilder.CreateIndex(
                name: "IX_LeagueJoinRequests_UserId",
                table: "LeagueJoinRequests",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LeagueJoinRequests");

            migrationBuilder.DropColumn(
                name: "IsPublic",
                table: "Leagues");

            migrationBuilder.DropColumn(
                name: "Latitude",
                table: "Leagues");

            migrationBuilder.DropColumn(
                name: "Longitude",
                table: "Leagues");

            migrationBuilder.DropColumn(
                name: "Postcode",
                table: "Leagues");
        }
    }
}
