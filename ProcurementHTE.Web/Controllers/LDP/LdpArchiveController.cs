using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using ProcurementHTE.Core.Constants;
using BulkSelection = ProcurementHTE.Core.Common.BulkSelection;
using ProcurementHTE.Core.Interfaces;
using ProcurementHTE.Core.Models;
using ProcurementHTE.Core.Models.DTOs;
using ProcurementHTE.Core.Services;
using ProcurementHTE.Web.Extensions;
using ProcurementHTE.Web.Models.ViewModels;

namespace ProcurementHTE.Web.Controllers.Ldp;

/// <summary>
/// Completed jobs from the LDP spreadsheet. Everyone signed in can read and export them;
/// only Admin imports, adds, edits, or deletes.
/// </summary>
[Authorize]
public class LdpArchiveController(
    ILdpArchiveRepository archive,
    ILdpArchiveImporter importer,
    ILdpArchiveRangeService ranges,
    ILdpService ldp,
    IExcelExporter excel,
    IMemoryCache cache
) : Controller
{
    private const long MaxImportBytes = 60L * 1024 * 1024;
    private static readonly TimeSpan ImportHold = TimeSpan.FromMinutes(20);
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    [HttpGet]
    public async Task<IActionResult> Index(string? search, int page = 1, int pageSize = 25, CancellationToken ct = default)
    {
        ViewData["LdpArchiveRange"] = await ranges.GetAsync(ct);
        return View(await archive.SearchAsync(Query(search, page, pageSize), ct));
    }

    // The same detail page as a Docutrax job's, with tracking from the spreadsheet dates and
    // every spreadsheet value. Arsip LDP and the LDP open it; from names the way back.
    [HttpGet]
    public async Task<IActionResult> Details(string id, string? from = null, CancellationToken ct = default)
    {
        var row = await archive.GetAsync(id, ct);
        var job = row is null ? null : await ldp.GetArchiveByIdAsync(id, ct);
        if (row is null || job is null)
            return NotFound();

        return View(new JobDetailViewModel
        {
            Job = job,
            ArchiveSteps = LdpArchiveTracking.Steps(row),
            ExcelValues = JsonSerializer.Deserialize<List<LdpArchiveValue>>(row.ValuesJson, JsonOptions) ?? [],
            Origin = JobDetailOrigin.From(from, JobDetailOrigin.Arsip),
        });
    }

    [HttpGet]
    public async Task<IActionResult> Export(string? search, CancellationToken ct = default)
    {
        var rows = await archive.ExportAsync(Query(search, 1, int.MaxValue), ct);
        var cells = rows.Select(row => LdpArchiveEditor.Read(row.ValuesJson)).ToList();
        var labels = cells.SelectMany(list => list.Select(cell => cell.Label)).Distinct().ToList();

        var bytes = excel.Export(
            "Arsip LDP",
            ["Status", .. labels],
            rows.Select((row, index) =>
            {
                var byLabel = cells[index].GroupBy(cell => cell.Label).ToDictionary(group => group.Key, group => ExcelValue(group.First()));
                return (IReadOnlyList<object?>)[row.Status, .. labels.Select(label => byLabel.GetValueOrDefault(label))];
            })
        );
        return File(bytes, LdpController.ExcelContentType, $"Arsip-LDP-{DateTime.Now:yyyyMMdd-HHmm}.xlsx");
    }

    [HttpGet]
    [Authorize(Roles = "Admin")]
    public IActionResult Create() => View("Form", new LdpArchiveFormModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create(LdpArchiveFormModel form, CancellationToken ct = default)
    {
        if (!Validate(form))
            return View("Form", form);

        var row = new LdpArchiveRow { ImportBatch = "Manual" };
        Apply(row, form);
        archive.Add(row);
        await archive.SaveAsync(ct);

        TempData["SuccessMessage"] = "Pekerjaan ditambahkan ke Arsip LDP.";
        return RedirectToAction(nameof(Details), new { id = row.LdpArchiveRowId });
    }

    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Edit(string id, CancellationToken ct = default)
    {
        var row = await archive.GetAsync(id, ct);
        return row is null ? NotFound() : View("Form", LdpArchiveFormModel.From(row));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Edit(string id, LdpArchiveFormModel form, CancellationToken ct = default)
    {
        var row = await archive.FindForEditAsync(id, ct);
        if (row is null)
            return NotFound();
        if (!Validate(form))
            return View("Form", form);

        Apply(row, form);
        await archive.SaveAsync(ct);

        TempData["SuccessMessage"] = "Arsip LDP diperbarui.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(string id, string? returnUrl = null, CancellationToken ct = default)
    {
        var row = await archive.FindForEditAsync(id, ct);
        if (row is null)
            return NotFound();

        archive.Remove(row);
        await archive.SaveAsync(ct);

        TempData["SuccessMessage"] = "Baris dipindahkan ke Data Terhapus.";
        return Url.IsLocalUrl(returnUrl) ? Redirect(returnUrl) : RedirectToAction(nameof(Index));
    }

    // The rows ticked on the list, each a soft delete like Delete above.
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteSelected(string[]? ids, string? returnUrl = null, CancellationToken ct = default)
    {
        var selected = BulkSelection.Ids(ids);
        if (selected is null || selected.Count == 0)
        {
            TempData["ErrorMessage"] = selected is null
                ? $"Pilih paling banyak {BulkSelection.Max} baris sekali hapus."
                : "Pilih dulu baris yang mau dihapus.";
        }
        else
        {
            var rows = await archive.FindManyForEditAsync(selected, ct);
            foreach (var row in rows)
                archive.Remove(row);
            await archive.SaveAsync(ct);

            TempData["SuccessMessage"] = $"{rows.Count} baris dipindahkan ke Data Terhapus.";
        }

        return Url.IsLocalUrl(returnUrl) ? Redirect(returnUrl) : RedirectToAction(nameof(Index));
    }

    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Import(CancellationToken ct = default) =>
        View(new LdpImportPageViewModel { Range = await ranges.GetAsync(ct) });

    // Reads the file and shows what an import would change. Nothing is written yet: the
    // parsed rows wait in memory for the Admin who uploaded them to confirm.
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin")]
    [RequestSizeLimit(MaxImportBytes)]
    [RequestFormLimits(MultipartBodyLengthLimit = MaxImportBytes)]
    public async Task<IActionResult> ImportPreview(IFormFile? file, CancellationToken ct = default)
    {
        var range = await ranges.GetAsync(ct);
        if (file is null || file.Length == 0 || !Path.GetExtension(file.FileName).Equals(".xlsx", StringComparison.OrdinalIgnoreCase))
        {
            ModelState.AddModelError(string.Empty, "Pilih file Excel berformat .xlsx.");
            return View(nameof(Import), new LdpImportPageViewModel { Range = range });
        }

        LdpImportFile parsed;
        try
        {
            await using var stream = file.OpenReadStream();
            parsed = importer.Read(stream, Path.GetFileName(file.FileName), range);
        }
        catch (LdpImportException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(nameof(Import), new LdpImportPageViewModel { Range = range });
        }

        var token = Guid.NewGuid().ToString("N");
        cache.Set(CacheKey(token), parsed, ImportHold);
        return View(nameof(Import), new LdpImportPageViewModel
        {
            Token = token,
            FileName = parsed.FileName,
            Summary = await importer.PreviewAsync(parsed, ct),
            Range = parsed.Range,
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> ImportApply(string token, CancellationToken ct = default)
    {
        if (!cache.TryGetValue(CacheKey(token), out LdpImportFile? parsed) || parsed is null)
        {
            TempData["ErrorMessage"] = "Pratinjau sudah kedaluwarsa. Upload ulang file-nya.";
            return RedirectToAction(nameof(Import));
        }

        var summary = await importer.ApplyAsync(parsed, User.FindFirstValue(ClaimTypes.NameIdentifier)!, ct);
        cache.Remove(CacheKey(token));

        TempData["SuccessMessage"] =
            $"Impor selesai: {summary.New} baru, {summary.Updated} diperbarui, {summary.Restored} dipulihkan, {summary.Unchanged} tidak berubah.";
        return RedirectToAction(nameof(Index));
    }

    // The start dates of archived jobs that show in the LDP instead of here.
    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Range(CancellationToken ct = default)
    {
        var current = await ranges.GetAsync(ct);
        return View(await RangePage(current.From, current.To, ct));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Range(LdpArchiveRangeViewModel form, CancellationToken ct = default)
    {
        var result = await ranges.ChangeAsync(form.From, form.To, User.FindFirstValue(ClaimTypes.NameIdentifier)!, ct);
        switch (result)
        {
            case LdpArchiveRangeResult.Invalid invalid:
                ModelState.AddModelError(string.Empty, invalid.Message);
                return View(await RangePage(form.From, form.To, ct));

            case LdpArchiveRangeResult.Changed changed:
                TempData["SuccessMessage"] = $"Rentang LDP diubah menjadi {changed.Range.Text}."
                    + (changed.Widened ? " Impor ulang Excel LDP supaya pekerjaan bulan baru yang sudah ada No PO ikut masuk." : "");
                return RedirectToAction(nameof(Index));

            default:
                TempData["SuccessMessage"] = "Rentang tidak berubah.";
                return RedirectToAction(nameof(Range));
        }
    }

    private async Task<LdpArchiveRangeViewModel> RangePage(DateTime? from, DateTime? to, CancellationToken ct) => new()
    {
        From = from,
        To = to,
        Current = await ranges.GetAsync(ct),
        History = await ranges.HistoryAsync(ct),
    };

    // Scoped to the uploader, so another Admin cannot confirm someone else's file.
    private string CacheKey(string token) =>
        $"ldp-import:{User.FindFirstValue(ClaimTypes.NameIdentifier)}:{token}";

    private LdpArchiveQuery Query(string? search, int page, int pageSize) =>
        new(search, Request.ReadListFilters(), Request.ReadListSort(), page, pageSize, Request.ReadListPeriod());

    private bool Validate(LdpArchiveFormModel form)
    {
        foreach (var column in LdpArchiveColumns.Typed)
        {
            if (!LdpArchiveEditor.IsValid(column.Kind, form.Typed.GetValueOrDefault(column.Property)))
                ModelState.AddModelError($"Typed[{column.Property}]", $"{column.Label} tidak valid.");
        }

        if (string.IsNullOrWhiteSpace(form.Typed.GetValueOrDefault("JobName")))
            ModelState.AddModelError("Typed[JobName]", "Nama Pekerjaan wajib diisi.");

        return ModelState.IsValid;
    }

    private void Apply(LdpArchiveRow row, LdpArchiveFormModel form)
    {
        LdpArchiveEditor.Apply(
            row,
            form.Status,
            form.Typed,
            form.Others.Where(other => other.Label.Length > 0).GroupBy(other => other.Label).ToDictionary(group => group.Key, group => group.First().Value)
        );
        row.UpdatedAt = DateTime.UtcNow;
        row.UpdatedByUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
    }

    private static object? ExcelValue(LdpArchiveEditor.Cell cell) =>
        cell.Value is null ? null
        : cell.Type == "number" && decimal.TryParse(cell.Value.ToString(), System.Globalization.NumberStyles.Number, System.Globalization.CultureInfo.InvariantCulture, out var number) ? number
        : cell.Type == "date" && DateTime.TryParse(cell.Value.ToString(), System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out var date) ? date
        : cell.Value.ToString();
}
