using ProcurementHTE.Core.Interfaces;

namespace ProcurementHTE.Core.Services;

public partial class ProcurementService : IProcurementService
{
    private readonly IProcurementRepository _procurementRepository;
    private const string STATUS_COMPLETED = "Completed";

    public ProcurementService(IProcurementRepository procurementRepository)
    {
        _procurementRepository =
            procurementRepository ?? throw new ArgumentNullException(nameof(procurementRepository));
    }
}
