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
