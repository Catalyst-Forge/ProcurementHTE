using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProcurementHTE.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Alpha9 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "JobTypeOther",
                table: "Procurements");

            migrationBuilder.AddColumn<string>(
                name: "AssignedApproverId",
                table: "ProcDocumentApprovals",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProcDocumentApprovals_AssignedApproverId",
                table: "ProcDocumentApprovals",
                column: "AssignedApproverId");

            migrationBuilder.AddForeignKey(
                name: "FK_ProcDocumentApprovals_AspNetUsers_AssignedApproverId",
                table: "ProcDocumentApprovals",
                column: "AssignedApproverId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProcDocumentApprovals_AspNetUsers_AssignedApproverId",
                table: "ProcDocumentApprovals");

            migrationBuilder.DropIndex(
                name: "IX_ProcDocumentApprovals_AssignedApproverId",
                table: "ProcDocumentApprovals");

            migrationBuilder.DropColumn(
                name: "AssignedApproverId",
                table: "ProcDocumentApprovals");

            migrationBuilder.AddColumn<string>(
                name: "JobTypeOther",
                table: "Procurements",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true);
        }
    }
}
