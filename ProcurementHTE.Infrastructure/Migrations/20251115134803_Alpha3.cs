using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProcurementHTE.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Alpha3 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AccrualAmount",
                table: "Procurements");

            migrationBuilder.DropColumn(
                name: "RealizationAmount",
                table: "Procurements");

            migrationBuilder.AddColumn<decimal>(
                name: "AccrualAmount",
                table: "ProfitLosses",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "RealizationAmount",
                table: "ProfitLosses",
                type: "decimal(18,2)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AccrualAmount",
                table: "ProfitLosses");

            migrationBuilder.DropColumn(
                name: "RealizationAmount",
                table: "ProfitLosses");

            migrationBuilder.AddColumn<decimal>(
                name: "AccrualAmount",
                table: "Procurements",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "RealizationAmount",
                table: "Procurements",
                type: "decimal(18,2)",
                nullable: true);
        }
    }
}
