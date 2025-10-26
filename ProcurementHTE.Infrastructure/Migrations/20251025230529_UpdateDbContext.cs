using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProcurementHTE.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateDbContext : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_WoDocumentApprovals_WoDocumentId_Level",
                table: "WoDocumentApprovals");

            migrationBuilder.CreateIndex(
                name: "IX_WoDocumentApprovals_WoDocumentId",
                table: "WoDocumentApprovals",
                column: "WoDocumentId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_WoDocumentApprovals_WoDocumentId",
                table: "WoDocumentApprovals");

            migrationBuilder.CreateIndex(
                name: "IX_WoDocumentApprovals_WoDocumentId_Level",
                table: "WoDocumentApprovals",
                columns: new[] { "WoDocumentId", "Level" },
                unique: true);
        }
    }
}
