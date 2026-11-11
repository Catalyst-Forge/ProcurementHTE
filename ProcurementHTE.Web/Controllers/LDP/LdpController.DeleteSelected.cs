using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProcurementHTE.Core.Interfaces;
using BulkSelection = ProcurementHTE.Core.Common.BulkSelection;

namespace ProcurementHTE.Web.Controllers.Ldp
{
    public partial class LdpController
    {
        /// <summary>
        /// The rows an Admin ticked on the LDP. A row from the old spreadsheet is deleted like
        /// Arsip LDP's Hapus; a Docutrax job like the job's own Hapus, with its documents and
        /// P&amp;L. Either way it lands in Data Terhapus and can be restored from there.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteSelected(
            string[]? archiveIds,
            string[]? procurementIds,
            [FromServices] ILdpArchiveRepository archive,
            [FromServices] IProcurementDeletionService deletion,
            string? returnUrl = null,
            CancellationToken ct = default)
        {
            var archived = BulkSelection.Ids(archiveIds);
            var jobs = BulkSelection.Ids(procurementIds);
            var back = Url.IsLocalUrl(returnUrl) ? Redirect(returnUrl) : (IActionResult)RedirectToAction(nameof(Index));

            if (archived is null || jobs is null || archived.Count + jobs.Count > BulkSelection.Max)
            {
                TempData["ErrorMessage"] = $"Pilih paling banyak {BulkSelection.Max} baris sekali hapus.";
                return back;
            }
            if (archived.Count + jobs.Count == 0)
            {
                TempData["ErrorMessage"] = "Pilih dulu baris yang mau dihapus.";
                return back;
            }

            var rows = await archive.FindManyForEditAsync(archived, ct);
            foreach (var row in rows)
                archive.Remove(row);
            await archive.SaveAsync(ct);

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var deleted = rows.Count;
            var failed = 0;
            foreach (var id in jobs)
            {
                try
                {
                    if (await deletion.DeleteAsync(id, userId) is not null)
                        deleted++;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error deleting procurement {ProcurementId} from the LDP selection", id);
                    failed++;
                }
            }

            if (failed > 0)
                TempData["ErrorMessage"] = $"{deleted} baris dipindahkan ke Data Terhapus, {failed} gagal dihapus. Coba hapus yang gagal satu per satu.";
            else
                TempData["SuccessMessage"] = $"{deleted} baris dipindahkan ke Data Terhapus.";

            return back;
        }
    }
}
