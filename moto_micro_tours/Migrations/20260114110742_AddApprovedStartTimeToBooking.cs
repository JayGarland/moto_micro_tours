using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace moto_micro_tours.Migrations
{
    /// <inheritdoc />
    public partial class AddApprovedStartTimeToBooking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ApprovedStartAtUtc",
                table: "Bookings",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ApprovedStartByMotardAtUtc",
                table: "Bookings",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ApprovedStartReason",
                table: "Bookings",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ApprovedStartAtUtc",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "ApprovedStartByMotardAtUtc",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "ApprovedStartReason",
                table: "Bookings");
        }
    }
}
