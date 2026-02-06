using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FairPlay.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCostToLeague : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "Cost",
                table: "Leagues",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Cost",
                table: "Leagues");
        }
    }
}
