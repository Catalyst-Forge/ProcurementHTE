using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProcurementHTE.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddIspaAndLdpFileFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "IspaDate",
                table: "Procurements",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IspaFileContentType",
                table: "Procurements",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IspaFileName",
                table: "Procurements",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IspaFileObjectKey",
                table: "Procurements",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "IspaFileSize",
                table: "Procurements",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "IspaSubmitDate",
                table: "Procurements",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LdpFileContentType",
                table: "Procurements",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LdpFileName",
                table: "Procurements",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LdpFileObjectKey",
                table: "Procurements",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "LdpFileSize",
                table: "Procurements",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LdpUploadedAt",
                table: "Procurements",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LdpUploadedByUserId",
                table: "Procurements",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Procurements_AnalystHteUserId",
                table: "Procurements",
                column: "AnalystHteUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Procurements_AssistantManagerUserId",
                table: "Procurements",
                column: "AssistantManagerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Procurements_ManagerUserId",
                table: "Procurements",
                column: "ManagerUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Procurements_AspNetUsers_AnalystHteUserId",
                table: "Procurements",
                column: "AnalystHteUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Procurements_AspNetUsers_AssistantManagerUserId",
                table: "Procurements",
                column: "AssistantManagerUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Procurements_AspNetUsers_ManagerUserId",
                table: "Procurements",
                column: "ManagerUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Procurements_AspNetUsers_AnalystHteUserId",
                table: "Procurements");

            migrationBuilder.DropForeignKey(
                name: "FK_Procurements_AspNetUsers_AssistantManagerUserId",
                table: "Procurements");

            migrationBuilder.DropForeignKey(
                name: "FK_Procurements_AspNetUsers_ManagerUserId",
                table: "Procurements");

            migrationBuilder.DropIndex(
                name: "IX_Procurements_AnalystHteUserId",
                table: "Procurements");

            migrationBuilder.DropIndex(
                name: "IX_Procurements_AssistantManagerUserId",
                table: "Procurements");

            migrationBuilder.DropIndex(
                name: "IX_Procurements_ManagerUserId",
                table: "Procurements");

            migrationBuilder.DropColumn(
                name: "IspaDate",
                table: "Procurements");

            migrationBuilder.DropColumn(
                name: "IspaFileContentType",
                table: "Procurements");

            migrationBuilder.DropColumn(
                name: "IspaFileName",
                table: "Procurements");

            migrationBuilder.DropColumn(
                name: "IspaFileObjectKey",
                table: "Procurements");

            migrationBuilder.DropColumn(
                name: "IspaFileSize",
                table: "Procurements");

            migrationBuilder.DropColumn(
                name: "IspaSubmitDate",
                table: "Procurements");

            migrationBuilder.DropColumn(
                name: "LdpFileContentType",
                table: "Procurements");

            migrationBuilder.DropColumn(
                name: "LdpFileName",
                table: "Procurements");

            migrationBuilder.DropColumn(
                name: "LdpFileObjectKey",
                table: "Procurements");

            migrationBuilder.DropColumn(
                name: "LdpFileSize",
                table: "Procurements");

            migrationBuilder.DropColumn(
                name: "LdpUploadedAt",
                table: "Procurements");

            migrationBuilder.DropColumn(
                name: "LdpUploadedByUserId",
                table: "Procurements");
        }
    }
}
