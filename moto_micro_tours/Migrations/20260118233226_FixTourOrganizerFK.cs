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
            migrationBuilder.DropForeignKey(
                name: "FK_Tours_AspNetUsers_OrganizerId",
                table: "Tours");

            migrationBuilder.DropIndex(
                name: "IX_Tours_OrganizerId",
                table: "Tours");

            migrationBuilder.DropColumn(
                name: "OrganizerId",
                table: "Tours");

            migrationBuilder.AlterColumn<string>(
                name: "OrganizerUserId",
                table: "Tours",
                type: "varchar(255)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "longtext")
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

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

            migrationBuilder.AlterColumn<string>(
                name: "OrganizerUserId",
                table: "Tours",
                type: "longtext",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(255)")
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "OrganizerId",
                table: "Tours",
                type: "varchar(255)",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_Tours_OrganizerId",
                table: "Tours",
                column: "OrganizerId");

            migrationBuilder.AddForeignKey(
                name: "FK_Tours_AspNetUsers_OrganizerId",
                table: "Tours",
                column: "OrganizerId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
