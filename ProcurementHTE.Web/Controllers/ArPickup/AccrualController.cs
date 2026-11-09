using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using ProcurementHTE.Core.Authorization;
using ProcurementHTE.Core.Finance;
using ProcurementHTE.Core.Interfaces;
using ProcurementHTE.Core.Models;
using ProcurementHTE.Web.Authorization;
using ProcurementHTE.Web.Controllers.Finance;
using ProcurementHTE.Web.Models.ViewModels;

namespace ProcurementHTE.Web.Controllers.ArPickup;

[Authorize]
[Route("[controller]")]
public class AccrualController : Controller
{
    private readonly IProcurementQueryService _queryService;
    private readonly IProcurementWorkflowService _workflowService;
    private readonly UserManager<User> _userManager;
    private readonly ILogger<AccrualController> _logger;
    private readonly IProcurementTrackingService _trackingService;

    public AccrualController(
        IProcurementQueryService queryService,
        IProcurementWorkflowService workflowService,
        UserManager<User> userManager,
        ILogger<AccrualController> logger,
        IProcurementTrackingService trackingService)
    {
        _queryService = queryService;
        _workflowService = workflowService;
        _userManager = userManager;
        _logger = logger;
        _trackingService = trackingService;
    }

    /// <summary>
    /// The Data Accrual table: every published job, No Accrual filled by clicking the cell.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Index(
        int page = 1,
        int pageSize = 25,
        string? search = null,
        CancellationToken ct = default)
    {
        ViewBag.Search = search;
        return View(await FinanceTableEndpoints.LoadAsync(_queryService, Request, search, page, pageSize, ct));
    }

    [HttpPost("Field/{id}")]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = ActionRoles.Accrual)]
    public Task<IActionResult> SaveField(string id, FinanceField field, string? value, CancellationToken ct) =>
        FinanceTableEndpoints.SaveFieldAsync(
            _trackingService,
            _queryService,
            FinanceTableKind.Accrual,
            id,
            field,
            value,
            _userManager.GetUserId(User),
            ct
        );

    [HttpGet("GetDetails/{id}")]
    public async Task<IActionResult> GetDetails(string id)
    {
        try
        {
            var procurement = await _queryService.GetProcurementByIdAsync(id);
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
    [Authorize(Roles = ActionRoles.Accrual)]
    public async Task<IActionResult> Save([FromBody] AccrualSaveRequest request)
    {
        try
        {
            if (string.IsNullOrEmpty(request.ProcurementId))
                return BadRequest(new { success = false, message = "Procurement ID tidak valid" });

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Unauthorized(new { success = false, message = "User tidak ditemukan" });

            var procurement = await _queryService.GetProcurementByIdAsync(request.ProcurementId);
            if (procurement == null)
                return NotFound(new { success = false, message = "Procurement tidak ditemukan" });

            if (!ActionRoles.CanFillAccrual(User))
            {
                return BadRequest(new { success = false, message = "Anda tidak punya akses untuk mengisi data accrual" });
            }

            await _workflowService.UpdateAccrualDataAsync(
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
