using ProcurementHTE.Core.Models;

namespace ProcurementHTE.Core.Interfaces {
  public interface IWorkOrderService {
    Task<IEnumerable<WorkOrder>> GetAllWorkOrderWithDetailsAsync();
    Task<WorkOrder?> GetWorkOrderByIdAsync(string id);
    Task AddWorkOrderAsync(WorkOrder wo);
    Task EditWorkOrderAsync(WorkOrder wo, string id);
    Task DeleteWorkOrderAsync(WorkOrder wo);
    Task<(List<WoTypes> WoTypes, List<Status> Statuses, List<Tender> Tenders)> GetRelatedEntitiesForWorkOrderAsync();
  }
}
