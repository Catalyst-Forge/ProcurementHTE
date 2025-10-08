using ProcurementHTE.Core.Interfaces;
using ProcurementHTE.Core.Models;

namespace ProcurementHTE.Core.Services {
  public class WorkOrderService : IWorkOrderService {
    private readonly IWorkOrderRepository _woRepository;

    public WorkOrderService(IWorkOrderRepository woRepository) {
      _woRepository = woRepository;
    }

    public async Task<IEnumerable<WorkOrder>> GetAllWorkOrderWithDetailsAsync() {
      return await _woRepository.GetAllAsync();
    }

    public async Task<WorkOrder?> GetWorkOrderByIdAsync(string id) {
      return await _woRepository.GetByIdAsync(id);
    }

    public async Task<(List<WoTypes> WoTypes, List<Status> Statuses, List<Tender> Tenders)> GetRelatedEntitiesForWorkOrderAsync() {
      var woTypes = await _woRepository.GetWoTypesAsync();
      var statuses = await _woRepository.GetStatusesAsync();
      var tenders = await _woRepository.GetTendersAsync();

      return (woTypes, statuses, tenders);
    }

    public async Task AddWorkOrderAsync(WorkOrder wo) {
      if (wo == null) {
        throw new ArgumentNullException(nameof(wo), "Work Order tidak boleh null");
      }

      if (string.IsNullOrWhiteSpace(wo.WoName)) {
        throw new ArgumentException("Nama Work Order tidak boleh kosong", nameof(wo.WoName));
      }

      if (wo.WoTypeId == 0) {
        throw new ArgumentException("Tipe Work Order harus dipilih", nameof(wo.WoTypeId));
      }

      if (wo.StatusId == 0) {
        throw new ArgumentException("Status harus dipilih", nameof(wo.StatusId));
      }

      if (string.IsNullOrWhiteSpace(wo.TenderId)) {
        throw new ArgumentException("Tender harus dipilih", nameof(wo.TenderId));
      }

      wo.CreatedAt = DateTime.Now;
      await _woRepository.StoreWorkOrderAsync(wo);
    }

    public async Task EditWorkOrderAsync(WorkOrder wo, string id) {
      if (wo == null) {
        throw new ArgumentNullException(nameof(wo));
      }

      var existingWo = await _woRepository.GetByIdAsync(id);
      if (existingWo == null) {
        throw new KeyNotFoundException($"WO with ID {id} not found");
      }

      existingWo.WoName = wo.WoName;
      existingWo.Description = wo.Description;
      existingWo.Note = wo.Note;
      existingWo.WoTypeId = wo.WoTypeId;
      existingWo.StatusId = wo.StatusId;
      existingWo.TenderId = wo.TenderId;

      await _woRepository.UpdateWorkOrderAsync(existingWo);
    }

    public async Task DeleteWorkOrderAsync(WorkOrder wo) {
      if (wo == null) {
        throw new ArgumentException(nameof(wo));
      }

      await _woRepository.DropWorkOrderAsync(wo);
    }
  }
}
