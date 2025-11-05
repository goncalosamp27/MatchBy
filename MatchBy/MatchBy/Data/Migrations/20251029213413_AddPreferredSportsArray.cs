using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MatchBy.Data.Migrations;
    /// <inheritdoc />
    public partial class AddPreferredSportsArray : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            //migrationBuilder.AddColumn<int[]>(
            //    name: "PreferredSports",
            //    table: "AspNetUsers",
            //    type: "integer[]",
            //    nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PreferredSports",
                table: "AspNetUsers");
        }
    }

