using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using ProcurementHTE.Core.Interfaces;
using ProcurementHTE.Core.Models;
using ProcurementHTE.Core.Models.DTOs;
using ProcurementHTE.Infrastructure.Data;
using ProcurementHTE.Infrastructure.Services;

namespace ProcurementHTE.Tests.Services;

public class RecordPurgeTests : IDisposable
{
    private const string AdminId = "admin-1";
    private const string Reason = "Data percobaan sebelum go-live";

    private readonly ServiceProvider _services;
    private readonly AppDbContext _db;
    private readonly RecordingStorage _storage = new();
    private readonly RecordPurgeService _purge;

    public RecordPurgeTests()
    {
        var services = new ServiceCollection();
        var databaseName = Guid.NewGuid().ToString();
        services.AddSingleton<ICurrentUserAccessor>(new FixedUser(AdminId));
        services.AddScoped<SoftDeleteInterceptor>();
        services.AddDbContext<AppDbContext>(
            (provider, options) => options
                .UseInMemoryDatabase(databaseName)
                .AddInterceptors(provider.GetRequiredService<SoftDeleteInterceptor>())
        );
        _services = services.BuildServiceProvider();
        _db = _services.CreateScope().ServiceProvider.GetRequiredService<AppDbContext>();
        _db.Users.Add(new User { Id = AdminId, UserName = "admin", FirstName = "Admin", LastName = "Satu" });
        _db.SaveChanges();
        _purge = new RecordPurgeService(
            _db,
            _storage,
            Options.Create(new ObjectStorageOptions { Bucket = "test" }),
            NullLogger<RecordPurgeService>.Instance
        );
    }

    public void Dispose() => _services.Dispose();

    private PurgeRequest Request(DeletedRecordKind kind, string id, string reason = Reason) =>
        new(kind, id, reason, AdminId, "10.0.0.1");

    private async Task<Procurement> DeletedJobAsync(string? inproKey = null, bool isDeleted = true)
    {
        var job = new Procurement { ProcNum = "PROC-1", SpmpNumber = "0003/PDC-1110/2026-S0", JobName = "Angkut rig", IsDeleted = isDeleted, IspaFileObjectKey = inproKey };
        _db.Procurements.Add(job);
        await _db.SaveChangesAsync();
        return job;
    }

    private async Task<Vendor> DeletedVendorAsync()
    {
        var vendor = new Vendor { VendorName = "PT Uji", VendorCode = "V-1", Address = "A", City = "C", Email = "v@test", NPWP = "N", Province = "P", IsDeleted = true };
        _db.Vendors.Add(vendor);
        await _db.SaveChangesAsync();
        return vendor;
    }

    [Fact]
    public async Task Purging_a_job_removes_it_with_its_children_and_files_and_writes_one_log()
    {
        var job = await DeletedJobAsync(inproKey: "inpro/1.pdf");
        var type = new DocumentType { Name = "OE", Description = "OE" };
        _db.AddRange(
            type,
            new ProcDocuments { ProcurementId = job.ProcurementId, DocumentTypeId = type.DocumentTypeId, ObjectKey = "docs/1.pdf", FileName = "1.pdf", ContentType = "application/pdf" },
            new ProcurementStatusHistory { ProcurementId = job.ProcurementId, Note = "x" }
        );
        await _db.SaveChangesAsync();

        var result = await _purge.PurgeAsync(Request(DeletedRecordKind.Procurement, job.ProcurementId));

        Assert.True(result.Succeeded, result.Message);
        Assert.Empty(await _db.Procurements.IgnoreQueryFilters().ToListAsync());
        Assert.Empty(await _db.ProcDocuments.IgnoreQueryFilters().ToListAsync());
        Assert.Empty(await _db.ProcurementStatusHistories.ToListAsync());
        Assert.Equal(["docs/1.pdf", "inpro/1.pdf"], _storage.Deleted.Order());

        var log = Assert.Single(await _db.PermanentDeletionLogs.ToListAsync());
        Assert.Equal("Procurement", log.Kind);
        Assert.Equal("SPMP 0003", log.Title);
        Assert.Equal(Reason, log.Reason);
        Assert.Equal(2, log.RelatedRows);
        Assert.Equal(2, log.FileCount);
        Assert.Equal("admin", log.DeletedByName);
        Assert.Equal("10.0.0.1", log.IpAddress);
    }

    [Fact]
    public async Task A_record_that_is_not_soft_deleted_cannot_be_purged()
    {
        var job = await DeletedJobAsync(isDeleted: false);

        var result = await _purge.PurgeAsync(Request(DeletedRecordKind.Procurement, job.ProcurementId));

        Assert.False(result.Succeeded);
        Assert.Single(await _db.Procurements.ToListAsync());
        Assert.Empty(await _db.PermanentDeletionLogs.ToListAsync());
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("pendek")]
    public async Task A_short_reason_is_refused(string reason)
    {
        var job = await DeletedJobAsync();

        var result = await _purge.PurgeAsync(Request(DeletedRecordKind.Procurement, job.ProcurementId, reason));

        Assert.False(result.Succeeded);
        Assert.Single(await _db.Procurements.IgnoreQueryFilters().ToListAsync());
    }

    // ProfitLosses.SelectedVendorId cascades, so purging this vendor would delete the job's P&L.
    [Fact]
    public async Task A_vendor_used_by_a_profit_loss_is_refused_and_nothing_is_removed()
    {
        var vendor = await DeletedVendorAsync();
        var job = await DeletedJobAsync(isDeleted: false);
        _db.ProfitLosses.Add(new ProfitLoss { ProcurementId = job.ProcurementId, SelectedVendorId = vendor.VendorId, NoLetterSelectedVendor = "L-1" });
        await _db.SaveChangesAsync();

        var preview = await _purge.PreviewAsync(DeletedRecordKind.Vendor, vendor.VendorId);
        var result = await _purge.PurgeAsync(Request(DeletedRecordKind.Vendor, vendor.VendorId));

        Assert.Contains("Profit & Loss (1)", preview!.Blockers);
        Assert.False(result.Succeeded);
        Assert.Single(await _db.Vendors.IgnoreQueryFilters().ToListAsync());
        Assert.Single(await _db.ProfitLosses.ToListAsync());
        Assert.Empty(await _db.PermanentDeletionLogs.ToListAsync());
    }

    [Fact]
    public async Task An_unused_vendor_can_be_purged()
    {
        var vendor = await DeletedVendorAsync();

        var result = await _purge.PurgeAsync(Request(DeletedRecordKind.Vendor, vendor.VendorId));

        Assert.True(result.Succeeded, result.Message);
        Assert.Empty(await _db.Vendors.IgnoreQueryFilters().ToListAsync());
    }

    // One PR submit stores the same INPRO file on every job of that PR.
    [Fact]
    public async Task A_shared_file_stays_until_the_last_job_using_it_is_purged()
    {
        var first = await DeletedJobAsync(inproKey: "inpro/shared.pdf");
        var second = await DeletedJobAsync(inproKey: "inpro/shared.pdf");

        var preview = await _purge.PreviewAsync(DeletedRecordKind.Procurement, first.ProcurementId);
        await _purge.PurgeAsync(Request(DeletedRecordKind.Procurement, first.ProcurementId));

        Assert.Equal(0, preview!.FileCount);
        Assert.Empty(_storage.Deleted);

        await _purge.PurgeAsync(Request(DeletedRecordKind.Procurement, second.ProcurementId));

        Assert.Equal(["inpro/shared.pdf"], _storage.Deleted);
    }

    [Fact]
    public async Task A_file_that_cannot_be_deleted_keeps_the_delete_and_adds_a_log_row()
    {
        var job = await DeletedJobAsync(inproKey: "inpro/1.pdf");
        _storage.Fails = true;

        var result = await _purge.PurgeAsync(Request(DeletedRecordKind.Procurement, job.ProcurementId));

        Assert.True(result.Succeeded);
        Assert.Contains("gagal", result.Message);
        Assert.Empty(await _db.Procurements.IgnoreQueryFilters().ToListAsync());

        var logs = await _db.PermanentDeletionLogs.ToListAsync();
        Assert.Equal(2, logs.Count);
        var failure = Assert.Single(logs, log => log.FilesFailed == 1);
        Assert.Equal("inpro/1.pdf", failure.Detail);
    }

    [Fact]
    public async Task A_plain_remove_stays_a_soft_delete_after_a_purge()
    {
        var purged = await DeletedJobAsync();
        var kept = await DeletedJobAsync(isDeleted: false);
        await _purge.PurgeAsync(Request(DeletedRecordKind.Procurement, purged.ProcurementId));

        _db.Procurements.Remove(kept);
        await _db.SaveChangesAsync();

        var row = await _db.Procurements.IgnoreQueryFilters().SingleAsync();
        Assert.Equal(kept.ProcurementId, row.ProcurementId);
        Assert.True(row.IsDeleted);
    }

    [Fact]
    public async Task An_account_with_history_is_refused()
    {
        var user = new User { UserName = "lama", FirstName = "Lama", LastName = string.Empty, IsDeleted = true };
        _db.Users.Add(user);
        _db.Procurements.Add(new Procurement { ProcNum = "PROC-2", JobName = "Job", UserId = user.Id });
        await _db.SaveChangesAsync();

        var result = await _purge.PurgeAsync(Request(DeletedRecordKind.User, user.Id));

        Assert.False(result.Succeeded);
        Assert.Contains("Procurement (1)", result.Message);
        Assert.True(await _db.Users.AnyAsync(x => x.Id == user.Id));
    }

    [Fact]
    public async Task The_log_outlives_the_account_it_names()
    {
        var user = new User { UserName = "uji", Email = "uji@test", FirstName = "Uji", LastName = string.Empty, IsDeleted = true, AvatarObjectKey = "avatar/1.png" };
        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        var result = await _purge.PurgeAsync(Request(DeletedRecordKind.User, user.Id));

        Assert.True(result.Succeeded, result.Message);
        Assert.False(await _db.Users.AnyAsync(x => x.Id == user.Id));
        Assert.Equal(["avatar/1.png"], _storage.Deleted);
        Assert.Equal("uji", Assert.Single(await _db.PermanentDeletionLogs.ToListAsync()).Title);
    }

    private sealed class FixedUser(string id) : ICurrentUserAccessor
    {
        public string? UserId => id;
    }

    private sealed class RecordingStorage : IObjectStorage
    {
        public List<string> Deleted { get; } = [];
        public bool Fails { get; set; }

        public Task DeleteAsync(string bucket, string objectKey, CancellationToken ct = default)
        {
            if (Fails)
                throw new IOException("storage down");

            Deleted.Add(objectKey);
            return Task.CompletedTask;
        }

        public Task UploadAsync(string bucket, string objectKey, Stream content, long size, string contentType, CancellationToken ct = default) =>
            throw new NotSupportedException();

        public Task<string> GetPresignedUrlAsync(string bucket, string objectKey, TimeSpan expiry, CancellationToken ct = default) =>
            throw new NotSupportedException();

        public Task<string> GetPresignedUrlHeaderAsync(string bucket, string objectKey, TimeSpan expiry, IDictionary<string, string>? responseHeaders, CancellationToken ct = default) =>
            throw new NotSupportedException();
    }
}
