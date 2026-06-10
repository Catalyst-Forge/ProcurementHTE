using ProcurementHTE.Core.Models;

namespace ProcurementHTE.Core.Interfaces
{
    public interface IProcurementCommandService
    {
        Task AddProcurementWithDetailsAsync(
            Procurement procurement,
            List<ProcDetail> details,
            List<ProcOffer> offers
        );
        Task EditProcurementAsync(
            Procurement procurement,
            string id,
            List<ProcDetail> details,
            List<ProcOffer> offers
        );
        Task DeleteProcurementAsync(Procurement procurement, string deletedByUserId);
    }
}
