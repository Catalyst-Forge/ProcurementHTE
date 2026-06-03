using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProcurementHTE.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Alpha8 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProfitLosses_Vendors_SelectedVendorId",
                table: "ProfitLosses");

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
                name: "ProfitLossId",
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

            migrationBuilder.AlterColumn<string>(
                name: "ProcurementId",
                table: "ProfitLossSelectedVendors",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<string>(
                name: "ProfitLossId",
                table: "ProfitLossSelectedVendors",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "Distance",
                table: "ProfitLosses",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NoLetterSelectedVendor",
                table: "ProfitLosses",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_VendorOffers_ProfitLossId",
                table: "VendorOffers",
                column: "ProfitLossId");

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

            migrationBuilder.AddForeignKey(
                name: "FK_ProfitLosses_Vendors_SelectedVendorId",
                table: "ProfitLosses",
                column: "SelectedVendorId",
                principalTable: "Vendors",
                principalColumn: "VendorId",
                onDelete: ReferentialAction.Cascade);

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
                name: "FK_ProfitLosses_Vendors_SelectedVendorId",
                table: "ProfitLosses");

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
                name: "FK_VendorOffers_ProfitLosses_ProfitLossId",
                table: "VendorOffers");

            migrationBuilder.DropIndex(
                name: "IX_VendorOffers_ProfitLossId",
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
                name: "ProfitLossId",
                table: "VendorOffers");

            migrationBuilder.DropColumn(
                name: "Quantity",
                table: "VendorOffers");

            migrationBuilder.DropColumn(
                name: "Trip",
                table: "VendorOffers");

            migrationBuilder.DropColumn(
                name: "ProfitLossId",
                table: "ProfitLossSelectedVendors");

            migrationBuilder.DropColumn(
                name: "NoLetterSelectedVendor",
                table: "ProfitLosses");

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

            migrationBuilder.AlterColumn<string>(
                name: "ProcurementId",
                table: "ProfitLossSelectedVendors",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AlterColumn<decimal>(
                name: "Distance",
                table: "ProfitLosses",
                type: "decimal(18,2)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)");

            migrationBuilder.AddForeignKey(
                name: "FK_ProfitLosses_Vendors_SelectedVendorId",
                table: "ProfitLosses",
                column: "SelectedVendorId",
                principalTable: "Vendors",
                principalColumn: "VendorId",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
