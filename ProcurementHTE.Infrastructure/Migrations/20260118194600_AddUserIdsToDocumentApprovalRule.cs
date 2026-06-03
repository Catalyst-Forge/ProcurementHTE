using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProcurementHTE.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUserIdsToDocumentApprovalRule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ApproverUserId",
                table: "DocumentApprovalRules",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SubmitterUserId",
                table: "DocumentApprovalRules",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_DocumentApprovalRules_ApproverUserId",
                table: "DocumentApprovalRules",
                column: "ApproverUserId");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentApprovalRules_SubmitterUserId",
                table: "DocumentApprovalRules",
                column: "SubmitterUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_DocumentApprovalRules_AspNetUsers_ApproverUserId",
                table: "DocumentApprovalRules",
                column: "ApproverUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_DocumentApprovalRules_AspNetUsers_SubmitterUserId",
                table: "DocumentApprovalRules",
                column: "SubmitterUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DocumentApprovalRules_AspNetUsers_ApproverUserId",
                table: "DocumentApprovalRules");

            migrationBuilder.DropForeignKey(
                name: "FK_DocumentApprovalRules_AspNetUsers_SubmitterUserId",
                table: "DocumentApprovalRules");

            migrationBuilder.DropIndex(
                name: "IX_DocumentApprovalRules_ApproverUserId",
                table: "DocumentApprovalRules");

            migrationBuilder.DropIndex(
                name: "IX_DocumentApprovalRules_SubmitterUserId",
                table: "DocumentApprovalRules");

            migrationBuilder.DropColumn(
                name: "ApproverUserId",
                table: "DocumentApprovalRules");

            migrationBuilder.DropColumn(
                name: "SubmitterUserId",
                table: "DocumentApprovalRules");
        }
    }
}
