using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProcurementHTE.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class MigrateToProcurementLevelTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ApprovalSentByUserId",
                table: "Procurements",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ApprovalToken",
                table: "Procurements",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ApprovalTokenGeneratedAt",
                table: "Procurements",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HardcopyEvidenceContentType",
                table: "Procurements",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HardcopyEvidenceFileName",
                table: "Procurements",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HardcopyEvidenceFilePath",
                table: "Procurements",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "HardcopyEvidenceFileSize",
                table: "Procurements",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "HardcopySubmittedAt",
                table: "Procurements",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HardcopySubmittedByUserId",
                table: "Procurements",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IspaNumber",
                table: "Procurements",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "IspaSubmittedAt",
                table: "Procurements",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IspaSubmittedByUserId",
                table: "Procurements",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PoNumber",
                table: "Procurements",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PoSubmittedAt",
                table: "Procurements",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PoSubmittedByUserId",
                table: "Procurements",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProcurementStatus",
                table: "Procurements",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "RejectedAt",
                table: "Procurements",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RejectedByUserId",
                table: "Procurements",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RejectionNote",
                table: "Procurements",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ProcurementStatusHistories",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ProcurementId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ChangedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    ChangedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    Note = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProcurementStatusHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProcurementStatusHistories_AspNetUsers_ChangedByUserId",
                        column: x => x.ChangedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ProcurementStatusHistories_Procurements_ProcurementId",
                        column: x => x.ProcurementId,
                        principalTable: "Procurements",
                        principalColumn: "ProcurementId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Procurements_ApprovalSentByUserId",
                table: "Procurements",
                column: "ApprovalSentByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Procurements_HardcopySubmittedByUserId",
                table: "Procurements",
                column: "HardcopySubmittedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Procurements_IspaSubmittedByUserId",
                table: "Procurements",
                column: "IspaSubmittedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Procurements_PoSubmittedByUserId",
                table: "Procurements",
                column: "PoSubmittedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Procurements_RejectedByUserId",
                table: "Procurements",
                column: "RejectedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ProcurementStatusHistories_ChangedByUserId",
                table: "ProcurementStatusHistories",
                column: "ChangedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ProcurementStatusHistories_ProcurementId",
                table: "ProcurementStatusHistories",
                column: "ProcurementId");

            migrationBuilder.CreateIndex(
                name: "IX_ProcurementStatusHistories_ProcurementId_ChangedAt",
                table: "ProcurementStatusHistories",
                columns: new[] { "ProcurementId", "ChangedAt" });

            migrationBuilder.AddForeignKey(
                name: "FK_Procurements_AspNetUsers_ApprovalSentByUserId",
                table: "Procurements",
                column: "ApprovalSentByUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Procurements_AspNetUsers_HardcopySubmittedByUserId",
                table: "Procurements",
                column: "HardcopySubmittedByUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Procurements_AspNetUsers_IspaSubmittedByUserId",
                table: "Procurements",
                column: "IspaSubmittedByUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Procurements_AspNetUsers_PoSubmittedByUserId",
                table: "Procurements",
                column: "PoSubmittedByUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Procurements_AspNetUsers_RejectedByUserId",
                table: "Procurements",
                column: "RejectedByUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            // ===== DATA MIGRATION: Copy PR-level tracking data to Procurements =====

            // Step 1: Set default ProcurementStatus = 'OnCreateDP3' for all existing procurements
            migrationBuilder.Sql(@"
                UPDATE Procurements
                SET ProcurementStatus = 'OnCreateDP3'
                WHERE ProcurementStatus IS NULL OR ProcurementStatus = ''
            ");

            // Step 2: Copy PR tracking data to ALL linked procurements
            // This updates procurement records with their parent PR's tracking information
            migrationBuilder.Sql(@"
                UPDATE p
                SET
                    p.ProcurementStatus = CASE pr.Status
                        WHEN 1 THEN 'OnCreateDP3'
                        WHEN 2 THEN 'WaitingApprovalAnalyst'
                        WHEN 3 THEN 'WaitingApprovalAsstManager'
                        WHEN 4 THEN 'WaitingApprovalManager'
                        WHEN 5 THEN 'OnSubmitISPA'
                        WHEN 6 THEN 'OnSubmitHardcopy'
                        WHEN 7 THEN 'OnSubmitPO'
                        WHEN 8 THEN 'DonePO'
                        WHEN 99 THEN 'Rejected'
                        ELSE 'OnCreateDP3'
                    END,
                    p.IspaNumber = pr.IspaNumber,
                    p.IspaSubmittedAt = pr.IspaSubmittedAt,
                    p.IspaSubmittedByUserId = pr.IspaSubmittedByUserId,
                    p.PoNumber = pr.PoNumber,
                    p.PoSubmittedAt = pr.PoSubmittedAt,
                    p.PoSubmittedByUserId = pr.PoSubmittedByUserId,
                    p.HardcopyEvidenceFileName = pr.HardcopyEvidenceFileName,
                    p.HardcopyEvidenceFilePath = pr.HardcopyEvidenceFilePath,
                    p.HardcopyEvidenceContentType = pr.HardcopyEvidenceContentType,
                    p.HardcopyEvidenceFileSize = pr.HardcopyEvidenceFileSize,
                    p.HardcopySubmittedAt = pr.HardcopySubmittedAt,
                    p.HardcopySubmittedByUserId = pr.HardcopySubmittedByUserId,
                    p.RejectionNote = pr.RejectionNote,
                    p.RejectedAt = pr.RejectedAt,
                    p.RejectedByUserId = pr.RejectedByUserId,
                    p.ApprovalToken = pr.ApprovalToken,
                    p.ApprovalTokenGeneratedAt = pr.ApprovalTokenGeneratedAt,
                    p.ApprovalSentByUserId = pr.ApprovalSentByUserId
                FROM Procurements p
                INNER JOIN PurchaseRequisitions pr ON p.PrId = pr.PrId
                WHERE p.PrId IS NOT NULL
            ");

            // Step 3: Create initial status history record for each procurement based on current status
            migrationBuilder.Sql(@"
                INSERT INTO ProcurementStatusHistories (Id, ProcurementId, Status, ChangedAt, ChangedByUserId, Note)
                SELECT
                    NEWID(),
                    p.ProcurementId,
                    p.ProcurementStatus,
                    ISNULL(p.CreatedAt, GETUTCDATE()),
                    p.UserId,
                    'Initial status migrated from PR-level tracking'
                FROM Procurements p
                WHERE p.ProcurementStatus IS NOT NULL
                  AND p.ProcurementStatus != ''
            ");

            // Note: PR fields are kept for backward compatibility and will be deprecated in future version
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Procurements_AspNetUsers_ApprovalSentByUserId",
                table: "Procurements");

            migrationBuilder.DropForeignKey(
                name: "FK_Procurements_AspNetUsers_HardcopySubmittedByUserId",
                table: "Procurements");

            migrationBuilder.DropForeignKey(
                name: "FK_Procurements_AspNetUsers_IspaSubmittedByUserId",
                table: "Procurements");

            migrationBuilder.DropForeignKey(
                name: "FK_Procurements_AspNetUsers_PoSubmittedByUserId",
                table: "Procurements");

            migrationBuilder.DropForeignKey(
                name: "FK_Procurements_AspNetUsers_RejectedByUserId",
                table: "Procurements");

            migrationBuilder.DropTable(
                name: "ProcurementStatusHistories");

            migrationBuilder.DropIndex(
                name: "IX_Procurements_ApprovalSentByUserId",
                table: "Procurements");

            migrationBuilder.DropIndex(
                name: "IX_Procurements_HardcopySubmittedByUserId",
                table: "Procurements");

            migrationBuilder.DropIndex(
                name: "IX_Procurements_IspaSubmittedByUserId",
                table: "Procurements");

            migrationBuilder.DropIndex(
                name: "IX_Procurements_PoSubmittedByUserId",
                table: "Procurements");

            migrationBuilder.DropIndex(
                name: "IX_Procurements_RejectedByUserId",
                table: "Procurements");

            migrationBuilder.DropColumn(
                name: "ApprovalSentByUserId",
                table: "Procurements");

            migrationBuilder.DropColumn(
                name: "ApprovalToken",
                table: "Procurements");

            migrationBuilder.DropColumn(
                name: "ApprovalTokenGeneratedAt",
                table: "Procurements");

            migrationBuilder.DropColumn(
                name: "HardcopyEvidenceContentType",
                table: "Procurements");

            migrationBuilder.DropColumn(
                name: "HardcopyEvidenceFileName",
                table: "Procurements");

            migrationBuilder.DropColumn(
                name: "HardcopyEvidenceFilePath",
                table: "Procurements");

            migrationBuilder.DropColumn(
                name: "HardcopyEvidenceFileSize",
                table: "Procurements");

            migrationBuilder.DropColumn(
                name: "HardcopySubmittedAt",
                table: "Procurements");

            migrationBuilder.DropColumn(
                name: "HardcopySubmittedByUserId",
                table: "Procurements");

            migrationBuilder.DropColumn(
                name: "IspaNumber",
                table: "Procurements");

            migrationBuilder.DropColumn(
                name: "IspaSubmittedAt",
                table: "Procurements");

            migrationBuilder.DropColumn(
                name: "IspaSubmittedByUserId",
                table: "Procurements");

            migrationBuilder.DropColumn(
                name: "PoNumber",
                table: "Procurements");

            migrationBuilder.DropColumn(
                name: "PoSubmittedAt",
                table: "Procurements");

            migrationBuilder.DropColumn(
                name: "PoSubmittedByUserId",
                table: "Procurements");

            migrationBuilder.DropColumn(
                name: "ProcurementStatus",
                table: "Procurements");

            migrationBuilder.DropColumn(
                name: "RejectedAt",
                table: "Procurements");

            migrationBuilder.DropColumn(
                name: "RejectedByUserId",
                table: "Procurements");

            migrationBuilder.DropColumn(
                name: "RejectionNote",
                table: "Procurements");
        }
    }
}
