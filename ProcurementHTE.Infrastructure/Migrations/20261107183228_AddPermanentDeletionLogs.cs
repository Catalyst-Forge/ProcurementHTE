using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProcurementHTE.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPermanentDeletionLogs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PermanentDeletionLogs",
                columns: table => new
                {
                    PermanentDeletionLogId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Kind = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    RecordId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Detail = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    RelatedRows = table.Column<int>(type: "int", nullable: false),
                    FileCount = table.Column<int>(type: "int", nullable: false),
                    FilesFailed = table.Column<int>(type: "int", nullable: false),
                    DeletedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    DeletedByName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    IpAddress = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PermanentDeletionLogs", x => x.PermanentDeletionLogId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PermanentDeletionLogs_DeletedAt",
                table: "PermanentDeletionLogs",
                column: "DeletedAt");

            // Append-only at database level. EXEC, because the idempotent script wraps every
            // statement in an IF block and CREATE TRIGGER must start its own batch.
            migrationBuilder.Sql("""
                EXEC(N'CREATE TRIGGER TR_PermanentDeletionLogs_AppendOnly
                ON PermanentDeletionLogs
                INSTEAD OF UPDATE, DELETE
                AS
                BEGIN
                    THROW 50001, N''Log hapus permanen tidak boleh diubah atau dihapus.'', 1;
                END');
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PermanentDeletionLogs");
        }
    }
}
