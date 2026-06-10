using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using ProcurementHTE.Core.Interfaces;
using ProcurementHTE.Core.Models;

namespace ProcurementHTE.Web.Controllers.ProcurementModule;

[Authorize]
[Route("ProcurementTracking")]
public partial class ProcurementTrackingController : Controller
{
    private readonly IProcurementTrackingService _trackingService;
    private readonly IDashboardService _dashboardService;
    private readonly UserManager<User> _userManager;
    private readonly ILogger<ProcurementTrackingController> _logger;
    private readonly IQrCodeGenerator _qrCodeGenerator;

    public ProcurementTrackingController(
        IProcurementTrackingService trackingService,
        IDashboardService dashboardService,
        UserManager<User> userManager,
        ILogger<ProcurementTrackingController> logger,
        IQrCodeGenerator qrCodeGenerator
    )
    {
        _trackingService = trackingService;
        _dashboardService = dashboardService;
        _userManager = userManager;
        _logger = logger;
        _qrCodeGenerator = qrCodeGenerator;
    }
}
