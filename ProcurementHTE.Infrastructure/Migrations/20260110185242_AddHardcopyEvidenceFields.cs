using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProcurementHTE.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddHardcopyEvidenceFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "HardcopyEvidenceContentType",
                table: "PurchaseRequisitions",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HardcopyEvidenceFileName",
                table: "PurchaseRequisitions",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HardcopyEvidenceFilePath",
                table: "PurchaseRequisitions",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "HardcopyEvidenceFileSize",
                table: "PurchaseRequisitions",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "HardcopySubmittedAt",
                table: "PurchaseRequisitions",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HardcopySubmittedByUserId",
                table: "PurchaseRequisitions",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseRequisitions_HardcopySubmittedByUserId",
                table: "PurchaseRequisitions",
                column: "HardcopySubmittedByUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_PurchaseRequisitions_AspNetUsers_HardcopySubmittedByUserId",
                table: "PurchaseRequisitions",
                column: "HardcopySubmittedByUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PurchaseRequisitions_AspNetUsers_HardcopySubmittedByUserId",
                table: "PurchaseRequisitions");

            migrationBuilder.DropIndex(
                name: "IX_PurchaseRequisitions_HardcopySubmittedByUserId",
                table: "PurchaseRequisitions");

            migrationBuilder.DropColumn(
                name: "HardcopyEvidenceContentType",
                table: "PurchaseRequisitions");

            migrationBuilder.DropColumn(
                name: "HardcopyEvidenceFileName",
                table: "PurchaseRequisitions");

            migrationBuilder.DropColumn(
                name: "HardcopyEvidenceFilePath",
                table: "PurchaseRequisitions");

            migrationBuilder.DropColumn(
                name: "HardcopyEvidenceFileSize",
                table: "PurchaseRequisitions");

            migrationBuilder.DropColumn(
                name: "HardcopySubmittedAt",
                table: "PurchaseRequisitions");

            migrationBuilder.DropColumn(
                name: "HardcopySubmittedByUserId",
                table: "PurchaseRequisitions");
        }
    }
}
