using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProcurementHTE.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAccrualAndRigHteFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "AccrualFilledAt",
                table: "Procurements",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AccrualFilledByUserId",
                table: "Procurements",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NoAccrual",
                table: "Procurements",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NoHte",
                table: "Procurements",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NoRig",
                table: "Procurements",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PotensiAccrual",
                table: "Procurements",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StatusAccrual",
                table: "Procurements",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Procurements_AccrualFilledByUserId",
                table: "Procurements",
                column: "AccrualFilledByUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Procurements_AspNetUsers_AccrualFilledByUserId",
                table: "Procurements",
                column: "AccrualFilledByUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Procurements_AspNetUsers_AccrualFilledByUserId",
                table: "Procurements");

            migrationBuilder.DropIndex(
                name: "IX_Procurements_AccrualFilledByUserId",
                table: "Procurements");

            migrationBuilder.DropColumn(
                name: "AccrualFilledAt",
                table: "Procurements");

            migrationBuilder.DropColumn(
                name: "AccrualFilledByUserId",
                table: "Procurements");

            migrationBuilder.DropColumn(
                name: "NoAccrual",
                table: "Procurements");

            migrationBuilder.DropColumn(
                name: "NoHte",
                table: "Procurements");

            migrationBuilder.DropColumn(
                name: "NoRig",
                table: "Procurements");

            migrationBuilder.DropColumn(
                name: "PotensiAccrual",
                table: "Procurements");

            migrationBuilder.DropColumn(
                name: "StatusAccrual",
                table: "Procurements");
        }
    }
}
