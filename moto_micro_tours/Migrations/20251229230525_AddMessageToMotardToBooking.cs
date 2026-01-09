using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace moto_micro_tours.Migrations
{
    /// <inheritdoc />
    public partial class AddMessageToMotardToBooking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "MessageToMotard",
                table: "Bookings",
                type: "longtext",
                nullable: true,
                collation: "utf8mb4_general_ci")
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MessageToMotard",
                table: "Bookings");
        }
    }
}
