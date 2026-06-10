using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace ProcurementHTE.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Beta2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                IF OBJECT_ID(N'[Tenders]', N'U') IS NOT NULL
                    DROP TABLE [Tenders];
                """
            );

            migrationBuilder.DropColumn(
                name: "Quantity",
                table: "VendorOffers");

            migrationBuilder.DropColumn(
                name: "SequenceOrder",
                table: "ProcDocumentApprovals");

            migrationBuilder.DropColumn(
                name: "SequenceOrder",
                table: "DocumentApprovals");

            migrationBuilder.RenameColumn(
                name: "Trip",
                table: "VendorOffers",
                newName: "QuantityItem");

            migrationBuilder.RenameColumn(
                name: "TarifAwal",
                table: "ProfitLossItems",
                newName: "BasePrice");

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "Vendors",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "Vendors",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Vendors",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AlterColumn<string>(
                name: "NoLetter",
                table: "VendorOffers",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(128)",
                oldMaxLength: 128);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "VendorOffers",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "VendorOffers",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "VendorOffers",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "QuantityOfUnit",
                table: "VendorOffers",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "UnitTypeId",
                table: "VendorOffers",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<decimal>(
                name: "TarifAdd",
                table: "ProfitLossItems",
                type: "decimal(18,2)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)");

            migrationBuilder.AlterColumn<decimal>(
                name: "Quantity",
                table: "ProfitLossItems",
                type: "decimal(18,2)",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<decimal>(
                name: "OperatorCost",
                table: "ProfitLossItems",
                type: "decimal(18,2)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)");

            migrationBuilder.AlterColumn<decimal>(
                name: "KmPer25",
                table: "ProfitLossItems",
                type: "decimal(18,2)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)");

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "ProfitLossItems",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "ItemName",
                table: "ProfitLossItems",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "SortOrder",
                table: "ProfitLossItems",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "UnitQty",
                table: "ProfitLossItems",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "UnitTypeId",
                table: "ProfitLossItems",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "Distance",
                table: "ProfitLosses",
                type: "decimal(18,2)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)");

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "ProfitLosses",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "ProfitLosses",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DurasiTotal",
                table: "ProfitLosses",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "ProfitLosses",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "TglMulaiMoving",
                table: "ProfitLosses",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "TglMulaiSewa",
                table: "ProfitLosses",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "JobTypeId",
                table: "Procurements",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AppoUserId",
                table: "Procurements",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "Procurements",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "Procurements",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DocumentDate",
                table: "Procurements",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Procurements",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "PrId",
                table: "Procurements",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProcurementCategory",
                table: "Procurements",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "UnitRevenue",
                table: "ProcOffers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "ProcDocuments",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "ProcDocuments",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "ProcDocuments",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "ProcurementCategory",
                table: "JobTypeDocuments",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "DocumentApprovalRules",
                columns: table => new
                {
                    DocumentApprovalRuleId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    DocumentTypeId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    JobTypeId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    ProcurementCategory = table.Column<int>(type: "int", nullable: true),
                    MinAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    MaxAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    SubmitterRoleId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ApproverRoleId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Sequence = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentApprovalRules", x => x.DocumentApprovalRuleId);
                    table.ForeignKey(
                        name: "FK_DocumentApprovalRules_DocumentTypes_DocumentTypeId",
                        column: x => x.DocumentTypeId,
                        principalTable: "DocumentTypes",
                        principalColumn: "DocumentTypeId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DocumentApprovalRules_JobTypes_JobTypeId",
                        column: x => x.JobTypeId,
                        principalTable: "JobTypes",
                        principalColumn: "JobTypeId");
                });

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
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    IspaNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IspaSubmittedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IspaSubmittedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    PoNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    PoSubmittedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PoSubmittedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    RejectionNote = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    RejectedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RejectedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true)
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
                    table.ForeignKey(
                        name: "FK_PurchaseRequisitions_AspNetUsers_IspaSubmittedByUserId",
                        column: x => x.IspaSubmittedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PurchaseRequisitions_AspNetUsers_PoSubmittedByUserId",
                        column: x => x.PoSubmittedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PurchaseRequisitions_AspNetUsers_RejectedByUserId",
                        column: x => x.RejectedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "UnitTypes",
                columns: table => new
                {
                    UnitTypeId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UnitTypes", x => x.UnitTypeId);
                });

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

            migrationBuilder.InsertData(
                table: "UnitTypes",
                columns: new[] { "UnitTypeId", "Code", "CreatedAt", "IsActive", "Name", "SortOrder" },
                values: new object[,]
                {
                    { "11111111-1111-1111-1111-111111111111", "HARI", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "Hari", 1 },
                    { "22222222-2222-2222-2222-222222222222", "JAM", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "Jam", 2 },
                    { "33333333-3333-3333-3333-333333333333", "LSP", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "Lumpsum", 3 },
                    { "44444444-4444-4444-4444-444444444444", "TRIP", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "Trip", 4 },
                    { "55555555-5555-5555-5555-555555555555", "KALI", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "Kali", 5 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_VendorOffers_UnitTypeId",
                table: "VendorOffers",
                column: "UnitTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_ProfitLossItems_UnitTypeId",
                table: "ProfitLossItems",
                column: "UnitTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_Procurements_PrId",
                table: "Procurements",
                column: "PrId");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentApprovalRules_DocumentTypeId_JobTypeId_ProcurementCategory_MinAmount_MaxAmount_IsActive",
                table: "DocumentApprovalRules",
                columns: new[] { "DocumentTypeId", "JobTypeId", "ProcurementCategory", "MinAmount", "MaxAmount", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_DocumentApprovalRules_JobTypeId",
                table: "DocumentApprovalRules",
                column: "JobTypeId");

            migrationBuilder.CreateIndex(
                name: "AK_PurchaseRequisitions_PrNumber",
                table: "PurchaseRequisitions",
                column: "PrNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseRequisitions_CreatedByUserId",
                table: "PurchaseRequisitions",
                column: "CreatedByUserId");

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

            migrationBuilder.CreateIndex(
                name: "IX_UnitTypes_Code",
                table: "UnitTypes",
                column: "Code",
                unique: true);

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

            migrationBuilder.AddForeignKey(
                name: "FK_Procurements_PurchaseRequisitions_PrId",
                table: "Procurements",
                column: "PrId",
                principalTable: "PurchaseRequisitions",
                principalColumn: "PrId",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_ProfitLossItems_UnitTypes_UnitTypeId",
                table: "ProfitLossItems",
                column: "UnitTypeId",
                principalTable: "UnitTypes",
                principalColumn: "UnitTypeId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_VendorOffers_UnitTypes_UnitTypeId",
                table: "VendorOffers",
                column: "UnitTypeId",
                principalTable: "UnitTypes",
                principalColumn: "UnitTypeId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Procurements_PurchaseRequisitions_PrId",
                table: "Procurements");

            migrationBuilder.DropForeignKey(
                name: "FK_ProfitLossItems_UnitTypes_UnitTypeId",
                table: "ProfitLossItems");

            migrationBuilder.DropForeignKey(
                name: "FK_VendorOffers_UnitTypes_UnitTypeId",
                table: "VendorOffers");

            migrationBuilder.DropTable(
                name: "DocumentApprovalRules");

            migrationBuilder.DropTable(
                name: "PurchaseRequisitionStatusHistories");

            migrationBuilder.DropTable(
                name: "UnitTypes");

            migrationBuilder.DropTable(
                name: "VendorRoundLetters");

            migrationBuilder.DropTable(
                name: "PurchaseRequisitions");

            migrationBuilder.DropIndex(
                name: "IX_VendorOffers_UnitTypeId",
                table: "VendorOffers");

            migrationBuilder.DropIndex(
                name: "IX_ProfitLossItems_UnitTypeId",
                table: "ProfitLossItems");

            migrationBuilder.DropIndex(
                name: "IX_Procurements_PrId",
                table: "Procurements");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "Vendors");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "Vendors");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Vendors");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "VendorOffers");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "VendorOffers");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "VendorOffers");

            migrationBuilder.DropColumn(
                name: "QuantityOfUnit",
                table: "VendorOffers");

            migrationBuilder.DropColumn(
                name: "UnitTypeId",
                table: "VendorOffers");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "ProfitLossItems");

            migrationBuilder.DropColumn(
                name: "ItemName",
                table: "ProfitLossItems");

            migrationBuilder.DropColumn(
                name: "SortOrder",
                table: "ProfitLossItems");

            migrationBuilder.DropColumn(
                name: "UnitQty",
                table: "ProfitLossItems");

            migrationBuilder.DropColumn(
                name: "UnitTypeId",
                table: "ProfitLossItems");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "ProfitLosses");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "ProfitLosses");

            migrationBuilder.DropColumn(
                name: "DurasiTotal",
                table: "ProfitLosses");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "ProfitLosses");

            migrationBuilder.DropColumn(
                name: "TglMulaiMoving",
                table: "ProfitLosses");

            migrationBuilder.DropColumn(
                name: "TglMulaiSewa",
                table: "ProfitLosses");

            migrationBuilder.DropColumn(
                name: "AppoUserId",
                table: "Procurements");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "Procurements");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "Procurements");

            migrationBuilder.DropColumn(
                name: "DocumentDate",
                table: "Procurements");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Procurements");

            migrationBuilder.DropColumn(
                name: "PrId",
                table: "Procurements");

            migrationBuilder.DropColumn(
                name: "ProcurementCategory",
                table: "Procurements");

            migrationBuilder.DropColumn(
                name: "UnitRevenue",
                table: "ProcOffers");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "ProcDocuments");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "ProcDocuments");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "ProcDocuments");

            migrationBuilder.DropColumn(
                name: "ProcurementCategory",
                table: "JobTypeDocuments");

            migrationBuilder.RenameColumn(
                name: "QuantityItem",
                table: "VendorOffers",
                newName: "Trip");

            migrationBuilder.RenameColumn(
                name: "BasePrice",
                table: "ProfitLossItems",
                newName: "TarifAwal");

            migrationBuilder.AlterColumn<string>(
                name: "NoLetter",
                table: "VendorOffers",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(255)",
                oldMaxLength: 255,
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Quantity",
                table: "VendorOffers",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<decimal>(
                name: "TarifAdd",
                table: "ProfitLossItems",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "Quantity",
                table: "ProfitLossItems",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "OperatorCost",
                table: "ProfitLossItems",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "KmPer25",
                table: "ProfitLossItems",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "Distance",
                table: "ProfitLosses",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "JobTypeId",
                table: "Procurements",
                type: "nvarchar(450)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AddColumn<int>(
                name: "SequenceOrder",
                table: "ProcDocumentApprovals",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "SequenceOrder",
                table: "DocumentApprovals",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "Tenders",
                columns: table => new
                {
                    TenderId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Information = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Price = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TenderName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tenders", x => x.TenderId);
                });
        }
    }
}
