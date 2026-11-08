using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProcurementHTE.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPrValueAndLdpArchive : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "PrValue",
                table: "PurchaseRequisitions",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "LdpArchiveRows",
                columns: table => new
                {
                    LdpArchiveRowId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    ImportBatch = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    SourceRow = table.Column<int>(type: "int", nullable: false),
                    SourceKey = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Wo = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    NoSpk = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    VendorName = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    JobName = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    JobType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ContractType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    LtcName = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    YearJob = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    NilaiPnl = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    NilaiAccrual = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    NilaiRealisasi = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    NoSpmp = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    NoPr = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    PrCreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    NoPo = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    PoDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    NoInvoice = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    InvoiceValue = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    NoSa = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    NoSp3 = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    PicPr = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    PicOps = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Keterangan = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ValuesJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ImportedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LdpArchiveRows", x => x.LdpArchiveRowId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LdpArchiveRows_ImportBatch",
                table: "LdpArchiveRows",
                column: "ImportBatch");

            migrationBuilder.CreateIndex(
                name: "IX_LdpArchiveRows_SourceKey",
                table: "LdpArchiveRows",
                column: "SourceKey",
                unique: true,
                filter: "[SourceKey] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LdpArchiveRows");

            migrationBuilder.DropColumn(
                name: "PrValue",
                table: "PurchaseRequisitions");
        }
    }
}
