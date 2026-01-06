using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProcurementHTE.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPRTrackingFeatures : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "IspaNumber",
                table: "PurchaseRequisitions",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "IspaSubmittedAt",
                table: "PurchaseRequisitions",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IspaSubmittedByUserId",
                table: "PurchaseRequisitions",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PoNumber",
                table: "PurchaseRequisitions",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PoSubmittedAt",
                table: "PurchaseRequisitions",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PoSubmittedByUserId",
                table: "PurchaseRequisitions",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "RejectedAt",
                table: "PurchaseRequisitions",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RejectedByUserId",
                table: "PurchaseRequisitions",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RejectionNote",
                table: "PurchaseRequisitions",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "PurchaseRequisitions",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "PurchaseRequisitionStatusHistories",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    PrId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    ChangedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ChangedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    Note = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PurchaseRequisitionStatusHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PurchaseRequisitionStatusHistories_AspNetUsers_ChangedByUserId",
                        column: x => x.ChangedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PurchaseRequisitionStatusHistories_PurchaseRequisitions_PrId",
                        column: x => x.PrId,
                        principalTable: "PurchaseRequisitions",
                        principalColumn: "PrId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseRequisitions_IspaSubmittedByUserId",
                table: "PurchaseRequisitions",
                column: "IspaSubmittedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseRequisitions_PoSubmittedByUserId",
                table: "PurchaseRequisitions",
                column: "PoSubmittedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseRequisitions_RejectedByUserId",
                table: "PurchaseRequisitions",
                column: "RejectedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseRequisitionStatusHistories_ChangedByUserId",
                table: "PurchaseRequisitionStatusHistories",
                column: "ChangedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseRequisitionStatusHistories_PrId",
                table: "PurchaseRequisitionStatusHistories",
                column: "PrId");

            migrationBuilder.AddForeignKey(
                name: "FK_PurchaseRequisitions_AspNetUsers_IspaSubmittedByUserId",
                table: "PurchaseRequisitions",
                column: "IspaSubmittedByUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_PurchaseRequisitions_AspNetUsers_PoSubmittedByUserId",
                table: "PurchaseRequisitions",
                column: "PoSubmittedByUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_PurchaseRequisitions_AspNetUsers_RejectedByUserId",
                table: "PurchaseRequisitions",
                column: "RejectedByUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PurchaseRequisitions_AspNetUsers_IspaSubmittedByUserId",
                table: "PurchaseRequisitions");

            migrationBuilder.DropForeignKey(
                name: "FK_PurchaseRequisitions_AspNetUsers_PoSubmittedByUserId",
                table: "PurchaseRequisitions");

            migrationBuilder.DropForeignKey(
                name: "FK_PurchaseRequisitions_AspNetUsers_RejectedByUserId",
                table: "PurchaseRequisitions");

            migrationBuilder.DropTable(
                name: "PurchaseRequisitionStatusHistories");

            migrationBuilder.DropIndex(
                name: "IX_PurchaseRequisitions_IspaSubmittedByUserId",
                table: "PurchaseRequisitions");

            migrationBuilder.DropIndex(
                name: "IX_PurchaseRequisitions_PoSubmittedByUserId",
                table: "PurchaseRequisitions");

            migrationBuilder.DropIndex(
                name: "IX_PurchaseRequisitions_RejectedByUserId",
                table: "PurchaseRequisitions");

            migrationBuilder.DropColumn(
                name: "IspaNumber",
                table: "PurchaseRequisitions");

            migrationBuilder.DropColumn(
                name: "IspaSubmittedAt",
                table: "PurchaseRequisitions");

            migrationBuilder.DropColumn(
                name: "IspaSubmittedByUserId",
                table: "PurchaseRequisitions");

            migrationBuilder.DropColumn(
                name: "PoNumber",
                table: "PurchaseRequisitions");

            migrationBuilder.DropColumn(
                name: "PoSubmittedAt",
                table: "PurchaseRequisitions");

            migrationBuilder.DropColumn(
                name: "PoSubmittedByUserId",
                table: "PurchaseRequisitions");

            migrationBuilder.DropColumn(
                name: "RejectedAt",
                table: "PurchaseRequisitions");

            migrationBuilder.DropColumn(
                name: "RejectedByUserId",
                table: "PurchaseRequisitions");

            migrationBuilder.DropColumn(
                name: "RejectionNote",
                table: "PurchaseRequisitions");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "PurchaseRequisitions");
        }
    }
}
