using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace moto_micro_tours.Migrations
{
    /// <inheritdoc />
    public partial class AddBookingApprovalAudit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ApprovedStartByMotardAtUtc",
                table: "Bookings",
                newName: "ApprovedAtUtc");

            migrationBuilder.AddColumn<string>(
                name: "ApprovedByUserId",
                table: "Bookings",
                type: "varchar(255)",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_ApprovedByUserId",
                table: "Bookings",
                column: "ApprovedByUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Bookings_AspNetUsers_ApprovedByUserId",
                table: "Bookings",
                column: "ApprovedByUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Bookings_AspNetUsers_ApprovedByUserId",
                table: "Bookings");

            migrationBuilder.DropIndex(
                name: "IX_Bookings_ApprovedByUserId",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "ApprovedByUserId",
                table: "Bookings");

            migrationBuilder.RenameColumn(
                name: "ApprovedAtUtc",
                table: "Bookings",
                newName: "ApprovedStartByMotardAtUtc");
        }
    }
}
