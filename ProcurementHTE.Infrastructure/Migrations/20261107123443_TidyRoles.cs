using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProcurementHTE.Infrastructure.Migrations
{
    /// <summary>
    /// Brings roles and approval data in line with the client's role list. It runs before
    /// RoleSeeder, so the renamed role keeps its id and is not created a second time.
    /// </summary>
    public partial class TidyRoles : Migration
    {
        private const string RemovedRoles =
            "N'DEWAN DIREKSI', N'DEWAN KOMISARIS', N'HSE', N'HTE', "
            + "N'OPERATION DIRECTOR', N'PRESIDENT DIRECTOR', N'SUPPLY CHAIN MANAGEMENT'";

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Stop before changing anything if a role to remove has gained a user.
            migrationBuilder.Sql($"""
                IF EXISTS (
                    SELECT 1
                    FROM AspNetUserRoles ur
                    JOIN AspNetRoles r ON r.Id = ur.RoleId
                    WHERE r.NormalizedName IN ({RemovedRoles})
                )
                    THROW 50000, N'Masih ada user pada role yang akan dihapus.', 1;
                """);

            migrationBuilder.Sql("""
                UPDATE AspNetRoles
                SET Name = N'Asst. Manager Transport & Logistic',
                    NormalizedName = N'ASST. MANAGER TRANSPORT & LOGISTIC',
                    Description = N'Asst. Manager Transport & Logistic system role',
                    ConcurrencyStamp = CONVERT(nvarchar(36), NEWID())
                WHERE NormalizedName = N'ASSISTANT MANAGER HTE';
                """);

            // HSE signs first on Risk Assessment, so the rest of that chain moves up one level.
            migrationBuilder.Sql("""
                UPDATE da
                SET [Level] = da.[Level] - 1
                FROM DocumentApprovals da
                JOIN AspNetRoles r ON r.Id = da.RoleId
                WHERE r.NormalizedName <> N'HSE'
                  AND da.JobTypeDocumentId IN (
                      SELECT hse.JobTypeDocumentId
                      FROM DocumentApprovals hse
                      JOIN AspNetRoles hseRole ON hseRole.Id = hse.RoleId
                      WHERE hseRole.NormalizedName = N'HSE'
                  );
                """);

            // Approval stops at the VP at any value, so Manager to VP now covers every range.
            migrationBuilder.Sql("""
                UPDATE rule_
                SET MaxAmount = 99000000000
                FROM DocumentApprovalRules rule_
                JOIN AspNetRoles submitter ON submitter.Id = rule_.SubmitterRoleId
                JOIN AspNetRoles approver ON approver.Id = rule_.ApproverRoleId
                WHERE rule_.IsDeleted = 0
                  AND rule_.MinAmount = 300000000
                  AND rule_.MaxAmount = 5000000000
                  AND submitter.NormalizedName = N'MANAGER TRANSPORT & LOGISTIC'
                  AND approver.NormalizedName = N'VICE PRESIDENT';
                """);

            // These rules have no foreign key to roles, so left alone they would print a role id.
            migrationBuilder.Sql("""
                UPDATE DocumentApprovalRules
                SET IsDeleted = 1,
                    DeletedAt = SYSUTCDATETIME()
                WHERE IsDeleted = 0
                  AND (
                      SubmitterRoleId IN (
                          SELECT Id FROM AspNetRoles
                          WHERE NormalizedName IN (N'OPERATION DIRECTOR', N'PRESIDENT DIRECTOR')
                      )
                      OR ApproverRoleId IN (
                          SELECT Id FROM AspNetRoles
                          WHERE NormalizedName IN (N'OPERATION DIRECTOR', N'PRESIDENT DIRECTOR')
                      )
                  );
                """);

            // Cascades to role claims and to the HSE rows in DocumentApprovals.
            migrationBuilder.Sql($"""
                DELETE FROM AspNetRoles
                WHERE NormalizedName IN ({RemovedRoles});
                """);
        }

        /// <summary>
        /// Only the rename can be undone here. Removed roles, the HSE approvals, and the
        /// approval rules come back from the backup taken before this migration.
        /// </summary>
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE AspNetRoles
                SET Name = N'Assistant Manager HTE',
                    NormalizedName = N'ASSISTANT MANAGER HTE',
                    Description = N'Assistant Manager HTE system role',
                    ConcurrencyStamp = CONVERT(nvarchar(36), NEWID())
                WHERE NormalizedName = N'ASST. MANAGER TRANSPORT & LOGISTIC';
                """);
        }
    }
}
