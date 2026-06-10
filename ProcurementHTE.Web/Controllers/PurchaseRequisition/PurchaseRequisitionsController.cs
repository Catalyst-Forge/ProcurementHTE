using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using ProcurementHTE.Core.Interfaces;

namespace ProcurementHTE.Web.Controllers.PR;

[Authorize(Roles = "Admin, AP-PO")]
public partial class PurchaseRequisitionsController : Controller
{
    private const string ActivePageName = "Index PR Service";
    private const long MaxFileSize = 10 * 1024 * 1024;

    private static readonly string[] AllowedExtensions =
    [
        ".pdf",
        ".doc",
        ".docx",
        ".xls",
        ".xlsx",
    ];

    private readonly IProcurementRepository _procurementRepository;
    private readonly IVendorRepository _vendorRepository;
    private readonly IProfitLossRepository _profitLossRepository;
    private readonly IPurchaseRequisitionQueryService _purchaseRequisitionQueryService;
    private readonly IPurchaseRequisitionCommandService _purchaseRequisitionCommandService;
    private readonly IProcurementDocumentQuery _procurementDocumentQuery;
    private readonly IProcDocumentService _procDocumentService;
    private readonly IVendorRoundLetterRepository _vendorRoundLetterRepository;
    private readonly IProcurementDocumentGenerator _documentGenerator;
    private readonly IDocumentTypeRepository _documentTypeRepository;
    private readonly IProcurementQueryService _queryService;
    private readonly IObjectStorage _objectStorage;
    private readonly ObjectStorageOptions _storageOptions;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IProcurementTrackingService _procurementTrackingService;
    private readonly ILogger<PurchaseRequisitionsController> _logger;

    public PurchaseRequisitionsController(
        IProcurementRepository procurementRepository,
        IVendorRepository vendorRepository,
        IProfitLossRepository profitLossRepository,
        IPurchaseRequisitionQueryService purchaseRequisitionQueryService,
        IPurchaseRequisitionCommandService purchaseRequisitionCommandService,
        IProcurementDocumentQuery procurementDocumentQuery,
        IProcDocumentService procDocumentService,
        IVendorRoundLetterRepository vendorRoundLetterRepository,
        IProcurementDocumentGenerator documentGenerator,
        IDocumentTypeRepository documentTypeRepository,
        IProcurementQueryService queryService,
        IObjectStorage objectStorage,
        IOptions<ObjectStorageOptions> storageOptions,
        IHttpClientFactory httpClientFactory,
        IProcurementTrackingService procurementTrackingService,
        ILogger<PurchaseRequisitionsController> logger
    )
    {
        _procurementRepository = procurementRepository;
        _vendorRepository = vendorRepository;
        _profitLossRepository = profitLossRepository;
        _purchaseRequisitionQueryService = purchaseRequisitionQueryService;
        _purchaseRequisitionCommandService = purchaseRequisitionCommandService;
        _procurementDocumentQuery = procurementDocumentQuery;
        _procDocumentService = procDocumentService;
        _vendorRoundLetterRepository = vendorRoundLetterRepository;
        _documentGenerator = documentGenerator;
        _documentTypeRepository = documentTypeRepository;
        _queryService = queryService;
        _objectStorage = objectStorage;
        _storageOptions =
            storageOptions?.Value ?? throw new ArgumentNullException(nameof(storageOptions));
        _httpClientFactory = httpClientFactory;
        _procurementTrackingService = procurementTrackingService;
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }
}
