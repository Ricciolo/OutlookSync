using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OutlookSync.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCalendarBindingWithCredentials : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CalendarBindings_SourceTarget_Unique",
                table: "CalendarBindings");

            migrationBuilder.RenameColumn(
                name: "TargetCalendarId",
                table: "CalendarBindings",
                newName: "TargetCredentialId");

            migrationBuilder.RenameColumn(
                name: "SourceCalendarId",
                table: "CalendarBindings",
                newName: "SourceCredentialId");

            migrationBuilder.AddColumn<string>(
                name: "SourceCalendarExternalId",
                table: "CalendarBindings",
                type: "TEXT",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TargetCalendarExternalId",
                table: "CalendarBindings",
                type: "TEXT",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_CalendarBindings_SourceTarget_Unique",
                table: "CalendarBindings",
                columns: new[] { "SourceCredentialId", "SourceCalendarExternalId", "TargetCredentialId", "TargetCalendarExternalId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CalendarBindings_SourceTarget_Unique",
                table: "CalendarBindings");

            migrationBuilder.DropColumn(
                name: "SourceCalendarExternalId",
                table: "CalendarBindings");

            migrationBuilder.DropColumn(
                name: "TargetCalendarExternalId",
                table: "CalendarBindings");

            migrationBuilder.RenameColumn(
                name: "TargetCredentialId",
                table: "CalendarBindings",
                newName: "TargetCalendarId");

            migrationBuilder.RenameColumn(
                name: "SourceCredentialId",
                table: "CalendarBindings",
                newName: "SourceCalendarId");

            migrationBuilder.CreateIndex(
                name: "IX_CalendarBindings_SourceTarget_Unique",
                table: "CalendarBindings",
                columns: new[] { "SourceCalendarId", "TargetCalendarId" },
                unique: true);
        }
    }
}
