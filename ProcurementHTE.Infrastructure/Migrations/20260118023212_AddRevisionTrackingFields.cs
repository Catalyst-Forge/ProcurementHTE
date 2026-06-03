using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProcurementHTE.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRevisionTrackingFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PendingRevisionSymptoms",
                table: "Procurements",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RejectionSymptoms",
                table: "Procurements",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ResubmittedAt",
                table: "Procurements",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ResubmittedByUserId",
                table: "Procurements",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RevisionCount",
                table: "Procurements",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "StatusBeforeRejection",
                table: "Procurements",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Procurements_PicOpsUserId",
                table: "Procurements",
                column: "PicOpsUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Procurements_ResubmittedByUserId",
                table: "Procurements",
                column: "ResubmittedByUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Procurements_AspNetUsers_PicOpsUserId",
                table: "Procurements",
                column: "PicOpsUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Procurements_AspNetUsers_ResubmittedByUserId",
                table: "Procurements",
                column: "ResubmittedByUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Procurements_AspNetUsers_PicOpsUserId",
                table: "Procurements");

            migrationBuilder.DropForeignKey(
                name: "FK_Procurements_AspNetUsers_ResubmittedByUserId",
                table: "Procurements");

            migrationBuilder.DropIndex(
                name: "IX_Procurements_PicOpsUserId",
                table: "Procurements");

            migrationBuilder.DropIndex(
                name: "IX_Procurements_ResubmittedByUserId",
                table: "Procurements");

            migrationBuilder.DropColumn(
                name: "PendingRevisionSymptoms",
                table: "Procurements");

            migrationBuilder.DropColumn(
                name: "RejectionSymptoms",
                table: "Procurements");

            migrationBuilder.DropColumn(
                name: "ResubmittedAt",
                table: "Procurements");

            migrationBuilder.DropColumn(
                name: "ResubmittedByUserId",
                table: "Procurements");

            migrationBuilder.DropColumn(
                name: "RevisionCount",
                table: "Procurements");

            migrationBuilder.DropColumn(
                name: "StatusBeforeRejection",
                table: "Procurements");
        }
    }
}
