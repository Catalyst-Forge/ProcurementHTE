using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProcurementHTE.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPjsFlags : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "AnalystHtePjs",
                table: "Procurements",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "AssistantManagerPjs",
                table: "Procurements",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "ManagerPjs",
                table: "Procurements",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "OperationDirectorPjs",
                table: "Procurements",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "PresidentDirectorPjs",
                table: "Procurements",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "VicePresidentPjs",
                table: "Procurements",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AnalystHtePjs",
                table: "Procurements");

            migrationBuilder.DropColumn(
                name: "AssistantManagerPjs",
                table: "Procurements");

            migrationBuilder.DropColumn(
                name: "ManagerPjs",
                table: "Procurements");

            migrationBuilder.DropColumn(
                name: "OperationDirectorPjs",
                table: "Procurements");

            migrationBuilder.DropColumn(
                name: "PresidentDirectorPjs",
                table: "Procurements");

            migrationBuilder.DropColumn(
                name: "VicePresidentPjs",
                table: "Procurements");
        }
    }
}
