using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProcurementHTE.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddLdpArchiveRangeChanges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LdpArchiveRangeChanges",
                columns: table => new
                {
                    LdpArchiveRangeChangeId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    StartFrom = table.Column<DateTime>(type: "date", nullable: false),
                    StartTo = table.Column<DateTime>(type: "date", nullable: false),
                    PreviousStartFrom = table.Column<DateTime>(type: "date", nullable: false),
                    PreviousStartTo = table.Column<DateTime>(type: "date", nullable: false),
                    ChangedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    ChangedByName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    ChangedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LdpArchiveRangeChanges", x => x.LdpArchiveRangeChangeId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LdpArchiveRangeChanges_ChangedAt",
                table: "LdpArchiveRangeChanges",
                column: "ChangedAt");

            // Append-only at database level, like PermanentDeletionLogs. EXEC, because the
            // idempotent script wraps every statement in an IF block and CREATE TRIGGER must
            // start its own batch.
            migrationBuilder.Sql("""
                EXEC(N'CREATE TRIGGER TR_LdpArchiveRangeChanges_AppendOnly
                ON LdpArchiveRangeChanges
                INSTEAD OF UPDATE, DELETE
                AS
                BEGIN
                    THROW 50002, N''Riwayat rentang LDP tidak boleh diubah atau dihapus.'', 1;
                END');
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LdpArchiveRangeChanges");
        }
    }
}
