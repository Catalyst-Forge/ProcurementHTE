using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProcurementHTE.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FixDocumentRelation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_WoDocuments_WorkOrders_WorkOrderId1",
                table: "WoDocuments");

            migrationBuilder.DropIndex(
                name: "IX_WoDocuments_WorkOrderId1",
                table: "WoDocuments");

            migrationBuilder.DropColumn(
                name: "WorkOrderId1",
                table: "WoDocuments");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "WorkOrderId1",
                table: "WoDocuments",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_WoDocuments_WorkOrderId1",
                table: "WoDocuments",
                column: "WorkOrderId1");

            migrationBuilder.AddForeignKey(
                name: "FK_WoDocuments_WorkOrders_WorkOrderId1",
                table: "WoDocuments",
                column: "WorkOrderId1",
                principalTable: "WorkOrders",
                principalColumn: "WorkOrderId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
