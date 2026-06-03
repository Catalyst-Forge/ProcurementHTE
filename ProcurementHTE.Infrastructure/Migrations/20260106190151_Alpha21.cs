using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProcurementHTE.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Alpha21 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "PickedUpAt",
                table: "Procurements",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PickedUpByUserId",
                table: "Procurements",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Procurements_PickedUpByUserId",
                table: "Procurements",
                column: "PickedUpByUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Procurements_AspNetUsers_PickedUpByUserId",
                table: "Procurements",
                column: "PickedUpByUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            // Insert "Waiting Pickup" status if not exists
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM Statuses WHERE StatusName = 'Waiting Pickup')
                BEGIN
                    INSERT INTO Statuses (StatusName) VALUES ('Waiting Pickup')
                END
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Procurements_AspNetUsers_PickedUpByUserId",
                table: "Procurements");

            migrationBuilder.DropIndex(
                name: "IX_Procurements_PickedUpByUserId",
                table: "Procurements");

            migrationBuilder.DropColumn(
                name: "PickedUpAt",
                table: "Procurements");

            migrationBuilder.DropColumn(
                name: "PickedUpByUserId",
                table: "Procurements");
        }
    }
}
