using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using ProcurementHTE.Core.Finance;
using ProcurementHTE.Core.Interfaces;
using ProcurementHTE.Core.Models.DTOs;
using ProcurementHTE.Web.Controllers.Ldp;
using ProcurementHTE.Web.Extensions;
using ProcurementHTE.Web.Models.ViewModels;

namespace ProcurementHTE.Web.Controllers.Finance;

/// <summary>
/// Excel export and import for AP-PO, AP-Invoice, Data Accrual, and the LDP. The export takes the
/// rows the page shows (search, column filters, month and year, sort) without paging. An import
/// is previewed first; the plan waits in memory for the user who uploaded it, then each change is
/// saved as filling its cell saves it.
/// </summary>
public static class FinanceImportEndpoints
{
    public const long MaxBytes = 20L * 1024 * 1024;
    private const string View = "FinanceImport";
    private static readonly TimeSpan Hold = TimeSpan.FromMinutes(20);

    public static async Task<IActionResult> ExportAsync(
        IProcurementQueryService queryService,
        IExcelExporter excel,
        HttpRequest request,
        FinanceTableKind kind,
        string sheetName,
        string? search,
        CancellationToken ct
    )
    {
        var rows = await queryService.GetFinanceTableAsync(
            search, request.ReadListFilters(), request.ReadListSort(), 1, int.MaxValue, ct, request.ReadListPeriod());
        var columns = FinanceImport.ExportColumns(kind);
        var bytes = excel.Export(
            sheetName,
            columns.Select(column => column.Header).ToList(),
            rows.Items.Select(row => (IReadOnlyList<object?>)columns.Select(column => column.Value(row)).ToList())
        );
        var file = sheetName.Replace(' ', '-');
        return new FileContentResult(bytes, LdpController.ExcelContentType) { FileDownloadName = $"{file}-{DateTime.Now:yyyyMMdd-HHmm}.xlsx" };
    }

    public static ViewResult Page(Controller controller, FinanceImportTarget target) =>
        controller.View(View, FinanceImportPageViewModel.For(target));

    public static async Task<IActionResult> PreviewAsync(
        Controller controller,
        FinanceImportTarget target,
        IFormFile? file,
        IExcelTableReader reader,
        IFinanceImportService imports,
        IMemoryCache cache,
        CancellationToken ct
    )
    {
        var model = FinanceImportPageViewModel.For(target);
        if (file is null || file.Length == 0 || !Path.GetExtension(file.FileName).Equals(".xlsx", StringComparison.OrdinalIgnoreCase))
        {
            controller.ModelState.AddModelError(string.Empty, "Pilih file Excel berformat .xlsx.");
            return controller.View(View, model);
        }

        try
        {
            using var copy = new MemoryStream();
            await using (var upload = file.OpenReadStream())
                await upload.CopyToAsync(copy, ct);
            copy.Position = 0;

            var plan = await imports.PreviewAsync(target, reader.Read(copy), ct);
            var token = Guid.NewGuid().ToString("N");
            cache.Set(Key(controller.User, target, token), plan, Hold);
            model.Plan = plan;
            model.Token = token;
            model.FileName = Path.GetFileName(file.FileName);
        }
        catch (FinanceImportException ex)
        {
            controller.ModelState.AddModelError(string.Empty, ex.Message);
        }

        return controller.View(View, model);
    }

    public static async Task<IActionResult> ApplyAsync(
        Controller controller,
        FinanceImportTarget target,
        string? token,
        IFinanceImportService imports,
        IMemoryCache cache,
        CancellationToken ct
    )
    {
        var key = Key(controller.User, target, token ?? string.Empty);
        if (!cache.TryGetValue(key, out FinanceImportPlan? plan) || plan is null)
        {
            controller.TempData["ErrorMessage"] = "Pratinjau sudah kedaluwarsa. Unggah ulang file-nya.";
            return controller.RedirectToAction("Import");
        }

        cache.Remove(key);
        var result = await imports.ApplyAsync(plan, controller.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty, ct);
        controller.TempData[result.Failed.Count == 0 ? "SuccessMessage" : "ErrorMessage"] = Summary(result);
        return controller.RedirectToAction("Index");
    }

    public static string Summary(FinanceImportResult result) =>
        result.Failed.Count == 0
            ? $"Impor selesai: {result.Saved} isian tersimpan."
            : $"Impor selesai: {result.Saved} isian tersimpan, {result.Failed.Count} gagal. "
                + string.Join(" ", result.Failed.Take(3).Select(failure => $"{failure.Change.Job} ({failure.Change.Label}): {failure.Reason}"));

    // Scoped to the uploader and the list, so nobody confirms someone else's file into another list.
    private static string Key(ClaimsPrincipal user, FinanceImportTarget target, string token) =>
        $"finance-import:{target}:{user.FindFirstValue(ClaimTypes.NameIdentifier)}:{token}";
}
