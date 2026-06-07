using System.Text;
using Microsoft.AspNetCore.Mvc.Rendering;
using ProcurementHTE.Core.Enums;

namespace ProcurementHTE.Web.Controllers.PR;

public partial class PurchaseRequisitionsController
{
    private async Task PopulateViewBagForCreate()
    {
        var vendors = await _vendorRepository.GetAllAsync();
        ViewBag.Vendors = vendors
            .Select(v => new SelectListItem { Value = v.VendorId, Text = v.VendorName })
            .ToList();

        var jobTypes = await _procurementRepository.GetJobTypesAsync();
        ViewBag.JobTypes = jobTypes
            .Select(j => new SelectListItem { Value = j.JobTypeId, Text = j.TypeName })
            .ToList();

        ViewBag.Categories = Enum.GetValues(typeof(ProcurementCategory))
            .Cast<ProcurementCategory>()
            .Select(c => new SelectListItem
            {
                Value = ((int)c).ToString(),
                Text = c.ToString(),
            })
            .ToList();
    }

    private bool ValidateUploadedPrDocument(IFormFile? documentFile, bool populateViewBag = false)
    {
        if (documentFile == null || documentFile.Length == 0)
        {
            ModelState.AddModelError("DocumentFile", "Please upload a document file.");
            return false;
        }

        if (documentFile.Length > MaxFileSize)
        {
            ModelState.AddModelError("DocumentFile", "File size exceeds 10MB limit.");
            return false;
        }

        var fileExtension = Path.GetExtension(documentFile.FileName).ToLowerInvariant();
        if (AllowedExtensions.Contains(fileExtension))
            return true;

        ModelState.AddModelError(
            "DocumentFile",
            "Invalid file type. Allowed: PDF, DOC, DOCX, XLS, XLSX."
        );
        return false;
    }

    private static string BuildPrDocumentObjectKey(string prId, string fileName)
    {
        var sanitized = SanitizeFileName(fileName);
        return $"purchase-requisitions/{prId}/documents/{Guid.NewGuid():N}-{sanitized}";
    }

    private static string SanitizeFileName(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            return "file.dat";

        var invalid = Path.GetInvalidFileNameChars();
        var builder = new StringBuilder(fileName.Length);
        foreach (var ch in fileName.Trim())
        {
            builder.Append(invalid.Contains(ch) ? '_' : ch);
        }

        return builder.ToString();
    }

    private async Task SafeDeleteFromStorageAsync(string objectKey)
    {
        try
        {
            await _objectStorage.DeleteAsync(
                _storageOptions.Bucket,
                objectKey,
                HttpContext.RequestAborted
            );
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Failed to delete PR document object {ObjectKey} from bucket {Bucket}.",
                objectKey,
                _storageOptions.Bucket
            );
        }
    }
}
