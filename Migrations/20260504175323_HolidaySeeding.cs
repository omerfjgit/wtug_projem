using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NoteTrackerApp.Migrations
{
    /// <inheritdoc />
    public partial class HolidaySeeding : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                column: "Password",
                value: "AQAAAAIAAYagAAAAECqCiboF9qHcQCvihbqy8sK99QP8JRiEEsnnukChjN1NHVplHG3s604tFMr2Jwehsw==");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                column: "Password",
                value: "AQAAAAIAAYagAAAAEHoqY8TTDbJ0xLy+KI9U1G9+tjlbpXJJ+dENkl936v+XXJNv3tZJJS2AukuszkXsbA==");
        }
    }
}
