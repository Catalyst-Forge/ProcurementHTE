using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProcurementHTE.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddApprovalTimelineFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ManagerApprovalEndAt",
                table: "Procurements",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ManagerApprovalStartAt",
                table: "Procurements",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "OpDirApprovalEndAt",
                table: "Procurements",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "OpDirApprovalStartAt",
                table: "Procurements",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PresDirApprovalEndAt",
                table: "Procurements",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PresDirApprovalStartAt",
                table: "Procurements",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "VpApprovalEndAt",
                table: "Procurements",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "VpApprovalStartAt",
                table: "Procurements",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ManagerApprovalEndAt",
                table: "Procurements");

            migrationBuilder.DropColumn(
                name: "ManagerApprovalStartAt",
                table: "Procurements");

            migrationBuilder.DropColumn(
                name: "OpDirApprovalEndAt",
                table: "Procurements");

            migrationBuilder.DropColumn(
                name: "OpDirApprovalStartAt",
                table: "Procurements");

            migrationBuilder.DropColumn(
                name: "PresDirApprovalEndAt",
                table: "Procurements");

            migrationBuilder.DropColumn(
                name: "PresDirApprovalStartAt",
                table: "Procurements");

            migrationBuilder.DropColumn(
                name: "VpApprovalEndAt",
                table: "Procurements");

            migrationBuilder.DropColumn(
                name: "VpApprovalStartAt",
                table: "Procurements");
        }
    }
}
