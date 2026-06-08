using Microsoft.AspNetCore.Mvc;

namespace ProcurementHTE.Web.Controllers.LDP
{
    public partial class LdpController
    {
        [HttpGet]
        public async Task<IActionResult> DownloadDocument(string procurementId, CancellationToken ct)
        {
            try
            {
                var procurement = await _procurementRepo.GetByIdAsync(procurementId);
                if (procurement == null || string.IsNullOrEmpty(procurement.LdpFileObjectKey))
                {
                    TempData["Error"] = "Dokumen LDP tidak ditemukan.";
                    return RedirectToAction(nameof(Index));
                }

                // Generate presigned URL for download
                var url = await _objectStorage.GetPresignedUrlAsync(
                    _bucketName,
                    procurement.LdpFileObjectKey,
                    TimeSpan.FromMinutes(15),
                    ct
                );

                return Redirect(url);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading LDP document for procurement {ProcurementId}", procurementId);
                TempData["Error"] = "Terjadi kesalahan saat mengunduh dokumen LDP.";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteDocument(string procurementId, CancellationToken ct)
        {
            bool isAjax = Request.Headers.XRequestedWith == "XMLHttpRequest" 
                       || Request.Headers.Accept.ToString().Contains("application/json");
            
            try
            {
                var procurement = await _procurementRepo.GetByIdAsync(procurementId);
                if (procurement == null)
                {
                    if (isAjax) return NotFound(new { success = false, message = "Procurement tidak ditemukan." });
                    TempData["Error"] = "Procurement tidak ditemukan.";
                    return RedirectToAction(nameof(Index));
                }

                if (!string.IsNullOrEmpty(procurement.LdpFileObjectKey))
                {
                    // Delete file from object storage
                    await _objectStorage.DeleteAsync(_bucketName, procurement.LdpFileObjectKey, ct);
                }

                // Clear LDP file info
                procurement.LdpFileName = null;
                procurement.LdpFileObjectKey = null;
                procurement.LdpFileContentType = null;
                procurement.LdpFileSize = null;
                procurement.LdpUploadedAt = null;
                procurement.LdpUploadedByUserId = null;
                procurement.UpdatedAt = DateTime.UtcNow;

                await _procurementRepo.UpdateProcurementAsync(procurement);

                if (isAjax) return Ok(new { success = true, message = "Dokumen LDP berhasil dihapus." });
                TempData["Success"] = "Dokumen LDP berhasil dihapus.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting LDP document for procurement {ProcurementId}", procurementId);
                if (isAjax) return StatusCode(500, new { success = false, message = "Terjadi kesalahan saat menghapus dokumen LDP." });
                TempData["Error"] = "Terjadi kesalahan saat menghapus dokumen LDP.";
                return RedirectToAction(nameof(Index));
            }
        }
    }
}
