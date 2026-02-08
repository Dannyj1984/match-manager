using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FairPlay.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddMultiplePreferredPositions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // First, add a temporary column to hold the array data
            migrationBuilder.Sql(@"
                ALTER TABLE ""Players"" 
                ADD COLUMN ""PreferredPosition_temp"" text[];
            ");
            
            // Convert existing single position strings to arrays
            migrationBuilder.Sql(@"
                UPDATE ""Players"" 
                SET ""PreferredPosition_temp"" = ARRAY[""PreferredPosition""];
            ");
            
            // Drop the old column
            migrationBuilder.DropColumn(
                name: "PreferredPosition",
                table: "Players");
            
            // Rename temp column to PreferredPosition
            migrationBuilder.RenameColumn(
                name: "PreferredPosition_temp",
                table: "Players",
                newName: "PreferredPosition");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "PreferredPosition",
                table: "Players",
                type: "character varying(10)",
                maxLength: 10,
                nullable: false,
                oldClrType: typeof(List<string>),
                oldType: "text[]");
        }
    }
}
