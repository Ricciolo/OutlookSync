using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OutlookSync.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveCopyConferenceLinkFromConfiguration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Configuration_CopyConferenceLink",
                table: "CalendarBindings");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "Configuration_CopyConferenceLink",
                table: "CalendarBindings",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }
    }
}
