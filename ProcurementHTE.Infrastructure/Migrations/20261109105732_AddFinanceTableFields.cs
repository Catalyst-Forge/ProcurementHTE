using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProcurementHTE.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddFinanceTableFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "ApprovalAwaitingSend",
                table: "Procurements",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "InvoiceDate",
                table: "Procurements",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InvoiceNumber",
                table: "Procurements",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LoketSubmitDate",
                table: "Procurements",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "NilaiAccrual",
                table: "Procurements",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "NilaiRealisasi",
                table: "Procurements",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PicApInvoice",
                table: "Procurements",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PicAppo",
                table: "Procurements",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PicAr",
                table: "Procurements",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PrNumber",
                table: "Procurements",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "SaDate",
                table: "Procurements",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "Sp3Date",
                table: "Procurements",
                type: "datetime2",
                nullable: true);

            // The finance tables read typed values from now on, so carry over what the
            // pickup flow and the PR menu used to hold. Each statement runs through EXEC so
            // SQL Server compiles it after the new columns exist, also in an idempotent script
            // that sends the whole migration as one batch.
            migrationBuilder.Sql(
                """
                EXEC(N'UPDATE p SET p.PrNumber = LEFT(pr.PrNumber, 100)
                FROM Procurements p
                JOIN PurchaseRequisitions pr ON pr.PrId = p.PrId
                WHERE p.PrNumber IS NULL');

                EXEC(N'UPDATE p SET p.PicAppo = LEFT(u.FullName, 150)
                FROM Procurements p
                JOIN AspNetUsers u ON u.Id = p.AppoUserId
                WHERE p.PicAppo IS NULL');

                EXEC(N'UPDATE p SET p.PicApInvoice = LEFT(u.FullName, 150)
                FROM Procurements p
                JOIN AspNetUsers u ON u.Id = p.ApInvoiceUserId
                WHERE p.PicApInvoice IS NULL');

                EXEC(N'UPDATE p SET p.PicAr = LEFT(u.FullName, 150)
                FROM Procurements p
                JOIN AspNetUsers u ON u.Id = p.ArUserId
                WHERE p.PicAr IS NULL');

                UPDATE p SET p.StatusId = inProgress.StatusId
                FROM Procurements p
                JOIN Statuses waiting ON waiting.StatusId = p.StatusId AND waiting.StatusName = N'Waiting Pickup'
                CROSS JOIN (SELECT TOP 1 StatusId FROM Statuses WHERE StatusName = N'In Progress') inProgress;

                -- Only Angkutan (spot) keeps a generated P&L; every other type uploads its own.
                UPDATE jtd SET jtd.IsGenerated = 0, jtd.IsUploadRequired = 1
                FROM JobTypeDocuments jtd
                JOIN DocumentTypes dt ON dt.DocumentTypeId = jtd.DocumentTypeId
                JOIN JobTypes jt ON jt.JobTypeId = jtd.JobTypeId
                WHERE dt.Name = N'Profit & Loss' AND jt.TypeName <> N'Angkutan';
                """
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ApprovalAwaitingSend",
                table: "Procurements");

            migrationBuilder.DropColumn(
                name: "InvoiceDate",
                table: "Procurements");

            migrationBuilder.DropColumn(
                name: "InvoiceNumber",
                table: "Procurements");

            migrationBuilder.DropColumn(
                name: "LoketSubmitDate",
                table: "Procurements");

            migrationBuilder.DropColumn(
                name: "NilaiAccrual",
                table: "Procurements");

            migrationBuilder.DropColumn(
                name: "NilaiRealisasi",
                table: "Procurements");

            migrationBuilder.DropColumn(
                name: "PicApInvoice",
                table: "Procurements");

            migrationBuilder.DropColumn(
                name: "PicAppo",
                table: "Procurements");

            migrationBuilder.DropColumn(
                name: "PicAr",
                table: "Procurements");

            migrationBuilder.DropColumn(
                name: "PrNumber",
                table: "Procurements");

            migrationBuilder.DropColumn(
                name: "SaDate",
                table: "Procurements");

            migrationBuilder.DropColumn(
                name: "Sp3Date",
                table: "Procurements");
        }
    }
}
