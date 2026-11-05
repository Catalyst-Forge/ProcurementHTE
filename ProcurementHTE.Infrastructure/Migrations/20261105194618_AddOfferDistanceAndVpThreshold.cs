using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProcurementHTE.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddOfferDistanceAndVpThreshold : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Distance",
                table: "ProcOffers",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.Sql(
                "UPDATE DocumentApprovalRules SET MinAmount = 300000000 WHERE MinAmount = 500000000"
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "UPDATE DocumentApprovalRules SET MinAmount = 500000000 WHERE MinAmount = 300000000"
            );

            migrationBuilder.DropColumn(
                name: "Distance",
                table: "ProcOffers");
        }
    }
}
