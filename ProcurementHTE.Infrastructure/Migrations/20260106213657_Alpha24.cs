using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProcurementHTE.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Alpha24 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ApprovedAt",
                table: "ProcDocuments");

            migrationBuilder.DropColumn(
                name: "ApprovedByUserId",
                table: "ProcDocuments");

            migrationBuilder.DropColumn(
                name: "IsApproved",
                table: "ProcDocuments");

            migrationBuilder.DropColumn(
                name: "QrObjectKey",
                table: "ProcDocuments");

            migrationBuilder.DropColumn(
                name: "QrText",
                table: "ProcDocuments");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "ProcDocuments");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ApprovedAt",
                table: "ProcDocuments",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ApprovedByUserId",
                table: "ProcDocuments",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsApproved",
                table: "ProcDocuments",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "QrObjectKey",
                table: "ProcDocuments",
                type: "nvarchar(600)",
                maxLength: 600,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "QrText",
                table: "ProcDocuments",
                type: "nvarchar(512)",
                maxLength: 512,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "ProcDocuments",
                type: "nvarchar(16)",
                maxLength: 16,
                nullable: false,
                defaultValue: "");
        }
    }
}
