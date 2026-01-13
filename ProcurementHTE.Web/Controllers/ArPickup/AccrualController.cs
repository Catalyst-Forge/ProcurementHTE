using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using ProcurementHTE.Core.Authorization;
using ProcurementHTE.Core.Interfaces;
using ProcurementHTE.Core.Models;
using ProcurementHTE.Web.Models.ViewModels;

namespace ProcurementHTE.Web.Controllers.ArPickup;

[Authorize]
[Route("[controller]")]
public class AccrualController : Controller
{
    private readonly IProcurementService _procurementService;
    private readonly UserManager<User> _userManager;
    private readonly ILogger<AccrualController> _logger;

    public AccrualController(
        IProcurementService procurementService,
        UserManager<User> userManager,
        ILogger<AccrualController> logger)
    {
        _procurementService = procurementService;
        _userManager = userManager;
        _logger = logger;
    }

    /// <summary>
    /// List procurements for AR to fill accrual data
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Index(
        int page = 1,
        int pageSize = 20,
        string? search = null,
        string? filter = "all",
        CancellationToken ct = default)
    {
        try
        {
            var result = await _procurementService.GetProcurementsForAccrualAsync(
                page, pageSize, search, filter, ct);

            var viewModel = new AccrualIndexViewModel
            {
                Procurements = result.Items,
                CurrentPage = page,
                PageSize = pageSize,
                TotalItems = result.Total,
                TotalPages = result.TotalPages,
                Search = search,
                Filter = filter
            };

            return View(viewModel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading accrual list");
            TempData["ErrorMessage"] = "Gagal memuat data accrual.";
            return View(new AccrualIndexViewModel());
        }
    }

    /// <summary>
    /// Get procurement details for accrual modal
    /// </summary>
    [HttpGet("GetDetails/{id}")]
    public async Task<IActionResult> GetDetails(string id)
    {
        try
        {
            var procurement = await _procurementService.GetProcurementByIdAsync(id);
            if (procurement == null)
                return NotFound(new { message = "Procurement tidak ditemukan" });

            return Json(new
            {
                procurementId = procurement.ProcurementId,
                procNum = procurement.ProcNum,
                wonum = procurement.Wonum,
                jobName = procurement.JobName,
                noAccrual = procurement.NoAccrual,
                potensiAccrual = procurement.PotensiAccrual,
                statusAccrual = procurement.StatusAccrual,
                accrualFilledAt = procurement.AccrualFilledAt?.ToString("dd MMM yyyy HH:mm"),
                accrualFilledBy = procurement.AccrualFilledByUser?.FullName
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting procurement details for accrual: {Id}", id);
            return BadRequest(new { message = "Gagal memuat detail procurement" });
        }
    }

    /// <summary>
    /// Save accrual data
    /// </summary>
    [HttpPost("Save")]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "AR,Admin")]
    public async Task<IActionResult> Save([FromBody] AccrualSaveRequest request)
    {
        try
        {
            if (string.IsNullOrEmpty(request.ProcurementId))
                return BadRequest(new { success = false, message = "Procurement ID tidak valid" });

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Unauthorized(new { success = false, message = "User tidak ditemukan" });

            // Validate that procurement has been picked up by AR
            var procurement = await _procurementService.GetProcurementByIdAsync(request.ProcurementId);
            if (procurement == null)
                return NotFound(new { success = false, message = "Procurement tidak ditemukan" });

            // Check if AR has picked up this procurement (unless admin)
            if (!User.IsInRole("Admin") && string.IsNullOrEmpty(procurement.ArUserId))
            {
                return BadRequest(new { success = false, message = "Anda harus pickup procurement ini terlebih dahulu dari menu AR Pickup" });
            }

            await _procurementService.UpdateAccrualDataAsync(
                request.ProcurementId,
                request.NoAccrual,
                request.PotensiAccrual,
                request.StatusAccrual,
                user.Id);

            _logger.LogInformation(
                "Accrual data updated for procurement {ProcurementId} by user {UserId}",
                request.ProcurementId, user.Id);

            return Json(new { success = true, message = "Data accrual berhasil disimpan" });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving accrual data for procurement {ProcurementId}", request.ProcurementId);
            return BadRequest(new { success = false, message = "Gagal menyimpan data accrual" });
        }
    }
}

public class AccrualSaveRequest
{
    public string ProcurementId { get; set; } = null!;
    public string? NoAccrual { get; set; }
    public decimal? PotensiAccrual { get; set; }
    public string? StatusAccrual { get; set; }
}
