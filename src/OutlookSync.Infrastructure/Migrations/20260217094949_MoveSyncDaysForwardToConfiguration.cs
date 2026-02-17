using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OutlookSync.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class MoveSyncDaysForwardToConfiguration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "SyncDaysForward",
                table: "Calendars",
                newName: "Configuration_SyncDaysForward");

            migrationBuilder.AlterColumn<int>(
                name: "Configuration_SyncDaysForward",
                table: "Calendars",
                type: "INTEGER",
                nullable: false,
                defaultValue: 30,
                oldClrType: typeof(int),
                oldType: "INTEGER");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Configuration_SyncDaysForward",
                table: "Calendars",
                newName: "SyncDaysForward");

            migrationBuilder.AlterColumn<int>(
                name: "SyncDaysForward",
                table: "Calendars",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "INTEGER",
                oldDefaultValue: 30);
        }
    }
}
