using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProcurementHTE.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class WoOfferPNLModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProfitLosses_Vendors_SelectedVendorId",
                table: "ProfitLosses");

            migrationBuilder.DropForeignKey(
                name: "FK_WoDocuments_WorkOrders_WorkOrderId",
                table: "WoDocuments");

            migrationBuilder.DropIndex(
                name: "IX_WoDocumentApprovals_RoleId",
                table: "WoDocumentApprovals");

            migrationBuilder.DropIndex(
                name: "IX_WoDocumentApprovals_WoDocumentId",
                table: "WoDocumentApprovals");

            migrationBuilder.DropColumn(
                name: "KmPer25",
                table: "ProfitLosses");

            migrationBuilder.DropColumn(
                name: "OperatorCost",
                table: "ProfitLosses");

            migrationBuilder.DropColumn(
                name: "Revenue",
                table: "ProfitLosses");

            migrationBuilder.DropColumn(
                name: "TarifAdd",
                table: "ProfitLosses");

            migrationBuilder.DropColumn(
                name: "TarifAwal",
                table: "ProfitLosses");

            migrationBuilder.RenameColumn(
                name: "FromLocation",
                table: "WorkOrders",
                newName: "To");

            migrationBuilder.RenameColumn(
                name: "Destination",
                table: "WorkOrders",
                newName: "From");

            migrationBuilder.AlterColumn<int>(
                name: "StatusId",
                table: "WorkOrders",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NoLetter",
                table: "VendorOffers",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "WoOfferId",
                table: "VendorOffers",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<string>(
                name: "SelectedVendorId",
                table: "ProfitLosses",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldNullable: true);

            migrationBuilder.CreateTable(
                name: "WoOffers",
                columns: table => new
                {
                    WoOfferId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ItemPenawaran = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    WorkOrderId = table.Column<string>(type: "nvarchar(450)", nullable: false)
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
                name: "ProfitLossItems",
                columns: table => new
                {
                    ProfitLossItemId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    TarifAwal = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TarifAdd = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    KmPer25 = table.Column<int>(type: "int", nullable: false),
                    OperatorCost = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Revenue = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ProfitLossId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    WoOfferId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProfitLossItems", x => x.ProfitLossItemId);
                    table.ForeignKey(
                        name: "FK_ProfitLossItems_ProfitLosses_ProfitLossId",
                        column: x => x.ProfitLossId,
                        principalTable: "ProfitLosses",
                        principalColumn: "ProfitLossId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProfitLossItems_WoOffers_WoOfferId",
                        column: x => x.WoOfferId,
                        principalTable: "WoOffers",
                        principalColumn: "WoOfferId");
                });

            migrationBuilder.CreateIndex(
                name: "IX_WoDocApprovals_Role_Status",
                table: "WoDocumentApprovals",
                columns: new[] { "RoleId", "Status" })
                .Annotation("SqlServer:Include", new[] { "WoDocumentId", "WorkOrderId", "Level", "SequenceOrder" });

            migrationBuilder.CreateIndex(
                name: "UX_WoDocApprovals_Doc_Level_Seq",
                table: "WoDocumentApprovals",
                columns: new[] { "WoDocumentId", "Level", "SequenceOrder" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VendorOffers_WoOfferId",
                table: "VendorOffers",
                column: "WoOfferId");

            migrationBuilder.CreateIndex(
                name: "IX_ProfitLossItems_ProfitLossId",
                table: "ProfitLossItems",
                column: "ProfitLossId");

            migrationBuilder.CreateIndex(
                name: "IX_ProfitLossItems_WoOfferId",
                table: "ProfitLossItems",
                column: "WoOfferId");

            migrationBuilder.CreateIndex(
                name: "IX_WoOffers_WorkOrderId",
                table: "WoOffers",
                column: "WorkOrderId");

            migrationBuilder.AddForeignKey(
                name: "FK_ProfitLosses_Vendors_SelectedVendorId",
                table: "ProfitLosses",
                column: "SelectedVendorId",
                principalTable: "Vendors",
                principalColumn: "VendorId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_VendorOffers_WoOffers_WoOfferId",
                table: "VendorOffers",
                column: "WoOfferId",
                principalTable: "WoOffers",
                principalColumn: "WoOfferId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_WoDocuments_WorkOrders_WorkOrderId",
                table: "WoDocuments",
                column: "WorkOrderId",
                principalTable: "WorkOrders",
                principalColumn: "WorkOrderId",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProfitLosses_Vendors_SelectedVendorId",
                table: "ProfitLosses");

            migrationBuilder.DropForeignKey(
                name: "FK_VendorOffers_WoOffers_WoOfferId",
                table: "VendorOffers");

            migrationBuilder.DropForeignKey(
                name: "FK_WoDocuments_WorkOrders_WorkOrderId",
                table: "WoDocuments");

            migrationBuilder.DropTable(
                name: "ProfitLossItems");

            migrationBuilder.DropTable(
                name: "WoOffers");

            migrationBuilder.DropIndex(
                name: "IX_WoDocApprovals_Role_Status",
                table: "WoDocumentApprovals");

            migrationBuilder.DropIndex(
                name: "UX_WoDocApprovals_Doc_Level_Seq",
                table: "WoDocumentApprovals");

            migrationBuilder.DropIndex(
                name: "IX_VendorOffers_WoOfferId",
                table: "VendorOffers");

            migrationBuilder.DropColumn(
                name: "NoLetter",
                table: "VendorOffers");

            migrationBuilder.DropColumn(
                name: "WoOfferId",
                table: "VendorOffers");

            migrationBuilder.RenameColumn(
                name: "To",
                table: "WorkOrders",
                newName: "FromLocation");

            migrationBuilder.RenameColumn(
                name: "From",
                table: "WorkOrders",
                newName: "Destination");

            migrationBuilder.AlterColumn<int>(
                name: "StatusId",
                table: "WorkOrders",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<string>(
                name: "SelectedVendorId",
                table: "ProfitLosses",
                type: "nvarchar(450)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldMaxLength: 450);

            migrationBuilder.AddColumn<int>(
                name: "KmPer25",
                table: "ProfitLosses",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "OperatorCost",
                table: "ProfitLosses",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "Revenue",
                table: "ProfitLosses",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "TarifAdd",
                table: "ProfitLosses",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "TarifAwal",
                table: "ProfitLosses",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateIndex(
                name: "IX_WoDocumentApprovals_RoleId",
                table: "WoDocumentApprovals",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_WoDocumentApprovals_WoDocumentId",
                table: "WoDocumentApprovals",
                column: "WoDocumentId");

            migrationBuilder.AddForeignKey(
                name: "FK_ProfitLosses_Vendors_SelectedVendorId",
                table: "ProfitLosses",
                column: "SelectedVendorId",
                principalTable: "Vendors",
                principalColumn: "VendorId");

            migrationBuilder.AddForeignKey(
                name: "FK_WoDocuments_WorkOrders_WorkOrderId",
                table: "WoDocuments",
                column: "WorkOrderId",
                principalTable: "WorkOrders",
                principalColumn: "WorkOrderId");
        }
    }
}
