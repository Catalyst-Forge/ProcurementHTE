using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProcurementHTE.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddInvoiceAndArPickupFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ApInvoicePickedUpAt",
                table: "Procurements",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ApInvoiceUserId",
                table: "Procurements",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ArPickedUpAt",
                table: "Procurements",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ArUserId",
                table: "Procurements",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SANo",
                table: "Procurements",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SP3No",
                table: "Procurements",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Procurements_ApInvoiceUserId",
                table: "Procurements",
                column: "ApInvoiceUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Procurements_ArUserId",
                table: "Procurements",
                column: "ArUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Procurements_AspNetUsers_ApInvoiceUserId",
                table: "Procurements",
                column: "ApInvoiceUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Procurements_AspNetUsers_ArUserId",
                table: "Procurements",
                column: "ArUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Procurements_AspNetUsers_ApInvoiceUserId",
                table: "Procurements");

            migrationBuilder.DropForeignKey(
                name: "FK_Procurements_AspNetUsers_ArUserId",
                table: "Procurements");

            migrationBuilder.DropIndex(
                name: "IX_Procurements_ApInvoiceUserId",
                table: "Procurements");

            migrationBuilder.DropIndex(
                name: "IX_Procurements_ArUserId",
                table: "Procurements");

            migrationBuilder.DropColumn(
                name: "ApInvoicePickedUpAt",
                table: "Procurements");

            migrationBuilder.DropColumn(
                name: "ApInvoiceUserId",
                table: "Procurements");

            migrationBuilder.DropColumn(
                name: "ArPickedUpAt",
                table: "Procurements");

            migrationBuilder.DropColumn(
                name: "ArUserId",
                table: "Procurements");

            migrationBuilder.DropColumn(
                name: "SANo",
                table: "Procurements");

            migrationBuilder.DropColumn(
                name: "SP3No",
                table: "Procurements");
        }
    }
}
