using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using ProcurementHTE.Core.Constants;
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
    IExcelExporter excel,
    IMemoryCache cache
) : Controller
{
    private const long MaxImportBytes = 60L * 1024 * 1024;
    private static readonly TimeSpan ImportHold = TimeSpan.FromMinutes(20);
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    [HttpGet]
    public async Task<IActionResult> Index(string? search, int page = 1, int pageSize = 25, CancellationToken ct = default) =>
        View(await archive.SearchAsync(Query(search, page, pageSize), ct));

    [HttpGet]
    public async Task<IActionResult> Details(string id, CancellationToken ct = default)
    {
        var row = await archive.GetAsync(id, ct);
        if (row is null)
            return NotFound();

        var values = JsonSerializer.Deserialize<List<LdpArchiveValue>>(row.ValuesJson, JsonOptions) ?? [];
        return View(new LdpArchiveDetailsViewModel { Row = row, Values = values });
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
                var byLabel = cells[index].ToDictionary(cell => cell.Label, ExcelValue);
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
    public async Task<IActionResult> Delete(string id, CancellationToken ct = default)
    {
        var row = await archive.FindForEditAsync(id, ct);
        if (row is null)
            return NotFound();

        archive.Remove(row);
        await archive.SaveAsync(ct);

        TempData["SuccessMessage"] = "Baris dipindahkan ke Data Terhapus.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    [Authorize(Roles = "Admin")]
    public IActionResult Import() => View(new LdpImportPageViewModel());

    // Reads the file and shows what an import would change. Nothing is written yet: the
    // parsed rows wait in memory for the Admin who uploaded them to confirm.
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin")]
    [RequestSizeLimit(MaxImportBytes)]
    [RequestFormLimits(MultipartBodyLengthLimit = MaxImportBytes)]
    public async Task<IActionResult> ImportPreview(IFormFile? file, CancellationToken ct = default)
    {
        if (file is null || file.Length == 0 || !Path.GetExtension(file.FileName).Equals(".xlsx", StringComparison.OrdinalIgnoreCase))
        {
            ModelState.AddModelError(string.Empty, "Pilih file Excel berformat .xlsx.");
            return View(nameof(Import), new LdpImportPageViewModel());
        }

        LdpImportFile parsed;
        try
        {
            await using var stream = file.OpenReadStream();
            parsed = importer.Read(stream, Path.GetFileName(file.FileName));
        }
        catch (LdpImportException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(nameof(Import), new LdpImportPageViewModel());
        }

        var token = Guid.NewGuid().ToString("N");
        cache.Set(CacheKey(token), parsed, ImportHold);
        return View(nameof(Import), new LdpImportPageViewModel
        {
            Token = token,
            FileName = parsed.FileName,
            Summary = await importer.PreviewAsync(parsed, ct),
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

    // Scoped to the uploader, so another Admin cannot confirm someone else's file.
    private string CacheKey(string token) =>
        $"ldp-import:{User.FindFirstValue(ClaimTypes.NameIdentifier)}:{token}";

    private LdpArchiveQuery Query(string? search, int page, int pageSize) =>
        new(search, Request.ReadListFilters(), Request.ReadListSort(), page, pageSize);

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
            form.Others.Where(other => other.Label.Length > 0).ToDictionary(other => other.Label, other => other.Value)
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
