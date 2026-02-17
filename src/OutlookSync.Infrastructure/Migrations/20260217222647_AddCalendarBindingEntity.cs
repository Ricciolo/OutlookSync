using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OutlookSync.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCalendarBindingEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CalendarBindings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    SourceCalendarId = table.Column<Guid>(type: "TEXT", nullable: false),
                    TargetCalendarId = table.Column<Guid>(type: "TEXT", nullable: false),
                    IsEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    Configuration_TitleHandling = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Configuration_CustomTitle = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    Configuration_CopyDescription = table.Column<bool>(type: "INTEGER", nullable: false),
                    Configuration_CopyParticipants = table.Column<bool>(type: "INTEGER", nullable: false),
                    Configuration_CopyLocation = table.Column<bool>(type: "INTEGER", nullable: false),
                    Configuration_CopyConferenceLink = table.Column<bool>(type: "INTEGER", nullable: false),
                    Configuration_TargetEventColor = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    Configuration_TargetCategory = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    Configuration_TargetStatus = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    Configuration_CopyAttachments = table.Column<bool>(type: "INTEGER", nullable: false),
                    Configuration_ReminderHandling = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Configuration_MarkAsPrivate = table.Column<bool>(type: "INTEGER", nullable: false),
                    Configuration_CustomTag = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    Configuration_CustomTagInTitle = table.Column<bool>(type: "INTEGER", nullable: false),
                    ExcludedColors = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    ExcludedRsvpResponses = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    ExcludedStatuses = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    LastSyncAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    LastSyncEventCount = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 0),
                    LastSyncError = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CalendarBindings", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CalendarBindings_SourceTarget_Unique",
                table: "CalendarBindings",
                columns: new[] { "SourceCalendarId", "TargetCalendarId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CalendarBindings");
        }
    }
}
