using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ProcurementHTE.Core.Interfaces;

namespace ProcurementHTE.Core.Services;

public partial class ProcurementTrackingService : IProcurementTrackingService
{
    private readonly IProcurementRepository _procurementRepo;
    private readonly IPurchaseRequisitionTrackingRepository _prRepo;
    private readonly IUserRepository _userRepo;
    private readonly ILogger<ProcurementTrackingService> _logger;
    private readonly INotificationService _notificationService;
    private readonly INotificationPusher _notificationPusher;
    private readonly IQrCodeGenerator _qrCodeGenerator;
    private readonly IObjectStorage _objectStorage;
    private readonly ObjectStorageOptions _storageOptions;
    private readonly TimeProvider _timeProvider;

    public ProcurementTrackingService(
        IProcurementRepository procurementRepo,
        IPurchaseRequisitionTrackingRepository prRepo,
        IUserRepository userRepo,
        ILogger<ProcurementTrackingService> logger,
        INotificationService notificationService,
        INotificationPusher notificationPusher,
        IQrCodeGenerator qrCodeGenerator,
        IObjectStorage objectStorage,
        IOptions<ObjectStorageOptions> storageOptions,
        TimeProvider timeProvider
    )
    {
        _procurementRepo = procurementRepo;
        _prRepo = prRepo;
        _userRepo = userRepo;
        _logger = logger;
        _notificationService = notificationService;
        _notificationPusher = notificationPusher;
        _qrCodeGenerator = qrCodeGenerator;
        _objectStorage = objectStorage;
        _storageOptions = storageOptions.Value;
        _timeProvider = timeProvider;
    }
}
