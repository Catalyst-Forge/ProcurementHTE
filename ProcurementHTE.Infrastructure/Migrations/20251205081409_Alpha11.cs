using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProcurementHTE.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Alpha11 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "VendorRoundLetters",
                columns: table => new
                {
                    VendorRoundLetterId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ProcurementId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    ProfitLossId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    VendorId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Round = table.Column<int>(type: "int", nullable: false),
                    LetterNumber = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    ProcDocumentId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VendorRoundLetters", x => x.VendorRoundLetterId);
                    table.ForeignKey(
                        name: "FK_VendorRoundLetters_ProcDocuments_ProcDocumentId",
                        column: x => x.ProcDocumentId,
                        principalTable: "ProcDocuments",
                        principalColumn: "ProcDocumentId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_VendorRoundLetters_Vendors_VendorId",
                        column: x => x.VendorId,
                        principalTable: "Vendors",
                        principalColumn: "VendorId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_VendorRoundLetters_ProcDocumentId",
                table: "VendorRoundLetters",
                column: "ProcDocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_VendorRoundLetters_ProcurementId_VendorId_Round",
                table: "VendorRoundLetters",
                columns: new[] { "ProcurementId", "VendorId", "Round" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VendorRoundLetters_VendorId",
                table: "VendorRoundLetters",
                column: "VendorId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "VendorRoundLetters");
        }
    }
}
