using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProcurementHTE.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Alpha15 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PrId",
                table: "Procurements",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "PurchaseRequisitions",
                columns: table => new
                {
                    PrId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    PrNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    RequestDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    DocumentFileName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    DocumentFilePath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    DocumentContentType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    DocumentFileSize = table.Column<long>(type: "bigint", nullable: true),
                    CreatedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PurchaseRequisitions", x => x.PrId);
                    table.ForeignKey(
                        name: "FK_PurchaseRequisitions_AspNetUsers_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Procurements_PrId",
                table: "Procurements",
                column: "PrId");

            migrationBuilder.CreateIndex(
                name: "AK_PurchaseRequisitions_PrNumber",
                table: "PurchaseRequisitions",
                column: "PrNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseRequisitions_CreatedByUserId",
                table: "PurchaseRequisitions",
                column: "CreatedByUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Procurements_PurchaseRequisitions_PrId",
                table: "Procurements",
                column: "PrId",
                principalTable: "PurchaseRequisitions",
                principalColumn: "PrId",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Procurements_PurchaseRequisitions_PrId",
                table: "Procurements");

            migrationBuilder.DropTable(
                name: "PurchaseRequisitions");

            migrationBuilder.DropIndex(
                name: "IX_Procurements_PrId",
                table: "Procurements");

            migrationBuilder.DropColumn(
                name: "PrId",
                table: "Procurements");
        }
    }
}
