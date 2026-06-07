using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;
using ProcurementHTE.Core.Interfaces;
using ProcurementHTE.Core.Models;

namespace ProcurementHTE.Web.Controllers.ProcurementModule;

[Authorize]
public partial class ProcurementsController : Controller
{
    private const string ActivePageName = "Index Procurements";

    private readonly IProcurementService _procurementService;
    private readonly IVendorService _vendorService;
    private readonly IProfitLossService _pnlService;
    private readonly IVendorOfferService _voService;
    private readonly IDocumentGenerator _documentGenerator;
    private readonly IDocumentTypeService _docTypeService;
    private readonly IProcDocumentService _procDocService;
    private readonly IVendorRoundLetterRepository _roundLetterRepository;
    private readonly UserManager<User> _userManager;
    private readonly IUnitTypeRepository _unitTypeRepository;
    private readonly INotificationService _notificationService;
    private readonly IProcurementTrackingService _trackingService;
    private readonly IQrCodeGenerator _qrCodeGenerator;
    private readonly ILogger<ProcurementsController> _logger;

    public ProcurementsController(
        IProcurementService procurementService,
        IVendorService vendorService,
        IProfitLossService pnlService,
        IVendorOfferService voService,
        IDocumentGenerator documentGenerator,
        IDocumentTypeService docTypeService,
        IProcDocumentService procDocService,
        IVendorRoundLetterRepository roundLetterRepository,
        UserManager<User> userManager,
        IUnitTypeRepository unitTypeRepository,
        INotificationService notificationService,
        IProcurementTrackingService trackingService,
        IQrCodeGenerator qrCodeGenerator,
        ILogger<ProcurementsController> logger
    )
    {
        _procurementService = procurementService;
        _vendorService = vendorService;
        _pnlService = pnlService;
        _voService = voService;
        _documentGenerator = documentGenerator;
        _docTypeService = docTypeService;
        _procDocService = procDocService;
        _roundLetterRepository = roundLetterRepository;
        _userManager = userManager;
        _unitTypeRepository = unitTypeRepository;
        _notificationService = notificationService;
        _trackingService = trackingService;
        _qrCodeGenerator = qrCodeGenerator;
        _logger = logger;
    }

    public override void OnActionExecuting(ActionExecutingContext context)
    {
        ViewBag.ActivePage = ActivePageName;
        base.OnActionExecuting(context);
    }
}
