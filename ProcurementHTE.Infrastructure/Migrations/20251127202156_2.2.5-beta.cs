using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProcurementHTE.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class _225beta : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DocumentApprovals_WoTypesDocuments_WoTypeDocumentId",
                table: "DocumentApprovals");

            migrationBuilder.DropForeignKey(
                name: "FK_ProfitLosses_WorkOrders_WorkOrderId",
                table: "ProfitLosses");

            migrationBuilder.DropForeignKey(
                name: "FK_ProfitLossItems_ProfitLosses_ProfitLossId",
                table: "ProfitLossItems");

            migrationBuilder.DropForeignKey(
                name: "FK_ProfitLossItems_WoOffers_WoOfferId",
                table: "ProfitLossItems");

            migrationBuilder.DropForeignKey(
                name: "FK_VendorOffers_WoOffers_WoOfferId",
                table: "VendorOffers");

            migrationBuilder.DropForeignKey(
                name: "FK_VendorOffers_WorkOrders_WorkOrderId",
                table: "VendorOffers");

            migrationBuilder.DropTable(
                name: "WoDetails");

            migrationBuilder.DropTable(
                name: "WoDocumentApprovals");

            migrationBuilder.DropTable(
                name: "WoOffers");

            migrationBuilder.DropTable(
                name: "WoTypesDocuments");

            migrationBuilder.DropTable(
                name: "WoDocuments");

            migrationBuilder.DropTable(
                name: "WorkOrders");

            migrationBuilder.DropTable(
                name: "WoTypes");

            migrationBuilder.DropColumn(
                name: "WorkOrderId",
                table: "ProfitLossSelectedVendors");

            migrationBuilder.RenameColumn(
                name: "WorkOrderId",
                table: "VendorOffers",
                newName: "ProfitLossId");

            migrationBuilder.RenameColumn(
                name: "WoOfferId",
                table: "VendorOffers",
                newName: "ProcurementId");

            migrationBuilder.RenameIndex(
                name: "IX_VendorOffers_WorkOrderId",
                table: "VendorOffers",
                newName: "IX_VendorOffers_ProfitLossId");

            migrationBuilder.RenameIndex(
                name: "IX_VendorOffers_WoOfferId",
                table: "VendorOffers",
                newName: "IX_VendorOffers_ProcurementId");

            migrationBuilder.RenameColumn(
                name: "WoOfferId",
                table: "ProfitLossItems",
                newName: "ProcOfferId");

            migrationBuilder.RenameIndex(
                name: "IX_ProfitLossItems_WoOfferId",
                table: "ProfitLossItems",
                newName: "IX_ProfitLossItems_ProcOfferId");

            migrationBuilder.RenameColumn(
                name: "WorkOrderId",
                table: "ProfitLosses",
                newName: "ProcurementId");

            migrationBuilder.RenameIndex(
                name: "IX_ProfitLosses_WorkOrderId",
                table: "ProfitLosses",
                newName: "IX_ProfitLosses_ProcurementId");

            migrationBuilder.RenameColumn(
                name: "WoTypeDocumentId",
                table: "DocumentApprovals",
                newName: "JobTypeDocumentId");

            migrationBuilder.RenameIndex(
                name: "IX_DocumentApprovals_WoTypeDocumentId",
                table: "DocumentApprovals",
                newName: "IX_DocumentApprovals_JobTypeDocumentId");

            migrationBuilder.AlterColumn<string>(
                name: "NoLetter",
                table: "VendorOffers",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(128)",
                oldMaxLength: 128,
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProcOfferId",
                table: "VendorOffers",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "Quantity",
                table: "VendorOffers",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Trip",
                table: "VendorOffers",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<string>(
                name: "VendorId",
                table: "ProfitLossSelectedVendors",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<string>(
                name: "ProcurementId",
                table: "ProfitLossSelectedVendors",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ProfitLossId",
                table: "ProfitLossSelectedVendors",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "KmPer25",
                table: "ProfitLossItems",
                type: "decimal(18,2)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<int>(
                name: "Quantity",
                table: "ProfitLossItems",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "AccrualAmount",
                table: "ProfitLosses",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Distance",
                table: "ProfitLosses",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "NoLetterSelectedVendor",
                table: "ProfitLosses",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "RealizationAmount",
                table: "ProfitLosses",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AvatarFileName",
                table: "AspNetUsers",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AvatarObjectKey",
                table: "AspNetUsers",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "AvatarUpdatedAt",
                table: "AspNetUsers",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "JobTitle",
                table: "AspNetUsers",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PasswordChangedAt",
                table: "AspNetUsers",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "RecoveryCodesGeneratedAt",
                table: "AspNetUsers",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "RecoveryCodesHidden",
                table: "AspNetUsers",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "RecoveryCodesJson",
                table: "AspNetUsers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TwoFactorMethod",
                table: "AspNetUsers",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "None");

            migrationBuilder.CreateTable(
                name: "JobTypes",
                columns: table => new
                {
                    JobTypeId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    TypeName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JobTypes", x => x.JobTypeId);
                });

            migrationBuilder.CreateTable(
                name: "UserSecurityLogs",
                columns: table => new
                {
                    UserSecurityLogId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    EventType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    IsSuccess = table.Column<bool>(type: "bit", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    IpAddress = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    UserAgent = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserSecurityLogs", x => x.UserSecurityLogId);
                    table.ForeignKey(
                        name: "FK_UserSecurityLogs_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserSessions",
                columns: table => new
                {
                    UserSessionId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Device = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    Browser = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    UserAgent = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    IpAddress = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    Location = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    LastAccessedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsCurrent = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserSessions", x => x.UserSessionId);
                    table.ForeignKey(
                        name: "FK_UserSessions_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "JobTypeDocuments",
                columns: table => new
                {
                    JobTypeDocumentId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    IsMandatory = table.Column<bool>(type: "bit", nullable: false),
                    Sequence = table.Column<int>(type: "int", nullable: false),
                    IsGenerated = table.Column<bool>(type: "bit", nullable: false),
                    IsUploadRequired = table.Column<bool>(type: "bit", nullable: false),
                    RequiresApproval = table.Column<bool>(type: "bit", nullable: false),
                    Note = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    JobTypeId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    DocumentTypeId = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JobTypeDocuments", x => x.JobTypeDocumentId);
                    table.ForeignKey(
                        name: "FK_JobTypeDocuments_DocumentTypes_DocumentTypeId",
                        column: x => x.DocumentTypeId,
                        principalTable: "DocumentTypes",
                        principalColumn: "DocumentTypeId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_JobTypeDocuments_JobTypes_JobTypeId",
                        column: x => x.JobTypeId,
                        principalTable: "JobTypes",
                        principalColumn: "JobTypeId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Procurements",
                columns: table => new
                {
                    ProcurementId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ProcNum = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SpkNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Wonum = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ContractType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    JobName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ProjectRegion = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    PotentialAccrualDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SpmpNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    MemoNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    OeNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    RaNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ProjectCode = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    LtcName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    Note = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    PicOpsUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    AnalystHteUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    AssistantManagerUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    ManagerUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    JobTypeId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    StatusId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Procurements", x => x.ProcurementId);
                    table.ForeignKey(
                        name: "FK_Procurements_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Procurements_JobTypes_JobTypeId",
                        column: x => x.JobTypeId,
                        principalTable: "JobTypes",
                        principalColumn: "JobTypeId");
                    table.ForeignKey(
                        name: "FK_Procurements_Statuses_StatusId",
                        column: x => x.StatusId,
                        principalTable: "Statuses",
                        principalColumn: "StatusId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ProcDetails",
                columns: table => new
                {
                    ProcDetailId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ItemName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    Quantity = table.Column<int>(type: "int", nullable: true),
                    Unit = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    DetailKind = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    VendorId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    ProcurementId = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProcDetails", x => x.ProcDetailId);
                    table.ForeignKey(
                        name: "FK_ProcDetails_Procurements_ProcurementId",
                        column: x => x.ProcurementId,
                        principalTable: "Procurements",
                        principalColumn: "ProcurementId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProcDetails_Vendors_VendorId",
                        column: x => x.VendorId,
                        principalTable: "Vendors",
                        principalColumn: "VendorId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ProcDocuments",
                columns: table => new
                {
                    ProcDocumentId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ProcurementId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    DocumentTypeId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    ObjectKey = table.Column<string>(type: "nvarchar(600)", maxLength: 600, nullable: false),
                    ContentType = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Size = table.Column<long>(type: "bigint", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    QrText = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    QrObjectKey = table.Column<string>(type: "nvarchar(600)", maxLength: 600, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    IsApproved = table.Column<bool>(type: "bit", nullable: true),
                    ApprovedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ApprovedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProcDocuments", x => x.ProcDocumentId);
                    table.ForeignKey(
                        name: "FK_ProcDocuments_DocumentTypes_DocumentTypeId",
                        column: x => x.DocumentTypeId,
                        principalTable: "DocumentTypes",
                        principalColumn: "DocumentTypeId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProcDocuments_Procurements_ProcurementId",
                        column: x => x.ProcurementId,
                        principalTable: "Procurements",
                        principalColumn: "ProcurementId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProcOffers",
                columns: table => new
                {
                    ProcOfferId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ItemPenawaran = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Qty = table.Column<int>(type: "int", nullable: false),
                    Unit = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ProcurementId = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProcOffers", x => x.ProcOfferId);
                    table.ForeignKey(
                        name: "FK_ProcOffers_Procurements_ProcurementId",
                        column: x => x.ProcurementId,
                        principalTable: "Procurements",
                        principalColumn: "ProcurementId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProcDocumentApprovals",
                columns: table => new
                {
                    ProcDocumentApprovalId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ProcurementId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ProcDocumentId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    RoleId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    AssignedApproverId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    ApproverId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    Level = table.Column<int>(type: "int", nullable: false),
                    SequenceOrder = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    ApprovedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Note = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProcDocumentApprovals", x => x.ProcDocumentApprovalId);
                    table.ForeignKey(
                        name: "FK_ProcDocumentApprovals_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProcDocumentApprovals_AspNetUsers_ApproverId",
                        column: x => x.ApproverId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProcDocumentApprovals_AspNetUsers_AssignedApproverId",
                        column: x => x.AssignedApproverId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProcDocumentApprovals_ProcDocuments_ProcDocumentId",
                        column: x => x.ProcDocumentId,
                        principalTable: "ProcDocuments",
                        principalColumn: "ProcDocumentId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProcDocumentApprovals_Procurements_ProcurementId",
                        column: x => x.ProcurementId,
                        principalTable: "Procurements",
                        principalColumn: "ProcurementId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_VendorOffers_ProcOfferId",
                table: "VendorOffers",
                column: "ProcOfferId");

            migrationBuilder.CreateIndex(
                name: "IX_ProfitLossSelectedVendors_ProcurementId",
                table: "ProfitLossSelectedVendors",
                column: "ProcurementId");

            migrationBuilder.CreateIndex(
                name: "IX_ProfitLossSelectedVendors_ProfitLossId",
                table: "ProfitLossSelectedVendors",
                column: "ProfitLossId");

            migrationBuilder.CreateIndex(
                name: "IX_ProfitLossSelectedVendors_VendorId",
                table: "ProfitLossSelectedVendors",
                column: "VendorId");

            migrationBuilder.CreateIndex(
                name: "IX_JobTypeDocuments_DocumentTypeId",
                table: "JobTypeDocuments",
                column: "DocumentTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_JobTypeDocuments_JobTypeId",
                table: "JobTypeDocuments",
                column: "JobTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_ProcDetails_ProcurementId",
                table: "ProcDetails",
                column: "ProcurementId");

            migrationBuilder.CreateIndex(
                name: "IX_ProcDetails_VendorId",
                table: "ProcDetails",
                column: "VendorId");

            migrationBuilder.CreateIndex(
                name: "IX_ProcDocumentApprovals_ApproverId",
                table: "ProcDocumentApprovals",
                column: "ApproverId");

            migrationBuilder.CreateIndex(
                name: "IX_ProcDocumentApprovals_AssignedApproverId",
                table: "ProcDocumentApprovals",
                column: "AssignedApproverId");

            migrationBuilder.CreateIndex(
                name: "IX_ProcDocumentApprovals_ProcDocumentId",
                table: "ProcDocumentApprovals",
                column: "ProcDocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_ProcDocumentApprovals_ProcurementId",
                table: "ProcDocumentApprovals",
                column: "ProcurementId");

            migrationBuilder.CreateIndex(
                name: "IX_ProcDocumentApprovals_RoleId",
                table: "ProcDocumentApprovals",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_ProcDocuments_DocumentTypeId",
                table: "ProcDocuments",
                column: "DocumentTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_ProcDocuments_ProcurementId",
                table: "ProcDocuments",
                column: "ProcurementId");

            migrationBuilder.CreateIndex(
                name: "IX_ProcOffers_ProcurementId",
                table: "ProcOffers",
                column: "ProcurementId");

            migrationBuilder.CreateIndex(
                name: "AK_Procurements_ProcNum",
                table: "Procurements",
                column: "ProcNum",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Procurements_JobTypeId",
                table: "Procurements",
                column: "JobTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_Procurements_StatusId",
                table: "Procurements",
                column: "StatusId");

            migrationBuilder.CreateIndex(
                name: "IX_Procurements_UserId_CreatedAt",
                table: "Procurements",
                columns: new[] { "UserId", "CreatedAt" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "IX_UserSecurityLogs_UserId",
                table: "UserSecurityLogs",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserSessions_UserId",
                table: "UserSessions",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_DocumentApprovals_JobTypeDocuments_JobTypeDocumentId",
                table: "DocumentApprovals",
                column: "JobTypeDocumentId",
                principalTable: "JobTypeDocuments",
                principalColumn: "JobTypeDocumentId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ProfitLosses_Procurements_ProcurementId",
                table: "ProfitLosses",
                column: "ProcurementId",
                principalTable: "Procurements",
                principalColumn: "ProcurementId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ProfitLossItems_ProcOffers_ProcOfferId",
                table: "ProfitLossItems",
                column: "ProcOfferId",
                principalTable: "ProcOffers",
                principalColumn: "ProcOfferId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ProfitLossItems_ProfitLosses_ProfitLossId",
                table: "ProfitLossItems",
                column: "ProfitLossId",
                principalTable: "ProfitLosses",
                principalColumn: "ProfitLossId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ProfitLossSelectedVendors_Procurements_ProcurementId",
                table: "ProfitLossSelectedVendors",
                column: "ProcurementId",
                principalTable: "Procurements",
                principalColumn: "ProcurementId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ProfitLossSelectedVendors_ProfitLosses_ProfitLossId",
                table: "ProfitLossSelectedVendors",
                column: "ProfitLossId",
                principalTable: "ProfitLosses",
                principalColumn: "ProfitLossId");

            migrationBuilder.AddForeignKey(
                name: "FK_ProfitLossSelectedVendors_Vendors_VendorId",
                table: "ProfitLossSelectedVendors",
                column: "VendorId",
                principalTable: "Vendors",
                principalColumn: "VendorId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_VendorOffers_ProcOffers_ProcOfferId",
                table: "VendorOffers",
                column: "ProcOfferId",
                principalTable: "ProcOffers",
                principalColumn: "ProcOfferId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_VendorOffers_Procurements_ProcurementId",
                table: "VendorOffers",
                column: "ProcurementId",
                principalTable: "Procurements",
                principalColumn: "ProcurementId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_VendorOffers_ProfitLosses_ProfitLossId",
                table: "VendorOffers",
                column: "ProfitLossId",
                principalTable: "ProfitLosses",
                principalColumn: "ProfitLossId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DocumentApprovals_JobTypeDocuments_JobTypeDocumentId",
                table: "DocumentApprovals");

            migrationBuilder.DropForeignKey(
                name: "FK_ProfitLosses_Procurements_ProcurementId",
                table: "ProfitLosses");

            migrationBuilder.DropForeignKey(
                name: "FK_ProfitLossItems_ProcOffers_ProcOfferId",
                table: "ProfitLossItems");

            migrationBuilder.DropForeignKey(
                name: "FK_ProfitLossItems_ProfitLosses_ProfitLossId",
                table: "ProfitLossItems");

            migrationBuilder.DropForeignKey(
                name: "FK_ProfitLossSelectedVendors_Procurements_ProcurementId",
                table: "ProfitLossSelectedVendors");

            migrationBuilder.DropForeignKey(
                name: "FK_ProfitLossSelectedVendors_ProfitLosses_ProfitLossId",
                table: "ProfitLossSelectedVendors");

            migrationBuilder.DropForeignKey(
                name: "FK_ProfitLossSelectedVendors_Vendors_VendorId",
                table: "ProfitLossSelectedVendors");

            migrationBuilder.DropForeignKey(
                name: "FK_VendorOffers_ProcOffers_ProcOfferId",
                table: "VendorOffers");

            migrationBuilder.DropForeignKey(
                name: "FK_VendorOffers_Procurements_ProcurementId",
                table: "VendorOffers");

            migrationBuilder.DropForeignKey(
                name: "FK_VendorOffers_ProfitLosses_ProfitLossId",
                table: "VendorOffers");

            migrationBuilder.DropTable(
                name: "JobTypeDocuments");

            migrationBuilder.DropTable(
                name: "ProcDetails");

            migrationBuilder.DropTable(
                name: "ProcDocumentApprovals");

            migrationBuilder.DropTable(
                name: "ProcOffers");

            migrationBuilder.DropTable(
                name: "UserSecurityLogs");

            migrationBuilder.DropTable(
                name: "UserSessions");

            migrationBuilder.DropTable(
                name: "ProcDocuments");

            migrationBuilder.DropTable(
                name: "Procurements");

            migrationBuilder.DropTable(
                name: "JobTypes");

            migrationBuilder.DropIndex(
                name: "IX_VendorOffers_ProcOfferId",
                table: "VendorOffers");

            migrationBuilder.DropIndex(
                name: "IX_ProfitLossSelectedVendors_ProcurementId",
                table: "ProfitLossSelectedVendors");

            migrationBuilder.DropIndex(
                name: "IX_ProfitLossSelectedVendors_ProfitLossId",
                table: "ProfitLossSelectedVendors");

            migrationBuilder.DropIndex(
                name: "IX_ProfitLossSelectedVendors_VendorId",
                table: "ProfitLossSelectedVendors");

            migrationBuilder.DropColumn(
                name: "ProcOfferId",
                table: "VendorOffers");

            migrationBuilder.DropColumn(
                name: "Quantity",
                table: "VendorOffers");

            migrationBuilder.DropColumn(
                name: "Trip",
                table: "VendorOffers");

            migrationBuilder.DropColumn(
                name: "ProcurementId",
                table: "ProfitLossSelectedVendors");

            migrationBuilder.DropColumn(
                name: "ProfitLossId",
                table: "ProfitLossSelectedVendors");

            migrationBuilder.DropColumn(
                name: "Quantity",
                table: "ProfitLossItems");

            migrationBuilder.DropColumn(
                name: "AccrualAmount",
                table: "ProfitLosses");

            migrationBuilder.DropColumn(
                name: "Distance",
                table: "ProfitLosses");

            migrationBuilder.DropColumn(
                name: "NoLetterSelectedVendor",
                table: "ProfitLosses");

            migrationBuilder.DropColumn(
                name: "RealizationAmount",
                table: "ProfitLosses");

            migrationBuilder.DropColumn(
                name: "AvatarFileName",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "AvatarObjectKey",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "AvatarUpdatedAt",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "JobTitle",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "PasswordChangedAt",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "RecoveryCodesGeneratedAt",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "RecoveryCodesHidden",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "RecoveryCodesJson",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "TwoFactorMethod",
                table: "AspNetUsers");

            migrationBuilder.RenameColumn(
                name: "ProfitLossId",
                table: "VendorOffers",
                newName: "WorkOrderId");

            migrationBuilder.RenameColumn(
                name: "ProcurementId",
                table: "VendorOffers",
                newName: "WoOfferId");

            migrationBuilder.RenameIndex(
                name: "IX_VendorOffers_ProfitLossId",
                table: "VendorOffers",
                newName: "IX_VendorOffers_WorkOrderId");

            migrationBuilder.RenameIndex(
                name: "IX_VendorOffers_ProcurementId",
                table: "VendorOffers",
                newName: "IX_VendorOffers_WoOfferId");

            migrationBuilder.RenameColumn(
                name: "ProcOfferId",
                table: "ProfitLossItems",
                newName: "WoOfferId");

            migrationBuilder.RenameIndex(
                name: "IX_ProfitLossItems_ProcOfferId",
                table: "ProfitLossItems",
                newName: "IX_ProfitLossItems_WoOfferId");

            migrationBuilder.RenameColumn(
                name: "ProcurementId",
                table: "ProfitLosses",
                newName: "WorkOrderId");

            migrationBuilder.RenameIndex(
                name: "IX_ProfitLosses_ProcurementId",
                table: "ProfitLosses",
                newName: "IX_ProfitLosses_WorkOrderId");

            migrationBuilder.RenameColumn(
                name: "JobTypeDocumentId",
                table: "DocumentApprovals",
                newName: "WoTypeDocumentId");

            migrationBuilder.RenameIndex(
                name: "IX_DocumentApprovals_JobTypeDocumentId",
                table: "DocumentApprovals",
                newName: "IX_DocumentApprovals_WoTypeDocumentId");

            migrationBuilder.AlterColumn<string>(
                name: "NoLetter",
                table: "VendorOffers",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(128)",
                oldMaxLength: 128);

            migrationBuilder.AlterColumn<string>(
                name: "VendorId",
                table: "ProfitLossSelectedVendors",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AddColumn<string>(
                name: "WorkOrderId",
                table: "ProfitLossSelectedVendors",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<int>(
                name: "KmPer25",
                table: "ProfitLossItems",
                type: "int",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)");

            migrationBuilder.CreateTable(
                name: "WoTypes",
                columns: table => new
                {
                    WoTypeId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TypeName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WoTypes", x => x.WoTypeId);
                });

            migrationBuilder.CreateTable(
                name: "WorkOrders",
                columns: table => new
                {
                    WorkOrderId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    StatusId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    WoTypeId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    Approved = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DateLetter = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DateRequired = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FileWorkOrder = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    From = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    GlAccount = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Note = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ProcurementType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Requester = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    To = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    WBS = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    WoNum = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    WoNumLetter = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    WorkOrderLetter = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    XS1 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    XS2 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    XS3 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    XS4 = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkOrders", x => x.WorkOrderId);
                    table.UniqueConstraint("AK_WorkOrders_WoNum", x => x.WoNum);
                    table.ForeignKey(
                        name: "FK_WorkOrders_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_WorkOrders_Statuses_StatusId",
                        column: x => x.StatusId,
                        principalTable: "Statuses",
                        principalColumn: "StatusId");
                    table.ForeignKey(
                        name: "FK_WorkOrders_WoTypes_WoTypeId",
                        column: x => x.WoTypeId,
                        principalTable: "WoTypes",
                        principalColumn: "WoTypeId");
                });

            migrationBuilder.CreateTable(
                name: "WoTypesDocuments",
                columns: table => new
                {
                    WoTypeDocumentId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    DocumentTypeId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    WoTypeId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    IsGenerated = table.Column<bool>(type: "bit", nullable: false),
                    IsMandatory = table.Column<bool>(type: "bit", nullable: false),
                    IsUploadRequired = table.Column<bool>(type: "bit", nullable: false),
                    Note = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RequiresApproval = table.Column<bool>(type: "bit", nullable: false),
                    Sequence = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WoTypesDocuments", x => x.WoTypeDocumentId);
                    table.ForeignKey(
                        name: "FK_WoTypesDocuments_DocumentTypes_DocumentTypeId",
                        column: x => x.DocumentTypeId,
                        principalTable: "DocumentTypes",
                        principalColumn: "DocumentTypeId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_WoTypesDocuments_WoTypes_WoTypeId",
                        column: x => x.WoTypeId,
                        principalTable: "WoTypes",
                        principalColumn: "WoTypeId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WoDetails",
                columns: table => new
                {
                    WoDetailId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    WorkOrderId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ItemName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    Quantity = table.Column<int>(type: "int", nullable: true),
                    Unit = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WoDetails", x => x.WoDetailId);
                    table.ForeignKey(
                        name: "FK_WoDetails_WorkOrders_WorkOrderId",
                        column: x => x.WorkOrderId,
                        principalTable: "WorkOrders",
                        principalColumn: "WorkOrderId");
                });

            migrationBuilder.CreateTable(
                name: "WoDocuments",
                columns: table => new
                {
                    WoDocumentId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    DocumentTypeId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    WorkOrderId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ApprovedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ApprovedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    ContentType = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    FileName = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    IsApproved = table.Column<bool>(type: "bit", nullable: true),
                    ObjectKey = table.Column<string>(type: "nvarchar(600)", maxLength: 600, nullable: false),
                    QrObjectKey = table.Column<string>(type: "nvarchar(600)", maxLength: 600, nullable: true),
                    QrText = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    Size = table.Column<long>(type: "bigint", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false, defaultValue: "Uploaded")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WoDocuments", x => x.WoDocumentId);
                    table.ForeignKey(
                        name: "FK_WoDocuments_DocumentTypes_DocumentTypeId",
                        column: x => x.DocumentTypeId,
                        principalTable: "DocumentTypes",
                        principalColumn: "DocumentTypeId");
                    table.ForeignKey(
                        name: "FK_WoDocuments_WorkOrders_WorkOrderId",
                        column: x => x.WorkOrderId,
                        principalTable: "WorkOrders",
                        principalColumn: "WorkOrderId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WoOffers",
                columns: table => new
                {
                    WoOfferId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    WorkOrderId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ItemPenawaran = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WoOffers", x => x.WoOfferId);
                    table.ForeignKey(
                        name: "FK_WoOffers_WorkOrders_WorkOrderId",
                        column: x => x.WorkOrderId,
                        principalTable: "WorkOrders",
                        principalColumn: "WorkOrderId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WoDocumentApprovals",
                columns: table => new
                {
                    WoDocumentApprovalId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ApproverId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    RoleId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    WoDocumentId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    WorkOrderId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ApprovedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Level = table.Column<int>(type: "int", nullable: false),
                    Note = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SequenceOrder = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false, defaultValue: "Pending")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WoDocumentApprovals", x => x.WoDocumentApprovalId);
                    table.ForeignKey(
                        name: "FK_WoDocumentApprovals_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_WoDocumentApprovals_AspNetUsers_ApproverId",
                        column: x => x.ApproverId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_WoDocumentApprovals_WoDocuments_WoDocumentId",
                        column: x => x.WoDocumentId,
                        principalTable: "WoDocuments",
                        principalColumn: "WoDocumentId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_WoDocumentApprovals_WorkOrders_WorkOrderId",
                        column: x => x.WorkOrderId,
                        principalTable: "WorkOrders",
                        principalColumn: "WorkOrderId");
                });

            migrationBuilder.CreateIndex(
                name: "IX_WoDetails_WorkOrderId",
                table: "WoDetails",
                column: "WorkOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_WoDocApprovals_Role_Status",
                table: "WoDocumentApprovals",
                columns: new[] { "RoleId", "Status" })
                .Annotation("SqlServer:Include", new[] { "WoDocumentId", "WorkOrderId", "Level", "SequenceOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_WoDocumentApprovals_ApproverId",
                table: "WoDocumentApprovals",
                column: "ApproverId");

            migrationBuilder.CreateIndex(
                name: "IX_WoDocumentApprovals_WorkOrderId",
                table: "WoDocumentApprovals",
                column: "WorkOrderId");

            migrationBuilder.CreateIndex(
                name: "UX_WoDocApprovals_Doc_Level_Seq",
                table: "WoDocumentApprovals",
                columns: new[] { "WoDocumentId", "Level", "SequenceOrder" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WoDocuments_DocumentTypeId",
                table: "WoDocuments",
                column: "DocumentTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_WoDocuments_QrText",
                table: "WoDocuments",
                column: "QrText");

            migrationBuilder.CreateIndex(
                name: "IX_WoDocuments_WorkOrderId_CreatedAt",
                table: "WoDocuments",
                columns: new[] { "WorkOrderId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_WoDocuments_WorkOrderId_DocumentTypeId_Status",
                table: "WoDocuments",
                columns: new[] { "WorkOrderId", "DocumentTypeId", "Status" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WoOffers_WorkOrderId",
                table: "WoOffers",
                column: "WorkOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkOrders_StatusId",
                table: "WorkOrders",
                column: "StatusId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkOrders_UserId_CreatedAt_Covering",
                table: "WorkOrders",
                columns: new[] { "UserId", "CreatedAt" },
                descending: new[] { false, true })
                .Annotation("SqlServer:Include", new[] { "WoNum", "Description", "StatusId" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkOrders_WoTypeId",
                table: "WorkOrders",
                column: "WoTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_WoTypesDocuments_DocumentTypeId",
                table: "WoTypesDocuments",
                column: "DocumentTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_WoTypesDocuments_WoTypeId",
                table: "WoTypesDocuments",
                column: "WoTypeId");

            migrationBuilder.AddForeignKey(
                name: "FK_DocumentApprovals_WoTypesDocuments_WoTypeDocumentId",
                table: "DocumentApprovals",
                column: "WoTypeDocumentId",
                principalTable: "WoTypesDocuments",
                principalColumn: "WoTypeDocumentId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ProfitLosses_WorkOrders_WorkOrderId",
                table: "ProfitLosses",
                column: "WorkOrderId",
                principalTable: "WorkOrders",
                principalColumn: "WorkOrderId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ProfitLossItems_ProfitLosses_ProfitLossId",
                table: "ProfitLossItems",
                column: "ProfitLossId",
                principalTable: "ProfitLosses",
                principalColumn: "ProfitLossId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ProfitLossItems_WoOffers_WoOfferId",
                table: "ProfitLossItems",
                column: "WoOfferId",
                principalTable: "WoOffers",
                principalColumn: "WoOfferId");

            migrationBuilder.AddForeignKey(
                name: "FK_VendorOffers_WoOffers_WoOfferId",
                table: "VendorOffers",
                column: "WoOfferId",
                principalTable: "WoOffers",
                principalColumn: "WoOfferId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_VendorOffers_WorkOrders_WorkOrderId",
                table: "VendorOffers",
                column: "WorkOrderId",
                principalTable: "WorkOrders",
                principalColumn: "WorkOrderId");
        }
    }
}
