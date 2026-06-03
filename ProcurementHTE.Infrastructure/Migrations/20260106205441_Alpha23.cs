using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProcurementHTE.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Alpha23 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ApprovalSentByUserId",
                table: "PurchaseRequisitions",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ApprovalToken",
                table: "PurchaseRequisitions",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ApprovalTokenGeneratedAt",
                table: "PurchaseRequisitions",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseRequisitions_ApprovalSentByUserId",
                table: "PurchaseRequisitions",
                column: "ApprovalSentByUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_PurchaseRequisitions_AspNetUsers_ApprovalSentByUserId",
                table: "PurchaseRequisitions",
                column: "ApprovalSentByUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PurchaseRequisitions_AspNetUsers_ApprovalSentByUserId",
                table: "PurchaseRequisitions");

            migrationBuilder.DropIndex(
                name: "IX_PurchaseRequisitions_ApprovalSentByUserId",
                table: "PurchaseRequisitions");

            migrationBuilder.DropColumn(
                name: "ApprovalSentByUserId",
                table: "PurchaseRequisitions");

            migrationBuilder.DropColumn(
                name: "ApprovalToken",
                table: "PurchaseRequisitions");

            migrationBuilder.DropColumn(
                name: "ApprovalTokenGeneratedAt",
                table: "PurchaseRequisitions");
        }
    }
}
