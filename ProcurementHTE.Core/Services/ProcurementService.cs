using ProcurementHTE.Core.Interfaces;

namespace ProcurementHTE.Core.Services;

public partial class ProcurementService : IProcurementQueryService, IProcurementCommandService, IProcurementWorkflowService
{
    private readonly IProcurementRepository _procurementRepository;
    private readonly TimeProvider _timeProvider;
    private const string STATUS_COMPLETED = "Completed";

    public ProcurementService(IProcurementRepository procurementRepository, TimeProvider timeProvider)
    {
        _procurementRepository =
            procurementRepository ?? throw new ArgumentNullException(nameof(procurementRepository));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }
}
