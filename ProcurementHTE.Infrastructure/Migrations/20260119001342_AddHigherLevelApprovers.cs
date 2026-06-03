using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProcurementHTE.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddHigherLevelApprovers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "OperationDirectorUserId",
                table: "Procurements",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PresidentDirectorUserId",
                table: "Procurements",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VicePresidentUserId",
                table: "Procurements",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Procurements_OperationDirectorUserId",
                table: "Procurements",
                column: "OperationDirectorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Procurements_PresidentDirectorUserId",
                table: "Procurements",
                column: "PresidentDirectorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Procurements_VicePresidentUserId",
                table: "Procurements",
                column: "VicePresidentUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Procurements_AspNetUsers_OperationDirectorUserId",
                table: "Procurements",
                column: "OperationDirectorUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Procurements_AspNetUsers_PresidentDirectorUserId",
                table: "Procurements",
                column: "PresidentDirectorUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Procurements_AspNetUsers_VicePresidentUserId",
                table: "Procurements",
                column: "VicePresidentUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Procurements_AspNetUsers_OperationDirectorUserId",
                table: "Procurements");

            migrationBuilder.DropForeignKey(
                name: "FK_Procurements_AspNetUsers_PresidentDirectorUserId",
                table: "Procurements");

            migrationBuilder.DropForeignKey(
                name: "FK_Procurements_AspNetUsers_VicePresidentUserId",
                table: "Procurements");

            migrationBuilder.DropIndex(
                name: "IX_Procurements_OperationDirectorUserId",
                table: "Procurements");

            migrationBuilder.DropIndex(
                name: "IX_Procurements_PresidentDirectorUserId",
                table: "Procurements");

            migrationBuilder.DropIndex(
                name: "IX_Procurements_VicePresidentUserId",
                table: "Procurements");

            migrationBuilder.DropColumn(
                name: "OperationDirectorUserId",
                table: "Procurements");

            migrationBuilder.DropColumn(
                name: "PresidentDirectorUserId",
                table: "Procurements");

            migrationBuilder.DropColumn(
                name: "VicePresidentUserId",
                table: "Procurements");
        }
    }
}
