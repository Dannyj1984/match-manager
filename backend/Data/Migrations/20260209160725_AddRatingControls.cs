using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FairPlay.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddRatingControls : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "AllowRatings",
                table: "Matches",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "AllowRatings",
                table: "Leagues",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AllowRatings",
                table: "Matches");

            migrationBuilder.DropColumn(
                name: "AllowRatings",
                table: "Leagues");
        }
    }
}
