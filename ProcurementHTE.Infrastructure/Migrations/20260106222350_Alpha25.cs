using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProcurementHTE.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Alpha25 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProcDocumentApprovals");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ProcDocumentApprovals",
                columns: table => new
                {
                    ProcDocumentApprovalId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ApproverId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    AssignedApproverId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    ProcDocumentId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ProcurementId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    RoleId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ApprovedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Level = table.Column<int>(type: "int", nullable: false),
                    Note = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProcDocumentApprovals", x => x.ProcDocumentApprovalId);
                    table.ForeignKey(
                        name: "FK_ProcDocumentApprovals_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProcDocumentApprovals_AspNetUsers_ApproverId",
                        column: x => x.ApproverId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProcDocumentApprovals_AspNetUsers_AssignedApproverId",
                        column: x => x.AssignedApproverId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProcDocumentApprovals_ProcDocuments_ProcDocumentId",
                        column: x => x.ProcDocumentId,
                        principalTable: "ProcDocuments",
                        principalColumn: "ProcDocumentId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProcDocumentApprovals_Procurements_ProcurementId",
                        column: x => x.ProcurementId,
                        principalTable: "Procurements",
                        principalColumn: "ProcurementId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProcDocumentApprovals_ApproverId",
                table: "ProcDocumentApprovals",
                column: "ApproverId");

            migrationBuilder.CreateIndex(
                name: "IX_ProcDocumentApprovals_AssignedApproverId",
                table: "ProcDocumentApprovals",
                column: "AssignedApproverId");

            migrationBuilder.CreateIndex(
                name: "IX_ProcDocumentApprovals_ProcDocumentId",
                table: "ProcDocumentApprovals",
                column: "ProcDocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_ProcDocumentApprovals_ProcurementId",
                table: "ProcDocumentApprovals",
                column: "ProcurementId");

            migrationBuilder.CreateIndex(
                name: "IX_ProcDocumentApprovals_RoleId",
                table: "ProcDocumentApprovals",
                column: "RoleId");
        }
    }
}
