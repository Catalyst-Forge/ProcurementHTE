using System.Security.Claims;
using ProcurementHTE.Core.Models;
using ProcurementHTE.Core.Models.DTOs;
using ProcurementHTE.Web.Mappers;
using ProcurementHTE.Web.Models.ViewModels;
using ProcurementHTE.Web.Utils;

namespace ProcurementHTE.Web.Controllers.ProcurementModule;

public partial class ProcurementsController
{
    private async Task UploadRoundLettersAsync(
        ProfitLossInputViewModel vm,
        string? profitLossId
    )
    {
        if (vm.Vendors == null || vm.Vendors.Count == 0)
            return;

        var docTypes = await GetProcDocumentTypesByNameAsync();
        var sphDocTypeId = FindRequiredDocumentTypeId(docTypes, "Surat Penawaran Harga");
        var snhDocTypeId = FindRequiredDocumentTypeId(docTypes, "Surat Negosiasi Harga");
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var vendorFileLookup = VendorRoundLetterFileMapper.BuildLookup(Request?.Form?.Files);

        for (var vendorIndex = 0; vendorIndex < vm.Vendors.Count; vendorIndex++)
        {
            await UploadVendorRoundLettersAsync(
                vm,
                vendorIndex,
                profitLossId,
                userId,
                vendorFileLookup,
                sphDocTypeId,
                snhDocTypeId
            );
        }

        await _roundLetterRepository.SaveChangesAsync(HttpContext.RequestAborted);
    }

    private async Task UploadVendorRoundLettersAsync(
        ProfitLossInputViewModel vm,
        int vendorIndex,
        string? profitLossId,
        string? userId,
        Dictionary<int, Dictionary<int, Microsoft.AspNetCore.Http.IFormFile>> vendorFileLookup,
        string sphDocTypeId,
        string snhDocTypeId
    )
    {
        var vendor = vm.Vendors[vendorIndex];
        if (vendor == null || string.IsNullOrWhiteSpace(vendor.VendorId))
            return;

        var letters = vendor.Letters ?? [];
        var docIds = vendor.LetterDocIds ?? [];
        var deletes = vendor.LetterDeletes ?? [];
        var files = VendorRoundLetterFileMapper.Merge(vendor.LetterFiles, vendorFileLookup, vendorIndex);
        var maxLength = Math.Max(Math.Max(letters.Count, files.Count), Math.Max(docIds.Count, deletes.Count));

        for (int i = 0; i < maxLength; i++)
        {
            var docId = i < docIds.Count ? docIds[i] : null;
            var file = i < files.Count ? files[i] : null;
            var deleteFlag = i < deletes.Count && deletes[i];
            var hasNewFile = file != null && file.Length > 0;

            if (!await DeleteExistingRoundLetterDocumentAsync(docId, deleteFlag, hasNewFile))
                continue;

            if (!hasNewFile)
                continue;

            await UploadVendorRoundLetterFileAsync(
                vm,
                vendor.VendorId,
                i + 1,
                i < letters.Count ? letters[i] : null,
                file!,
                profitLossId,
                userId,
                i == 0 ? sphDocTypeId : snhDocTypeId
            );
        }
    }

    private async Task<bool> DeleteExistingRoundLetterDocumentAsync(
        string? docId,
        bool deleteFlag,
        bool hasNewFile
    )
    {
        if (string.IsNullOrWhiteSpace(docId) || (!deleteFlag && !hasNewFile))
            return true;

        try
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            await _procDocService.DeleteAsync(docId, currentUserId);
        }
        catch (Exception ex)
        {
            ModelState.AddModelError("", $"Gagal menghapus dokumen SPH/SNH {docId}: {ex.Message}");
        }

        await _roundLetterRepository.DeleteByProcDocumentIdAsync(docId, HttpContext.RequestAborted);
        return !deleteFlag || hasNewFile;
    }

    private async Task UploadVendorRoundLetterFileAsync(
        ProfitLossInputViewModel vm,
        string vendorId,
        int round,
        string? letter,
        Microsoft.AspNetCore.Http.IFormFile file,
        string? profitLossId,
        string? userId,
        string docTypeId
    )
    {
        var label = round == 1 ? "Surat Penawaran Harga" : "Surat Negosiasi Harga";
        var prefix = round == 1 ? "SPH" : "SNH";
        var baseName = ProcurementReferenceNumberFormatter.SanitizeFileName(
            $"{prefix}_R{round}_{vendorId}"
        );

        await using var stream = file.OpenReadStream();
        var uploadResult = await _procDocService.UploadAsync(
            new UploadProcDocumentRequest
            {
                ProcurementId = vm.ProcurementId,
                DocumentTypeId = docTypeId,
                Content = stream,
                Size = file.Length,
                FileName = $"{baseName}.pdf",
                ContentType = "application/pdf",
                Description = $"{label} Ronde {round} - Vendor {vendorId}",
                UploadedByUserId = userId,
                NowUtc = DateTime.UtcNow,
            },
            HttpContext.RequestAborted
        );

        await _roundLetterRepository.AddOrUpdateAsync(
            new VendorRoundLetter
            {
                ProcurementId = vm.ProcurementId,
                VendorId = vendorId,
                Round = round,
                LetterNumber = string.IsNullOrWhiteSpace(letter) ? null : letter.Trim(),
                ProcDocumentId = uploadResult.ProcDocumentId,
                ProfitLossId = profitLossId,
                CreatedAt = DateTime.UtcNow,
                CreatedByUserId = userId,
            }
        );
    }
}
