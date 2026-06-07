using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProcurementHTE.Core.Interfaces;

namespace ProcurementHTE.Web.Controllers.ProcurementModule;

[Authorize(Roles = "Admin, AP-PO")]
public partial class ProcurementDocumentsController : Controller
{
    private static readonly HashSet<string> _roundLetterDocNames = new(
        StringComparer.OrdinalIgnoreCase
    )
    {
        "Surat Penawaran Harga",
        "Surat Negosiasi Harga",
    };

    private readonly IProcurementDocumentQuery _query;
    private readonly IProcDocumentService _docSvc;
    private readonly IProcurementService _procurementService;
    private readonly IVendorService _vendorService;
    private readonly IVendorRoundLetterRepository _roundLetterRepo;
    private readonly IHttpClientFactory _http;
    private readonly IProcurementDocumentGenerator _documentGenerator;
    private readonly IDocumentTypeRepository _docTypeRepo;
    private readonly IProcurementTrackingService _trackingService;

    public ProcurementDocumentsController(
        IProcurementDocumentQuery query,
        IProcurementService procurementService,
        IProcDocumentService docSvc,
        IVendorService vendorService,
        IVendorRoundLetterRepository roundLetterRepo,
        IHttpClientFactory http,
        IProcurementDocumentGenerator documentGenerator,
        IDocumentTypeRepository docTypeRepo,
        IProcurementTrackingService trackingService
    )
    {
        _query = query;
        _docSvc = docSvc;
        _procurementService = procurementService;
        _vendorService = vendorService;
        _roundLetterRepo = roundLetterRepo;
        _http = http;
        _documentGenerator = documentGenerator;
        _docTypeRepo = docTypeRepo;
        _trackingService = trackingService;
    }
}
