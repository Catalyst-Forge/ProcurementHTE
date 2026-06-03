using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProcurementHTE.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Alpha2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Procurements_AspNetUsers_AnalystHteSignerUserId",
                table: "Procurements");

            migrationBuilder.DropForeignKey(
                name: "FK_Procurements_AspNetUsers_AssistantManagerSignerUserId",
                table: "Procurements");

            migrationBuilder.DropForeignKey(
                name: "FK_Procurements_AspNetUsers_ManagerSignerUserId",
                table: "Procurements");

            migrationBuilder.DropForeignKey(
                name: "FK_Procurements_AspNetUsers_PicOpsUserId",
                table: "Procurements");

            migrationBuilder.DropForeignKey(
                name: "FK_Procurements_JobTypes_JobTypeId",
                table: "Procurements");

            migrationBuilder.DropIndex(
                name: "IX_Procurements_AnalystHteSignerUserId",
                table: "Procurements");

            migrationBuilder.DropIndex(
                name: "IX_Procurements_AssistantManagerSignerUserId",
                table: "Procurements");

            migrationBuilder.DropIndex(
                name: "IX_Procurements_ManagerSignerUserId",
                table: "Procurements");

            migrationBuilder.DropIndex(
                name: "IX_Procurements_PicOpsUserId",
                table: "Procurements");

            migrationBuilder.DropColumn(
                name: "AnalystHteSignerUserId",
                table: "Procurements");

            migrationBuilder.DropColumn(
                name: "AssistantManagerSignerUserId",
                table: "Procurements");

            migrationBuilder.DropColumn(
                name: "DistanceKm",
                table: "Procurements");

            migrationBuilder.DropColumn(
                name: "JobType",
                table: "Procurements");

            migrationBuilder.DropColumn(
                name: "ManagerSignerUserId",
                table: "Procurements");

            migrationBuilder.DropColumn(
                name: "SelectedVendorName",
                table: "Procurements");

            migrationBuilder.DropColumn(
                name: "VendorSphNumber",
                table: "Procurements");

            migrationBuilder.AlterColumn<DateTime>(
                name: "StartDate",
                table: "Procurements",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ProjectRegion",
                table: "Procurements",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(16)",
                oldMaxLength: 16,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "PicOpsUserId",
                table: "Procurements",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldMaxLength: 450,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "JobName",
                table: "Procurements",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(255)",
                oldMaxLength: 255,
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "EndDate",
                table: "Procurements",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ContractType",
                table: "Procurements",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AnalystHteUserId",
                table: "Procurements",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "AssistantManagerUserId",
                table: "Procurements",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ManagerUserId",
                table: "Procurements",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<int>(
                name: "Quantity",
                table: "ProcDetails",
                type: "int",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Procurements_JobTypes_JobTypeId",
                table: "Procurements",
                column: "JobTypeId",
                principalTable: "JobTypes",
                principalColumn: "JobTypeId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Procurements_JobTypes_JobTypeId",
                table: "Procurements");

            migrationBuilder.DropColumn(
                name: "AnalystHteUserId",
                table: "Procurements");

            migrationBuilder.DropColumn(
                name: "AssistantManagerUserId",
                table: "Procurements");

            migrationBuilder.DropColumn(
                name: "ManagerUserId",
                table: "Procurements");

            migrationBuilder.AlterColumn<DateTime>(
                name: "StartDate",
                table: "Procurements",
                type: "datetime2",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AlterColumn<string>(
                name: "ProjectRegion",
                table: "Procurements",
                type: "nvarchar(16)",
                maxLength: 16,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<string>(
                name: "PicOpsUserId",
                table: "Procurements",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldMaxLength: 450);

            migrationBuilder.AlterColumn<string>(
                name: "JobName",
                table: "Procurements",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(255)",
                oldMaxLength: 255);

            migrationBuilder.AlterColumn<DateTime>(
                name: "EndDate",
                table: "Procurements",
                type: "datetime2",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AlterColumn<string>(
                name: "ContractType",
                table: "Procurements",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);

            migrationBuilder.AddColumn<string>(
                name: "AnalystHteSignerUserId",
                table: "Procurements",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AssistantManagerSignerUserId",
                table: "Procurements",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "DistanceKm",
                table: "Procurements",
                type: "decimal(10,2)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "JobType",
                table: "Procurements",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ManagerSignerUserId",
                table: "Procurements",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SelectedVendorName",
                table: "Procurements",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VendorSphNumber",
                table: "Procurements",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "Quantity",
                table: "ProcDetails",
                type: "decimal(18,2)",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Procurements_AnalystHteSignerUserId",
                table: "Procurements",
                column: "AnalystHteSignerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Procurements_AssistantManagerSignerUserId",
                table: "Procurements",
                column: "AssistantManagerSignerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Procurements_ManagerSignerUserId",
                table: "Procurements",
                column: "ManagerSignerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Procurements_PicOpsUserId",
                table: "Procurements",
                column: "PicOpsUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Procurements_AspNetUsers_AnalystHteSignerUserId",
                table: "Procurements",
                column: "AnalystHteSignerUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Procurements_AspNetUsers_AssistantManagerSignerUserId",
                table: "Procurements",
                column: "AssistantManagerSignerUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Procurements_AspNetUsers_ManagerSignerUserId",
                table: "Procurements",
                column: "ManagerSignerUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Procurements_AspNetUsers_PicOpsUserId",
                table: "Procurements",
                column: "PicOpsUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Procurements_JobTypes_JobTypeId",
                table: "Procurements",
                column: "JobTypeId",
                principalTable: "JobTypes",
                principalColumn: "JobTypeId",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
