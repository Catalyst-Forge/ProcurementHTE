using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProcurementHTE.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Alpha12 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DocumentApprovalRules",
                columns: table => new
                {
                    DocumentApprovalRuleId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    DocumentTypeId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    JobTypeId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    ProcurementCategory = table.Column<int>(type: "int", nullable: true),
                    MinAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    MaxAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    SubmitterRoleId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ApproverRoleId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Sequence = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentApprovalRules", x => x.DocumentApprovalRuleId);
                    table.ForeignKey(
                        name: "FK_DocumentApprovalRules_DocumentTypes_DocumentTypeId",
                        column: x => x.DocumentTypeId,
                        principalTable: "DocumentTypes",
                        principalColumn: "DocumentTypeId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DocumentApprovalRules_JobTypes_JobTypeId",
                        column: x => x.JobTypeId,
                        principalTable: "JobTypes",
                        principalColumn: "JobTypeId");
                });

            migrationBuilder.CreateIndex(
                name: "IX_DocumentApprovalRules_DocumentTypeId_JobTypeId_ProcurementCategory_MinAmount_MaxAmount_IsActive",
                table: "DocumentApprovalRules",
                columns: new[] { "DocumentTypeId", "JobTypeId", "ProcurementCategory", "MinAmount", "MaxAmount", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_DocumentApprovalRules_JobTypeId",
                table: "DocumentApprovalRules",
                column: "JobTypeId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DocumentApprovalRules");
        }
    }
}
