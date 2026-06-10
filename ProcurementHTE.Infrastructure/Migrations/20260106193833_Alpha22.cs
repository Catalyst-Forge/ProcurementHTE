using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProcurementHTE.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Alpha22 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Procurements_AspNetUsers_PickedUpByUserId",
                table: "Procurements");

            migrationBuilder.DropIndex(
                name: "IX_Procurements_PickedUpByUserId",
                table: "Procurements");

            migrationBuilder.DropColumn(
                name: "PickedUpByUserId",
                table: "Procurements");

            migrationBuilder.CreateIndex(
                name: "IX_Procurements_AppoUserId",
                table: "Procurements",
                column: "AppoUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Procurements_AspNetUsers_AppoUserId",
                table: "Procurements",
                column: "AppoUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Procurements_AspNetUsers_AppoUserId",
                table: "Procurements");

            migrationBuilder.DropIndex(
                name: "IX_Procurements_AppoUserId",
                table: "Procurements");

            migrationBuilder.AddColumn<string>(
                name: "PickedUpByUserId",
                table: "Procurements",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Procurements_PickedUpByUserId",
                table: "Procurements",
                column: "PickedUpByUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Procurements_AspNetUsers_PickedUpByUserId",
                table: "Procurements",
                column: "PickedUpByUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }
    }
}
