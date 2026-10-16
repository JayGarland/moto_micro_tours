using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace moto_micro_tours.Migrations
{
    /// <inheritdoc />
    public partial class FixTourOrganizerFK : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // No earlier migration creates Tours.OrganizerUserId (or an older
            // OrganizerId column), so this migration must create the column
            // itself. The previous version dropped an FK/column that never
            // exists on a database built from this migration chain, which made
            // fresh-MySQL startup fail with "Can't DROP
            // 'FK_Tours_AspNetUsers_OrganizerId'".
            migrationBuilder.AddColumn<string>(
                name: "OrganizerUserId",
                table: "Tours",
                type: "varchar(255)",
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_Tours_OrganizerUserId",
                table: "Tours",
                column: "OrganizerUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Tours_AspNetUsers_OrganizerUserId",
                table: "Tours",
                column: "OrganizerUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Tours_AspNetUsers_OrganizerUserId",
                table: "Tours");

            migrationBuilder.DropIndex(
                name: "IX_Tours_OrganizerUserId",
                table: "Tours");

            migrationBuilder.DropColumn(
                name: "OrganizerUserId",
                table: "Tours");
        }
    }
}
