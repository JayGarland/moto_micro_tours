using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace moto_micro_tours.Migrations
{
    /// <inheritdoc />
    public partial class AddBookingCompletionTimestamps : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "CompletedAtUtc",
                table: "Bookings",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CompletedByMotardAtUtc",
                table: "Bookings",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CompletedByParticipantAtUtc",
                table: "Bookings",
                type: "datetime(6)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CompletedAtUtc",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "CompletedByMotardAtUtc",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "CompletedByParticipantAtUtc",
                table: "Bookings");
        }
    }
}
