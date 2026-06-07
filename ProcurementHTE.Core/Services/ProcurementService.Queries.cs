using ProcurementHTE.Core.Common;
using ProcurementHTE.Core.Models;

namespace ProcurementHTE.Core.Services;

public partial class ProcurementService
{
    public Task<PagedResult<Procurement>> GetAllProcurementWithDetailsAsync(
        int page,
        int pageSize,
        string? search,
        ISet<string> fields,
        CancellationToken ct,
        string? userId
    )
    {
        return _procurementRepository.GetAllAsync(page, pageSize, search, fields, ct, userId);
    }

    public async Task<Procurement?> GetProcurementByIdAsync(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("ID tidak boleh kosong", nameof(id));

        return await _procurementRepository.GetByIdAsync(id);
    }

    public async Task<IReadOnlyList<Procurement>> GetMyRecentProcurementAsync(
        string userId,
        int limit = 10,
        CancellationToken ct = default
    )
    {
        if (string.IsNullOrWhiteSpace(userId))
            throw new ArgumentException("User ID tidak boleh kosong", nameof(userId));

        if (limit <= 0)
            throw new ArgumentException("Limit harus lebih dari 0", nameof(limit));

        return await _procurementRepository.GetRecentByUserAsync(userId, limit, ct);
    }

    public Task<int> CountAllProcurementsAsync(CancellationToken ct)
    {
        return _procurementRepository.CountAsync(ct);
    }

    public Task<PagedResult<Procurement>> GetProcurementsForAppoApprovalAsync(
        int page,
        int pageSize,
        string? search,
        ISet<string> fields,
        CancellationToken ct
    )
    {
        return _procurementRepository.GetProcurementsForAppoApprovalAsync(
            page,
            pageSize,
            search,
            fields,
            ct
        );
    }

    public Task<PagedResult<Procurement>> GetMyAppoPickupsAsync(
        string appoUserId,
        int page,
        int pageSize,
        string? search,
        ISet<string> fields,
        CancellationToken ct
    )
    {
        return _procurementRepository.GetMyAppoPickupsAsync(
            appoUserId,
            page,
            pageSize,
            search,
            fields,
            ct
        );
    }

    public Task<JobTypes?> GetJobTypeByIdAsync(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("ID tipe pekerjaan tidak boleh kosong", nameof(id));

        return _procurementRepository.GetJobTypeByIdAsync(id);
    }

    public Task<Status?> GetStatusByNameAsync(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Nama status tidak boleh kosong", nameof(name));

        return _procurementRepository.GetStatusByNameAsync(name);
    }

    public async Task<(
        List<JobTypes> JobTypes,
        List<Status> Statuses
    )> GetRelatedEntitiesForProcurementAsync()
    {
        var jobTypes = await _procurementRepository.GetJobTypesAsync();
        var statuses = await _procurementRepository.GetStatusesAsync();
        return (jobTypes, statuses);
    }
}
