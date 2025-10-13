using ProcurementHTE.Core.Interfaces;
using ProcurementHTE.Core.Models;

namespace ProcurementHTE.Core.Services
{
    public class WorkOrderService : IWorkOrderService
    {
        private readonly IWorkOrderRepository _woRepository;

        public WorkOrderService(IWorkOrderRepository woRepository)
        {
            _woRepository = woRepository;
        }

        public async Task<IEnumerable<WorkOrder>> GetAllWorkOrderWithDetailsAsync()
        {
            return await _woRepository.GetAllAsync();
        }

        public async Task<WorkOrder?> GetWorkOrderByIdAsync(string id)
        {
            return await _woRepository.GetByIdAsync(id);
        }

        public async Task<IReadOnlyList<WorkOrder>> GetMyRecentWorkOrderAsync(
            string userId,
            int limit = 10,
            CancellationToken ct = default
        )
        {
            return await _woRepository.GetRecentByUserAsync(userId, limit, ct);
        }

        public async Task<int> CountAllWoAsync(CancellationToken ct)
        {
            return await _woRepository.CountAsync(ct);
        }

        public async Task<WoTypes?> GetWoTypeByIdAsync(int id)
        {
            return await _woRepository.GetWoTypeByIdAsync(id);
        }

        public async Task<Status?> GetStatusByNameAsync(string name)
        {
            return await _woRepository.GetStatusByNameAsync(name);
        }

        public async Task<(
            List<WoTypes> WoTypes,
            List<Status> Statuses
        )> GetRelatedEntitiesForWorkOrderAsync()
        {
            var woTypes = await _woRepository.GetWoTypesAsync();
            var statuses = await _woRepository.GetStatusesAsync();

            return (woTypes, statuses);
        }

        public async Task AddWorkOrderAsync(WorkOrder wo)
        {
            if (wo == null)
            {
                throw new ArgumentNullException(nameof(wo), "Work Order tidak boleh null");
            }

            if (wo.WoTypeId is null)
            {
                throw new ArgumentException("Tipe Work Order harus dipilih", nameof(wo.WoTypeId));
            }

            var woType = await _woRepository.GetWoTypeByIdAsync(wo.WoTypeId.Value);
            if (woType is null)
            {
                throw new KeyNotFoundException($"WoType dengan Id {wo.WoTypeId} tidak ditemukan");
            }

            if (wo.StatusId == 0)
            {
                throw new ArgumentException("Status harus dipilih", nameof(wo.StatusId));
            }

            wo.CreatedAt = DateTime.Now;
            await _woRepository.StoreWorkOrderAsync(wo);
        }

        public async Task AddWorkOrderWithDetailsAsync(WorkOrder wo, List<WoDetail> details)
        {
            if (wo == null)
            {
                throw new ArgumentNullException(nameof(wo), "Work order tidak boleh null");
            }

            if (wo.WoTypeId is null)
            {
                throw new ArgumentException("Tipe Work order harus dipilih", nameof(wo.WoTypeId));
            }

            var woType = await _woRepository.GetWoTypeByIdAsync(wo.WoTypeId.Value);
            if (woType is null)
            {
                throw new KeyNotFoundException($"WoType dengan Id {wo.WoTypeId} tidak ditemukan");
            }

            if (wo.StatusId == 0)
            {
                throw new ArgumentException("Status harus dipilih", nameof(wo.StatusId));
            }

            wo.CreatedAt = DateTime.Now;

            details = (details ?? new())
                .Where(d =>
                    !string.IsNullOrWhiteSpace(d.ItemName)
                    && !string.IsNullOrWhiteSpace(d.Unit)
                    && d.Quantity > 0
                )
                .ToList();

            await _woRepository.StoreWorkOrderWithDetailsAsync(wo, details);
        }

        public async Task EditWorkOrderAsync(WorkOrder wo, string id)
        {
            if (wo == null)
            {
                throw new ArgumentNullException(nameof(wo));
            }

            var existingWo = await _woRepository.GetByIdAsync(id);
            if (existingWo == null)
            {
                throw new KeyNotFoundException($"WO with ID {id} not found");
            }

            existingWo.Description = wo.Description;
            existingWo.Note = wo.Note;
            existingWo.WoTypeId = wo.WoTypeId;
            existingWo.StatusId = wo.StatusId;

            await _woRepository.UpdateWorkOrderAsync(existingWo);
        }

        public async Task DeleteWorkOrderAsync(WorkOrder wo)
        {
            if (wo == null)
            {
                throw new ArgumentException(nameof(wo));
            }

            await _woRepository.DropWorkOrderAsync(wo);
        }
    }
}
