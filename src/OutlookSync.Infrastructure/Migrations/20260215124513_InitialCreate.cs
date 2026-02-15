using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OutlookSync.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Calendars",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    ExternalId = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    CredentialId = table.Column<Guid>(type: "TEXT", nullable: false),
                    IsEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    SyncDaysForward = table.Column<int>(type: "INTEGER", nullable: false),
                    Configuration_Interval_Minutes = table.Column<int>(type: "INTEGER", nullable: false),
                    Configuration_Interval_CronExpression = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    Configuration_StartDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Configuration_IsPrivate = table.Column<bool>(type: "INTEGER", nullable: false),
                    Configuration_FieldSelection_Subject = table.Column<bool>(type: "INTEGER", nullable: false),
                    Configuration_FieldSelection_StartTime = table.Column<bool>(type: "INTEGER", nullable: false),
                    Configuration_FieldSelection_EndTime = table.Column<bool>(type: "INTEGER", nullable: false),
                    Configuration_FieldSelection_Location = table.Column<bool>(type: "INTEGER", nullable: false),
                    Configuration_FieldSelection_Attendees = table.Column<bool>(type: "INTEGER", nullable: false),
                    Configuration_FieldSelection_Body = table.Column<bool>(type: "INTEGER", nullable: false),
                    Configuration_FieldSelection_Organizer = table.Column<bool>(type: "INTEGER", nullable: false),
                    Configuration_FieldSelection_IsAllDay = table.Column<bool>(type: "INTEGER", nullable: false),
                    Configuration_FieldSelection_Recurrence = table.Column<bool>(type: "INTEGER", nullable: false),
                    LastSyncAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Calendars", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Credentials",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    TokenStatus = table.Column<int>(type: "INTEGER", nullable: false),
                    StatusData = table.Column<byte[]>(type: "BLOB", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Credentials", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Calendars_ExternalId",
                table: "Calendars",
                column: "ExternalId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Calendars");

            migrationBuilder.DropTable(
                name: "Credentials");
        }
    }
}
