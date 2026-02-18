using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OutlookSync.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveColorAddConferenceLink : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Configuration_TargetEventColor",
                table: "CalendarBindings");

            migrationBuilder.DropColumn(
                name: "ExcludedColors",
                table: "CalendarBindings");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Configuration_TargetEventColor",
                table: "CalendarBindings",
                type: "TEXT",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExcludedColors",
                table: "CalendarBindings",
                type: "TEXT",
                maxLength: 500,
                nullable: false,
                defaultValue: "");
        }
    }
}
