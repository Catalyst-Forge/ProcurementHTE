using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProcurementHTE.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RestructureDatabase : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Vendors_AspNetUsers_UserId",
                table: "Vendors");

            migrationBuilder.DropForeignKey(
                name: "FK_WoDetails_WorkOrders_WorkOrderId",
                table: "WoDetails");

            migrationBuilder.DropForeignKey(
                name: "FK_WorkOrders_Tenders_TenderId",
                table: "WorkOrders");

            migrationBuilder.DropForeignKey(
                name: "FK_WorkOrders_WoTypes_WoTypeId",
                table: "WorkOrders");

            migrationBuilder.DropTable(
                name: "ReasonRejecteds");

            migrationBuilder.DropIndex(
                name: "IX_WorkOrders_TenderId",
                table: "WorkOrders");

            migrationBuilder.DropIndex(
                name: "IX_Vendors_UserId",
                table: "Vendors");

            migrationBuilder.DropColumn(
                name: "TenderId",
                table: "WorkOrders");

            migrationBuilder.DropColumn(
                name: "WoName",
                table: "WorkOrders");

            migrationBuilder.DropColumn(
                name: "Price",
                table: "Vendors");

            migrationBuilder.RenameColumn(
                name: "UserId",
                table: "Vendors",
                newName: "VendorCode");

            migrationBuilder.RenameColumn(
                name: "Documents",
                table: "Vendors",
                newName: "Status");

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "WoTypes",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Approved",
                table: "WorkOrders",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "DateLetter",
                table: "WorkOrders",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DateRequired",
                table: "WorkOrders",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "Destination",
                table: "WorkOrders",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FileWorkOrder",
                table: "WorkOrders",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "FromLocation",
                table: "WorkOrders",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GlAccount",
                table: "WorkOrders",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ProcurementType",
                table: "WorkOrders",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Requester",
                table: "WorkOrders",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "UserId",
                table: "WorkOrders",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VendorId",
                table: "WorkOrders",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "WBS",
                table: "WorkOrders",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "WoLetter",
                table: "WorkOrders",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "WoNum",
                table: "WorkOrders",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "WorkOrderLetter",
                table: "WorkOrders",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "XS1",
                table: "WorkOrders",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "XS2",
                table: "WorkOrders",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "XS3",
                table: "WorkOrders",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "XS4",
                table: "WorkOrders",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "WorkOrderId",
                table: "WoDetails",
                type: "nvarchar(450)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AddColumn<int>(
                name: "Quantity",
                table: "WoDetails",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Unit",
                table: "WoDetails",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "WoNum",
                table: "WoDetails",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<string>(
                name: "VendorName",
                table: "Vendors",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(255)",
                oldMaxLength: 255);

            migrationBuilder.AddColumn<string>(
                name: "Address",
                table: "Vendors",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "City",
                table: "Vendors",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Comment",
                table: "Vendors",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ContactPerson",
                table: "Vendors",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ContactPosition",
                table: "Vendors",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Email",
                table: "Vendors",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "NPWP",
                table: "Vendors",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PhoneNumber",
                table: "Vendors",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PostalCode",
                table: "Vendors",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Province",
                table: "Vendors",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddUniqueConstraint(
                name: "AK_Vendors_VendorCode",
                table: "Vendors",
                column: "VendorCode");

            migrationBuilder.CreateTable(
                name: "DocumentTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UserRole",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    RoleId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Id = table.Column<int>(type: "int", nullable: false),
                    AssignedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserRole", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_UserRole_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserRole_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VendorWorkOrders",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AssignedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    InitialOfferTotal = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    FinalNegotiationTotal = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Efficiency = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    WoNum = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    VendorCode = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    WorkOrderId = table.Column<string>(type: "nvarchar(450)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VendorWorkOrders", x => x.Id);
                    table.UniqueConstraint("AK_VendorWorkOrders_WoNum", x => x.WoNum);
                    table.ForeignKey(
                        name: "FK_VendorWorkOrders_Vendors_VendorCode",
                        column: x => x.VendorCode,
                        principalTable: "Vendors",
                        principalColumn: "VendorCode");
                    table.ForeignKey(
                        name: "FK_VendorWorkOrders_WorkOrders_WoNum",
                        column: x => x.WoNum,
                        principalTable: "WorkOrders",
                        principalColumn: "WorkOrderId");
                    table.ForeignKey(
                        name: "FK_VendorWorkOrders_WorkOrders_WorkOrderId",
                        column: x => x.WorkOrderId,
                        principalTable: "WorkOrders",
                        principalColumn: "WorkOrderId");
                });

            migrationBuilder.CreateTable(
                name: "WoDocuments",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FilePath = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    WoNum = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    WorkOrderId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    DocumentTypeId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WoDocuments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WoDocuments_DocumentTypes_DocumentTypeId",
                        column: x => x.DocumentTypeId,
                        principalTable: "DocumentTypes",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_WoDocuments_WorkOrders_WoNum",
                        column: x => x.WoNum,
                        principalTable: "WorkOrders",
                        principalColumn: "WorkOrderId");
                    table.ForeignKey(
                        name: "FK_WoDocuments_WorkOrders_WorkOrderId",
                        column: x => x.WorkOrderId,
                        principalTable: "WorkOrders",
                        principalColumn: "WorkOrderId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WoTypesDocuments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IsMandatory = table.Column<bool>(type: "bit", nullable: false),
                    Sequence = table.Column<int>(type: "int", nullable: false),
                    IsGenerated = table.Column<bool>(type: "bit", nullable: false),
                    IsUploadRequired = table.Column<bool>(type: "bit", nullable: false),
                    RequiresApproval = table.Column<bool>(type: "bit", nullable: false),
                    Note = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    WoTypeId = table.Column<int>(type: "int", nullable: false),
                    DocumentTypeId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WoTypesDocuments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WoTypesDocuments_DocumentTypes_DocumentTypeId",
                        column: x => x.DocumentTypeId,
                        principalTable: "DocumentTypes",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_WoTypesDocuments_WoTypes_WoTypeId",
                        column: x => x.WoTypeId,
                        principalTable: "WoTypes",
                        principalColumn: "WoTypeId");
                });

            migrationBuilder.CreateTable(
                name: "VendorOffers",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ItemName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Quantity = table.Column<int>(type: "int", nullable: true),
                    UnitPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    TotalPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    OfferDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    WoNum = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VendorOffers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VendorOffers_VendorWorkOrders_WoNum",
                        column: x => x.WoNum,
                        principalTable: "VendorWorkOrders",
                        principalColumn: "WoNum");
                });

            migrationBuilder.CreateTable(
                name: "WoDocumentApprovals",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ApprovedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Note = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    WoNum = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    WoDocumentId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    RoleId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ApproverId = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WoDocumentApprovals", x => x.Id);
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
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_WoDocumentApprovals_WoDocuments_WoNum",
                        column: x => x.WoNum,
                        principalTable: "WoDocuments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DocumentApprovals",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Level = table.Column<int>(type: "int", nullable: false),
                    SequenceOrder = table.Column<int>(type: "int", nullable: false),
                    RoleId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    WoTypeDocumentId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentApprovals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DocumentApprovals_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_DocumentApprovals_WoTypesDocuments_WoTypeDocumentId",
                        column: x => x.WoTypeDocumentId,
                        principalTable: "WoTypesDocuments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WorkOrders_UserId",
                table: "WorkOrders",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkOrders_VendorId",
                table: "WorkOrders",
                column: "VendorId");

            migrationBuilder.CreateIndex(
                name: "IX_WoDetails_WoNum",
                table: "WoDetails",
                column: "WoNum");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentApprovals_RoleId",
                table: "DocumentApprovals",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentApprovals_WoTypeDocumentId",
                table: "DocumentApprovals",
                column: "WoTypeDocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_UserRole_RoleId",
                table: "UserRole",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_VendorOffers_WoNum",
                table: "VendorOffers",
                column: "WoNum");

            migrationBuilder.CreateIndex(
                name: "IX_VendorWorkOrders_VendorCode",
                table: "VendorWorkOrders",
                column: "VendorCode");

            migrationBuilder.CreateIndex(
                name: "IX_VendorWorkOrders_WorkOrderId",
                table: "VendorWorkOrders",
                column: "WorkOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_WoDocumentApprovals_ApproverId",
                table: "WoDocumentApprovals",
                column: "ApproverId");

            migrationBuilder.CreateIndex(
                name: "IX_WoDocumentApprovals_RoleId",
                table: "WoDocumentApprovals",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_WoDocumentApprovals_WoDocumentId",
                table: "WoDocumentApprovals",
                column: "WoDocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_WoDocumentApprovals_WoNum",
                table: "WoDocumentApprovals",
                column: "WoNum");

            migrationBuilder.CreateIndex(
                name: "IX_WoDocuments_DocumentTypeId",
                table: "WoDocuments",
                column: "DocumentTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_WoDocuments_WoNum",
                table: "WoDocuments",
                column: "WoNum");

            migrationBuilder.CreateIndex(
                name: "IX_WoDocuments_WorkOrderId",
                table: "WoDocuments",
                column: "WorkOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_WoTypesDocuments_DocumentTypeId",
                table: "WoTypesDocuments",
                column: "DocumentTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_WoTypesDocuments_WoTypeId",
                table: "WoTypesDocuments",
                column: "WoTypeId");

            migrationBuilder.AddForeignKey(
                name: "FK_WoDetails_WorkOrders_WoNum",
                table: "WoDetails",
                column: "WoNum",
                principalTable: "WorkOrders",
                principalColumn: "WorkOrderId");

            migrationBuilder.AddForeignKey(
                name: "FK_WoDetails_WorkOrders_WorkOrderId",
                table: "WoDetails",
                column: "WorkOrderId",
                principalTable: "WorkOrders",
                principalColumn: "WorkOrderId");

            migrationBuilder.AddForeignKey(
                name: "FK_WorkOrders_AspNetUsers_UserId",
                table: "WorkOrders",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_WorkOrders_Vendors_VendorId",
                table: "WorkOrders",
                column: "VendorId",
                principalTable: "Vendors",
                principalColumn: "VendorId");

            migrationBuilder.AddForeignKey(
                name: "FK_WorkOrders_WoTypes_WoTypeId",
                table: "WorkOrders",
                column: "WoTypeId",
                principalTable: "WoTypes",
                principalColumn: "WoTypeId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_WoDetails_WorkOrders_WoNum",
                table: "WoDetails");

            migrationBuilder.DropForeignKey(
                name: "FK_WoDetails_WorkOrders_WorkOrderId",
                table: "WoDetails");

            migrationBuilder.DropForeignKey(
                name: "FK_WorkOrders_AspNetUsers_UserId",
                table: "WorkOrders");

            migrationBuilder.DropForeignKey(
                name: "FK_WorkOrders_Vendors_VendorId",
                table: "WorkOrders");

            migrationBuilder.DropForeignKey(
                name: "FK_WorkOrders_WoTypes_WoTypeId",
                table: "WorkOrders");

            migrationBuilder.DropTable(
                name: "DocumentApprovals");

            migrationBuilder.DropTable(
                name: "UserRole");

            migrationBuilder.DropTable(
                name: "VendorOffers");

            migrationBuilder.DropTable(
                name: "WoDocumentApprovals");

            migrationBuilder.DropTable(
                name: "WoTypesDocuments");

            migrationBuilder.DropTable(
                name: "VendorWorkOrders");

            migrationBuilder.DropTable(
                name: "WoDocuments");

            migrationBuilder.DropTable(
                name: "DocumentTypes");

            migrationBuilder.DropIndex(
                name: "IX_WorkOrders_UserId",
                table: "WorkOrders");

            migrationBuilder.DropIndex(
                name: "IX_WorkOrders_VendorId",
                table: "WorkOrders");

            migrationBuilder.DropIndex(
                name: "IX_WoDetails_WoNum",
                table: "WoDetails");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_Vendors_VendorCode",
                table: "Vendors");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "WoTypes");

            migrationBuilder.DropColumn(
                name: "Approved",
                table: "WorkOrders");

            migrationBuilder.DropColumn(
                name: "DateLetter",
                table: "WorkOrders");

            migrationBuilder.DropColumn(
                name: "DateRequired",
                table: "WorkOrders");

            migrationBuilder.DropColumn(
                name: "Destination",
                table: "WorkOrders");

            migrationBuilder.DropColumn(
                name: "FileWorkOrder",
                table: "WorkOrders");

            migrationBuilder.DropColumn(
                name: "FromLocation",
                table: "WorkOrders");

            migrationBuilder.DropColumn(
                name: "GlAccount",
                table: "WorkOrders");

            migrationBuilder.DropColumn(
                name: "ProcurementType",
                table: "WorkOrders");

            migrationBuilder.DropColumn(
                name: "Requester",
                table: "WorkOrders");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "WorkOrders");

            migrationBuilder.DropColumn(
                name: "VendorId",
                table: "WorkOrders");

            migrationBuilder.DropColumn(
                name: "WBS",
                table: "WorkOrders");

            migrationBuilder.DropColumn(
                name: "WoLetter",
                table: "WorkOrders");

            migrationBuilder.DropColumn(
                name: "WoNum",
                table: "WorkOrders");

            migrationBuilder.DropColumn(
                name: "WorkOrderLetter",
                table: "WorkOrders");

            migrationBuilder.DropColumn(
                name: "XS1",
                table: "WorkOrders");

            migrationBuilder.DropColumn(
                name: "XS2",
                table: "WorkOrders");

            migrationBuilder.DropColumn(
                name: "XS3",
                table: "WorkOrders");

            migrationBuilder.DropColumn(
                name: "XS4",
                table: "WorkOrders");

            migrationBuilder.DropColumn(
                name: "Quantity",
                table: "WoDetails");

            migrationBuilder.DropColumn(
                name: "Unit",
                table: "WoDetails");

            migrationBuilder.DropColumn(
                name: "WoNum",
                table: "WoDetails");

            migrationBuilder.DropColumn(
                name: "Address",
                table: "Vendors");

            migrationBuilder.DropColumn(
                name: "City",
                table: "Vendors");

            migrationBuilder.DropColumn(
                name: "Comment",
                table: "Vendors");

            migrationBuilder.DropColumn(
                name: "ContactPerson",
                table: "Vendors");

            migrationBuilder.DropColumn(
                name: "ContactPosition",
                table: "Vendors");

            migrationBuilder.DropColumn(
                name: "Email",
                table: "Vendors");

            migrationBuilder.DropColumn(
                name: "NPWP",
                table: "Vendors");

            migrationBuilder.DropColumn(
                name: "PhoneNumber",
                table: "Vendors");

            migrationBuilder.DropColumn(
                name: "PostalCode",
                table: "Vendors");

            migrationBuilder.DropColumn(
                name: "Province",
                table: "Vendors");

            migrationBuilder.RenameColumn(
                name: "VendorCode",
                table: "Vendors",
                newName: "UserId");

            migrationBuilder.RenameColumn(
                name: "Status",
                table: "Vendors",
                newName: "Documents");

            migrationBuilder.AddColumn<string>(
                name: "TenderId",
                table: "WorkOrders",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "WoName",
                table: "WorkOrders",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<string>(
                name: "WorkOrderId",
                table: "WoDetails",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "VendorName",
                table: "Vendors",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<decimal>(
                name: "Price",
                table: "Vendors",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateTable(
                name: "ReasonRejecteds",
                columns: table => new
                {
                    ReasonId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    ReasonName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReasonRejecteds", x => x.ReasonId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WorkOrders_TenderId",
                table: "WorkOrders",
                column: "TenderId");

            migrationBuilder.CreateIndex(
                name: "IX_Vendors_UserId",
                table: "Vendors",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Vendors_AspNetUsers_UserId",
                table: "Vendors",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_WoDetails_WorkOrders_WorkOrderId",
                table: "WoDetails",
                column: "WorkOrderId",
                principalTable: "WorkOrders",
                principalColumn: "WorkOrderId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_WorkOrders_Tenders_TenderId",
                table: "WorkOrders",
                column: "TenderId",
                principalTable: "Tenders",
                principalColumn: "TenderId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_WorkOrders_WoTypes_WoTypeId",
                table: "WorkOrders",
                column: "WoTypeId",
                principalTable: "WoTypes",
                principalColumn: "WoTypeId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
