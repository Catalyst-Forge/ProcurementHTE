using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProcurementHTE.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class CheckUpdate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_WoDetails_WorkOrders_WoNum",
                table: "WoDetails");

            migrationBuilder.DropForeignKey(
                name: "FK_WoDetails_WorkOrders_WorkOrderId",
                table: "WoDetails");

            migrationBuilder.DropIndex(
                name: "IX_WoDetails_WorkOrderId",
                table: "WoDetails");

            migrationBuilder.DropColumn(
                name: "WorkOrderId",
                table: "WoDetails");

            migrationBuilder.AlterColumn<string>(
                name: "WoNum",
                table: "WorkOrders",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "WoNum",
                table: "WoDetails",
                type: "nvarchar(450)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AddUniqueConstraint(
                name: "AK_WorkOrders_WoNum",
                table: "WorkOrders",
                column: "WoNum");

            migrationBuilder.AddForeignKey(
                name: "FK_WoDetails_WorkOrders_WoNum",
                table: "WoDetails",
                column: "WoNum",
                principalTable: "WorkOrders",
                principalColumn: "WoNum");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_WoDetails_WorkOrders_WoNum",
                table: "WoDetails");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_WorkOrders_WoNum",
                table: "WorkOrders");

            migrationBuilder.AlterColumn<string>(
                name: "WoNum",
                table: "WorkOrders",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AlterColumn<string>(
                name: "WoNum",
                table: "WoDetails",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "WorkOrderId",
                table: "WoDetails",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_WoDetails_WorkOrderId",
                table: "WoDetails",
                column: "WorkOrderId");

            migrationBuilder.AddForeignKey(
                name: "FK_WoDetails_WorkOrders_WoNum",
                table: "WoDetails",
                column: "WoNum",
                principalTable: "WorkOrders",
                principalColumn: "WorkOrderId");

            migrationBuilder.AddForeignKey(
                name: "FK_WoDetails_WorkOrders_WorkOrderId",
                table: "WoDetails",
                column: "WorkOrderId",
                principalTable: "WorkOrders",
                principalColumn: "WorkOrderId");
        }
    }
}
