using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProcurementHTE.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Alpha10 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SequenceOrder",
                table: "ProcDocumentApprovals");

            migrationBuilder.DropColumn(
                name: "SequenceOrder",
                table: "DocumentApprovals");

            migrationBuilder.AddColumn<int>(
                name: "ProcurementCategory",
                table: "Procurements",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ProcurementCategory",
                table: "JobTypeDocuments",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ProcurementCategory",
                table: "Procurements");

            migrationBuilder.DropColumn(
                name: "ProcurementCategory",
                table: "JobTypeDocuments");

            migrationBuilder.AddColumn<int>(
                name: "SequenceOrder",
                table: "ProcDocumentApprovals",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "SequenceOrder",
                table: "DocumentApprovals",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }
    }
}
