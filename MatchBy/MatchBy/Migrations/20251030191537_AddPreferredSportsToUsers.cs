using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MatchBy.Migrations
{
    /// <inheritdoc />
    public partial class AddPreferredSportsToUsers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Intentionally empty: the PreferredSports property uses a ValueConverter
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Intentionally empty: no rollback required for this migration
        }
    }
}
