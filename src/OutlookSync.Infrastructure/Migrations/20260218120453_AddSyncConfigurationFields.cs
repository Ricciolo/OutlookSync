using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OutlookSync.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSyncConfigurationFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Configuration_SyncDaysForward",
                table: "CalendarBindings",
                type: "INTEGER",
                nullable: false,
                defaultValue: 30);

            migrationBuilder.AddColumn<string>(
                name: "SyncCronExpression",
                table: "CalendarBindings",
                type: "TEXT",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SyncIntervalMinutes",
                table: "CalendarBindings",
                type: "INTEGER",
                nullable: false,
                defaultValue: 30);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Configuration_SyncDaysForward",
                table: "CalendarBindings");

            migrationBuilder.DropColumn(
                name: "SyncCronExpression",
                table: "CalendarBindings");

            migrationBuilder.DropColumn(
                name: "SyncIntervalMinutes",
                table: "CalendarBindings");
        }
    }
}
