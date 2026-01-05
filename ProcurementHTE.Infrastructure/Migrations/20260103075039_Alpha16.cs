using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace ProcurementHTE.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Alpha16 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Quantity",
                table: "VendorOffers");

            migrationBuilder.RenameColumn(
                name: "Trip",
                table: "VendorOffers",
                newName: "QuantityItem");

            migrationBuilder.RenameColumn(
                name: "TarifAwal",
                table: "ProfitLossItems",
                newName: "BasePrice");

            migrationBuilder.AlterColumn<string>(
                name: "NoLetter",
                table: "VendorOffers",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(128)",
                oldMaxLength: 128);

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
                defaultValue: "44444444-4444-4444-4444-444444444444"); // Default to TRIP

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

            migrationBuilder.AddColumn<int>(
                name: "DurasiTotal",
                table: "ProfitLosses",
                type: "int",
                nullable: true);

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
                name: "IX_UnitTypes_Code",
                table: "UnitTypes",
                column: "Code",
                unique: true);

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
                name: "FK_ProfitLossItems_UnitTypes_UnitTypeId",
                table: "ProfitLossItems");

            migrationBuilder.DropForeignKey(
                name: "FK_VendorOffers_UnitTypes_UnitTypeId",
                table: "VendorOffers");

            migrationBuilder.DropTable(
                name: "UnitTypes");

            migrationBuilder.DropIndex(
                name: "IX_VendorOffers_UnitTypeId",
                table: "VendorOffers");

            migrationBuilder.DropIndex(
                name: "IX_ProfitLossItems_UnitTypeId",
                table: "ProfitLossItems");

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
                name: "DurasiTotal",
                table: "ProfitLosses");

            migrationBuilder.DropColumn(
                name: "TglMulaiMoving",
                table: "ProfitLosses");

            migrationBuilder.DropColumn(
                name: "TglMulaiSewa",
                table: "ProfitLosses");

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
        }
    }
}
