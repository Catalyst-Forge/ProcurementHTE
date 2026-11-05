using Microsoft.Extensions.Logging;
using ProcurementHTE.Core.Constants;
using ProcurementHTE.Core.Enums;
using ProcurementHTE.Core.Models;
using ProcurementHTE.Core.Models.DTOs;

namespace ProcurementHTE.Core.Services;

public partial class ProcurementTrackingService
{
    public async Task<PrSubmitState?> GetPrSubmitStateAsync(string prId, CancellationToken ct = default)
    {
        var pr = await _prRepo.GetWithTrackingIncludesByPrIdAsync(prId, ct);
        return pr == null
            ? null
            : new PrSubmitState(
                prId,
                pr.PrNumber,
                PrSubmitSteps.Current(pr.Procurements.Select(p => p.ProcurementStatus))
            );
    }

    public async Task<ProcurementTrackingResponse> SubmitInproAsync(
        string prId,
        PrSubmitActor actor,
        string inproNumber,
        DateTime inproDate,
        DateTime inproSubmitDate,
        PrFileUpload file,
        CancellationToken ct = default
    )
    {
        var (jobs, error) = await LoadPrJobsAsync(prId, ProcurementStatus.OnSubmitISPA, actor, ct);
        if (error != null)
            return Failed(error);

        var missingRa = jobs
            .Where(p => p.RequiresRaNumber && string.IsNullOrWhiteSpace(p.RaNumber))
            .Select(p => p.ProcNum)
            .ToList();
        if (missingRa.Count > 0)
            return Failed($"Nomor RA belum diisi untuk: {string.Join(", ", missingRa)}.");

        var objectKey = await UploadPrFileAsync(prId, "inpro", file, ct);
        if (objectKey == null)
            return Failed("Gagal mengupload file INPRO. Silakan coba lagi.");

        var now = _timeProvider.GetUtcNow().UtcDateTime;
        foreach (var procurement in jobs)
        {
            procurement.IspaNumber = inproNumber;
            procurement.IspaDate = inproDate;
            procurement.IspaSubmitDate = inproSubmitDate;
            procurement.IspaSubmittedAt = now;
            procurement.IspaSubmittedByUserId = actor.UserId;
            procurement.IspaFileName = file.FileName;
            procurement.IspaFileObjectKey = objectKey;
            procurement.IspaFileContentType = file.ContentType;
            procurement.IspaFileSize = file.Size;
            procurement.UpdatedAt = now;

            await _procurementRepo.UpdateProcurementAsync(procurement);
            await UpdateProcurementStatusAsync(
                procurement.ProcurementId,
                ProcurementStatus.OnSubmitHardcopy,
                actor.UserId,
                $"INPRO Number: {inproNumber}, Tanggal INPRO: {inproDate:dd MMM yyyy}, Tanggal Submit: {inproSubmitDate:dd MMM yyyy}",
                ct
            );
        }

        return Succeeded("INPRO berhasil disubmit.");
    }

    public async Task<ProcurementTrackingResponse> SubmitHardcopyAsync(
        string prId,
        PrSubmitActor actor,
        PrFileUpload file,
        CancellationToken ct = default
    )
    {
        var (jobs, error) = await LoadPrJobsAsync(prId, ProcurementStatus.OnSubmitHardcopy, actor, ct);
        if (error != null)
            return Failed(error);

        var objectKey = await UploadPrFileAsync(prId, "evidence-hardcopy", file, ct);
        if (objectKey == null)
            return Failed("Gagal mengupload hardcopy evidence. Silakan coba lagi.");

        var now = _timeProvider.GetUtcNow().UtcDateTime;
        foreach (var procurement in jobs)
        {
            procurement.HardcopyEvidenceFileName = file.FileName;
            procurement.HardcopyEvidenceFilePath = objectKey;
            procurement.HardcopyEvidenceContentType = file.ContentType;
            procurement.HardcopyEvidenceFileSize = file.Size;
            procurement.HardcopySubmittedAt = now;
            procurement.HardcopySubmittedByUserId = actor.UserId;
            procurement.UpdatedAt = now;

            await _procurementRepo.UpdateProcurementAsync(procurement);
            await UpdateProcurementStatusAsync(
                procurement.ProcurementId,
                ProcurementStatus.OnSubmitPO,
                actor.UserId,
                "Hardcopy evidence uploaded",
                ct
            );
        }

        return Succeeded("Hardcopy evidence berhasil disubmit.");
    }

    public async Task<ProcurementTrackingResponse> SubmitPoAsync(
        string prId,
        PrSubmitActor actor,
        string poNumber,
        CancellationToken ct = default
    )
    {
        var (jobs, error) = await LoadPrJobsAsync(prId, ProcurementStatus.OnSubmitPO, actor, ct);
        if (error != null)
            return Failed(error);

        // Done PO ends the procurement, so the lifecycle status must follow;
        // otherwise the details page keeps showing "In Progress".
        var completedStatus = await _procurementRepo.GetStatusByNameAsync("Completed");
        if (completedStatus == null)
            _logger.LogWarning("Status 'Completed' tidak ditemukan; PR {PrId} tetap pada status lamanya", prId);

        var now = _timeProvider.GetUtcNow().UtcDateTime;
        foreach (var procurement in jobs)
        {
            procurement.PoNumber = poNumber;
            procurement.PoSubmittedAt = now;
            procurement.PoSubmittedByUserId = actor.UserId;
            procurement.UpdatedAt = now;
            if (completedStatus != null)
            {
                procurement.StatusId = completedStatus.StatusId;
                procurement.CompletedAt = now;
            }

            await _procurementRepo.UpdateProcurementAsync(procurement);
            await UpdateProcurementStatusAsync(
                procurement.ProcurementId,
                ProcurementStatus.DonePO,
                actor.UserId,
                $"PO Number: {poNumber}",
                ct
            );
        }

        return Succeeded("PO berhasil disubmit. Procurement selesai!");
    }

    private async Task<(List<Procurement> Jobs, string? Error)> LoadPrJobsAsync(
        string prId,
        ProcurementStatus step,
        PrSubmitActor actor,
        CancellationToken ct
    )
    {
        var pr = await _prRepo.GetWithTrackingIncludesByPrIdAsync(prId, ct);
        if (pr == null)
            return ([], "PR tidak ditemukan.");

        if (PrSubmitSteps.Current(pr.Procurements.Select(p => p.ProcurementStatus)) != step)
            return ([], "Semua pekerjaan di PR ini harus berada di tahap yang sama sebelum disubmit.");

        var live = pr.Procurements.Where(p => p.ProcurementStatus != ProcurementStatus.Cancelled).ToList();
        if (!actor.IsAdmin && live.Any(p => p.AppoUserId != actor.UserId))
            return ([], "PR ini hanya bisa disubmit oleh AP-PO yang pick up pekerjaannya.");

        var jobs = new List<Procurement>();
        foreach (var job in live)
        {
            var procurement = await _procurementRepo.GetByIdAsync(job.ProcurementId);
            if (procurement != null)
                jobs.Add(procurement);
        }

        return (jobs, null);
    }

    private async Task<string?> UploadPrFileAsync(
        string prId,
        string folder,
        PrFileUpload file,
        CancellationToken ct
    )
    {
        var objectKey = $"purchase-requisitions/{prId}/{folder}/{Guid.NewGuid():N}-{file.FileName}";
        try
        {
            await _objectStorage.UploadAsync(
                _storageOptions.Bucket,
                objectKey,
                file.Content,
                file.Size,
                file.ContentType,
                ct
            );
            return objectKey;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upload {Folder} file for PR {PrId}", folder, prId);
            return null;
        }
    }

    private static ProcurementTrackingResponse Succeeded(string message) =>
        new() { Success = true, Message = message };

    private static ProcurementTrackingResponse Failed(string message) =>
        new() { Success = false, Message = message };
}
