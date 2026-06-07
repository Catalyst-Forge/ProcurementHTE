using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ProcurementHTE.Core.Interfaces;

namespace ProcurementHTE.Core.Services;

public sealed partial class ProcDocumentService : IProcDocumentService
{
    private readonly IProcDocumentRepository _procDocumentRepository;
    private readonly IProcurementRepository _procurementRepository;
    private readonly IJobTypeDocumentRepository _jobTypeDocumentRepository;
    private readonly IProfitLossService _pnlService;
    private readonly IDocumentTypeRepository _documentTypeRepository;
    private readonly IObjectStorage _objectStorage;
    private readonly ObjectStorageOptions _storageOptions;
    private readonly ILogger<ProcDocumentService> _logger;

    public ProcDocumentService(
        IProcDocumentRepository procDocumentRepository,
        IProcurementRepository procurementRepository,
        IJobTypeDocumentRepository jobTypeDocumentRepository,
        IProfitLossService pnlService,
        IDocumentTypeRepository documentTypeRepository,
        IObjectStorage objectStorage,
        IOptions<ObjectStorageOptions> storageOptions,
        ILogger<ProcDocumentService> logger
    )
    {
        _procDocumentRepository =
            procDocumentRepository
            ?? throw new ArgumentNullException(nameof(procDocumentRepository));
        _procurementRepository =
            procurementRepository ?? throw new ArgumentNullException(nameof(procurementRepository));
        _jobTypeDocumentRepository =
            jobTypeDocumentRepository
            ?? throw new ArgumentNullException(nameof(jobTypeDocumentRepository));
        _pnlService = pnlService ?? throw new ArgumentNullException(nameof(pnlService));
        _documentTypeRepository =
            documentTypeRepository
            ?? throw new ArgumentNullException(nameof(documentTypeRepository));
        _objectStorage = objectStorage ?? throw new ArgumentNullException(nameof(objectStorage));
        _storageOptions =
            storageOptions?.Value ?? throw new ArgumentNullException(nameof(storageOptions));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        if (string.IsNullOrWhiteSpace(_storageOptions.Bucket))
        {
            throw new ArgumentException(
                "Object storage bucket belum dikonfigurasi.",
                nameof(storageOptions)
            );
        }
    }
}
