using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ProcurementHTE.Core.Exceptions;
using ProcurementHTE.Core.Interfaces;
using ProcurementHTE.Core.Models;
using ProcurementHTE.Core.Models.DTOs;
using ProcurementHTE.Infrastructure.Data;
using ProcurementHTE.Infrastructure.Services;

namespace ProcurementHTE.Tests.Services;

public class SoftDeleteTests : IDisposable
{
    private const string AdminId = "admin-1";
    private readonly ServiceProvider _services;
    private readonly AppDbContext _db;

    public SoftDeleteTests()
    {
        var services = new ServiceCollection();
        var dbName = Guid.NewGuid().ToString();
        services.AddSingleton<ICurrentUserAccessor>(new FixedUser(AdminId));
        services.AddScoped<SoftDeleteInterceptor>();
        services.AddDbContext<AppDbContext>(
            (sp, o) => o.UseInMemoryDatabase(dbName).AddInterceptors(sp.GetRequiredService<SoftDeleteInterceptor>())
        );
        _services = services.BuildServiceProvider();
        _db = _services.CreateScope().ServiceProvider.GetRequiredService<AppDbContext>();
    }

    public void Dispose() => _services.Dispose();

    private sealed class FixedUser(string id) : ICurrentUserAccessor
    {
        public string? UserId => id;
    }

    private (JobTypes jobType, DocumentType doc, JobTypeDocuments mapping, DocumentApprovals approval, DocumentApprovalRule rule) SeedSetup()
    {
        var jobType = new JobTypes { TypeName = "Moving" };
        var doc = new DocumentType { Name = "Memorandum", Description = "Memorandum" };
        var mapping = new JobTypeDocuments { JobTypeId = jobType.JobTypeId, DocumentTypeId = doc.DocumentTypeId };
        var approval = new DocumentApprovals { JobTypeDocumentId = mapping.JobTypeDocumentId, RoleId = "role-1", Level = 1 };
        var rule = new DocumentApprovalRule { DocumentTypeId = doc.DocumentTypeId, JobTypeId = jobType.JobTypeId, SubmitterRoleId = "role-1" };
        _db.AddRange(jobType, doc, mapping, approval, rule);
        _db.SaveChanges();
        return (jobType, doc, mapping, approval, rule);
    }

    [Fact]
    public async Task Rule_pointing_at_a_removed_role_cannot_be_restored()
    {
        var (_, _, _, _, rule) = SeedSetup();
        _db.DocumentApprovalRules.Remove(rule);
        await _db.SaveChangesAsync();

        var result = await new DeletedRecordsService(_db).RestoreAsync(DeletedRecordKind.DocumentApprovalRule, rule.DocumentApprovalRuleId);

        Assert.False(result.Succeeded);
        Assert.Empty(await _db.DocumentApprovalRules.ToListAsync());
    }

    [Fact]
    public async Task Rule_with_existing_roles_can_be_restored()
    {
        var (_, _, _, _, rule) = SeedSetup();
        _db.Roles.Add(new Role { Id = "role-1", Name = "Operation", NormalizedName = "OPERATION", Description = "Operation" });
        _db.DocumentApprovalRules.Remove(rule);
        await _db.SaveChangesAsync();

        var result = await new DeletedRecordsService(_db).RestoreAsync(DeletedRecordKind.DocumentApprovalRule, rule.DocumentApprovalRuleId);

        Assert.True(result.Succeeded);
        Assert.Single(await _db.DocumentApprovalRules.ToListAsync());
    }

    [Fact]
    public async Task Remove_TurnsIntoSoftDelete_AndRecordsWhoDeleted()
    {
        var (_, _, _, approval, _) = SeedSetup();

        _db.DocumentApprovals.Remove(approval);
        await _db.SaveChangesAsync();

        Assert.Empty(await _db.DocumentApprovals.ToListAsync());
        var stored = await _db.DocumentApprovals.IgnoreQueryFilters().SingleAsync();
        Assert.True(stored.IsDeleted);
        Assert.NotNull(stored.DeletedAt);
        Assert.Equal(AdminId, stored.DeletedBy);
    }

    [Fact]
    public async Task DeleteJobType_IsRefused_WhileAnActiveProcurementUsesIt()
    {
        var (jobType, _, _, _, _) = SeedSetup();
        _db.Procurements.Add(new Procurement { JobTypeId = jobType.JobTypeId, ProcNum = "PROC-1", JobName = "Angkut rig" });
        await _db.SaveChangesAsync();

        var error = await Assert.ThrowsAsync<EntityInUseException>(
            () => MasterDataDeletion.DeleteJobTypeAsync(_db, jobType.JobTypeId)
        );
        Assert.Contains("1 procurement aktif", error.Message);
    }

    [Fact]
    public async Task DeleteJobType_IgnoresDeletedProcurements()
    {
        var (jobType, _, _, _, _) = SeedSetup();
        _db.Procurements.Add(new Procurement { JobTypeId = jobType.JobTypeId, ProcNum = "PROC-1", IsDeleted = true });
        await _db.SaveChangesAsync();

        await MasterDataDeletion.DeleteJobTypeAsync(_db, jobType.JobTypeId);
        await _db.SaveChangesAsync();

        Assert.Empty(await _db.JobTypes.ToListAsync());
    }

    [Fact]
    public async Task DeleteJobType_CascadesToItsSetup_AndRestoreBringsItAllBack()
    {
        var (jobType, _, _, _, _) = SeedSetup();

        await MasterDataDeletion.DeleteJobTypeAsync(_db, jobType.JobTypeId);
        await _db.SaveChangesAsync();

        Assert.Empty(await _db.JobTypeDocuments.ToListAsync());
        Assert.Empty(await _db.DocumentApprovals.ToListAsync());
        Assert.Empty(await _db.DocumentApprovalRules.ToListAsync());
        Assert.Single(await _db.DocumentTypes.ToListAsync()); // the document type itself stays

        var result = await new DeletedRecordsService(_db).RestoreAsync(DeletedRecordKind.JobType, jobType.JobTypeId);

        Assert.True(result.Succeeded, result.Message);
        Assert.Single(await _db.JobTypes.ToListAsync());
        Assert.Single(await _db.JobTypeDocuments.ToListAsync());
        Assert.Single(await _db.DocumentApprovals.ToListAsync());
        Assert.Single(await _db.DocumentApprovalRules.ToListAsync());
        Assert.Null((await _db.JobTypes.SingleAsync()).DeletedBy);
    }

    [Fact]
    public async Task RestoreJobType_KeepsSetupThatWasDeletedSeparatelyEarlier()
    {
        var (jobType, _, mapping, _, _) = SeedSetup();
        await MasterDataDeletion.DeleteJobTypeDocumentAsync(_db, mapping.JobTypeDocumentId);
        await _db.SaveChangesAsync();
        await Task.Delay(5);
        await MasterDataDeletion.DeleteJobTypeAsync(_db, jobType.JobTypeId);
        await _db.SaveChangesAsync();

        await new DeletedRecordsService(_db).RestoreAsync(DeletedRecordKind.JobType, jobType.JobTypeId);

        Assert.Empty(await _db.JobTypeDocuments.ToListAsync());
    }

    [Fact]
    public async Task RestoreProcurement_IsRefused_WhileItsTypeIsDeleted()
    {
        var (jobType, _, _, _, _) = SeedSetup();
        var procurement = new Procurement { JobTypeId = jobType.JobTypeId, ProcNum = "PROC-1", IsDeleted = true };
        _db.Procurements.Add(procurement);
        await _db.SaveChangesAsync();
        await MasterDataDeletion.DeleteJobTypeAsync(_db, jobType.JobTypeId);
        await _db.SaveChangesAsync();

        var result = await new DeletedRecordsService(_db).RestoreAsync(DeletedRecordKind.Procurement, procurement.ProcurementId);

        Assert.False(result.Succeeded);
        Assert.Empty(await _db.Procurements.ToListAsync());
    }

    [Fact]
    public async Task RestoreProcurement_BringsBackItsProfitLossAndDocuments_DeletedWithIt()
    {
        // Procurements/Delete deletes the documents, then the P&L, then the job, seconds apart.
        var jobDeletedAt = new DateTime(2026, 11, 9, 12, 38, 41, DateTimeKind.Utc);
        var job = new Procurement { ProcNum = "PROC-9", IsDeleted = true, DeletedAt = jobDeletedAt, DeletedBy = AdminId };
        ProcDocuments Document(string key, DateTime deletedAt, string deletedBy) => new()
        {
            ProcurementId = job.ProcurementId, DocumentTypeId = "doc-type", ObjectKey = key, FileName = key,
            ContentType = "application/pdf", IsDeleted = true, DeletedAt = deletedAt, DeletedBy = deletedBy,
        };
        var withJob = Document("with-job.pdf", jobDeletedAt.AddSeconds(-2), AdminId);
        var earlier = Document("earlier.pdf", jobDeletedAt.AddDays(-1), AdminId);
        var byOther = Document("by-other.pdf", jobDeletedAt.AddSeconds(-2), "someone-else");
        _db.AddRange(job, withJob, earlier, byOther, new ProfitLoss
        {
            ProcurementId = job.ProcurementId, SelectedVendorId = "vendor-1", NoLetterSelectedVendor = "L-1",
            Profit = 60_000_000, IsDeleted = true, DeletedAt = jobDeletedAt.AddSeconds(-1), DeletedBy = AdminId,
        });
        await _db.SaveChangesAsync();

        var result = await new DeletedRecordsService(_db).RestoreAsync(DeletedRecordKind.Procurement, job.ProcurementId);

        Assert.True(result.Succeeded, result.Message);
        Assert.Equal(60_000_000, (await _db.ProfitLosses.SingleAsync()).Profit);
        Assert.Equal(["with-job.pdf"], await _db.ProcDocuments.Select(d => d.ObjectKey).ToListAsync());
    }

    [Fact]
    public void Rows_deleted_with_a_job_translate_for_sql_server()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer("Server=localhost;Database=translate-only;Trusted_Connection=True;TrustServerCertificate=True")
            .Options;
        using var db = new AppDbContext(options);
        var at = new DateTime(2026, 11, 9, 12, 38, 41, DateTimeKind.Utc);

        var pnl = DeletedRecordsService.DeletedWithJob(db.ProfitLosses.IgnoreQueryFilters(), "job-1", at, AdminId).ToQueryString();
        var documents = DeletedRecordsService.DeletedWithJob(db.ProcDocuments.IgnoreQueryFilters(), "job-1", at, AdminId).ToQueryString();

        Assert.Contains("[DeletedAt] >=", pnl);
        Assert.Contains("[DeletedBy]", documents);
    }

    [Fact]
    public async Task RestoreVendor_UndoesLegacyRename_AndRefusesATakenCode()
    {
        Vendor NewVendor(string code, bool deleted) => new()
        {
            VendorCode = code, VendorName = code, NPWP = "-", Address = "-", City = "-", Province = "-",
            Email = "v@test", IsDeleted = deleted,
        };
        var old = NewVendor("-VND001", deleted: true);
        _db.Vendors.Add(old);
        await _db.SaveChangesAsync();
        var service = new DeletedRecordsService(_db);

        var restored = await service.RestoreAsync(DeletedRecordKind.Vendor, old.VendorId);
        Assert.True(restored.Succeeded, restored.Message);
        Assert.Equal("VND001", (await _db.Vendors.SingleAsync()).VendorCode);

        var other = NewVendor("VND001", deleted: true);
        _db.Vendors.Add(other);
        await _db.SaveChangesAsync();
        var refused = await service.RestoreAsync(DeletedRecordKind.Vendor, other.VendorId);
        Assert.False(refused.Succeeded);
    }

    [Theory]
    [InlineData("DELETED_PR/2026/001_20261009101112123", "PR/2026/001")]
    [InlineData("PR/2026/001", "PR/2026/001")]
    public void StripLegacyPrNumber_RecoversTheOriginalNumber(string stored, string expected) =>
        Assert.Equal(expected, DeletedRecordsService.StripLegacyPrNumber(stored));

    [Fact]
    public async Task RestoreUser_ReactivatesTheAccount()
    {
        var user = new User { UserName = "budi", FirstName = "Budi", IsDeleted = true, IsActive = false };
        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        var result = await new DeletedRecordsService(_db).RestoreAsync(DeletedRecordKind.User, user.Id);

        Assert.True(result.Succeeded, result.Message);
        var stored = await _db.Users.SingleAsync();
        Assert.False(stored.IsDeleted);
        Assert.True(stored.IsActive);
    }
}
