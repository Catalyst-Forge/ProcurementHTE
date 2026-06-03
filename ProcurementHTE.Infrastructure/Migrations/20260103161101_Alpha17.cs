using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProcurementHTE.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Alpha17 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "PurchaseRequisitions",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "PurchaseRequisitions",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "PurchaseRequisitions",
                type: "bit",
                nullable: false,
                defaultValue: false);

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

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "ProfitLosses",
                type: "bit",
                nullable: false,
                defaultValue: false);

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

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Procurements",
                type: "bit",
                nullable: false,
                defaultValue: false);

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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
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
                name: "DeletedAt",
                table: "PurchaseRequisitions");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "PurchaseRequisitions");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "PurchaseRequisitions");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "ProfitLosses");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "ProfitLosses");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "ProfitLosses");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "Procurements");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "Procurements");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Procurements");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "ProcDocuments");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "ProcDocuments");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "ProcDocuments");
        }
    }
}
