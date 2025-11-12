using ProcurementHTE.Core.Common;
using ProcurementHTE.Core.Interfaces;
using ProcurementHTE.Core.Models;

namespace ProcurementHTE.Core.Services
{
    public class WorkOrderService : IWorkOrderService
    {
        private readonly IWorkOrderRepository _woRepository;
        private const string STATUS_COMPLETED = "Completed";

        public WorkOrderService(IWorkOrderRepository woRepository)
        {
            _woRepository = woRepository ?? throw new ArgumentNullException(nameof(woRepository));
        }

        #region Query Methods

        public Task<PagedResult<WorkOrder>> GetAllWorkOrderWithDetailsAsync(
            int page,
            int pageSize,
            string? search,
            ISet<string> fields,
            CancellationToken ct
        )
        {
            return _woRepository.GetAllAsync(page, pageSize, search, fields, ct);
        }

        public async Task<WorkOrder?> GetWorkOrderByIdAsync(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("ID tidak boleh kosong", nameof(id));

            return await _woRepository.GetByIdAsync(id);
        }

        public async Task<IReadOnlyList<WorkOrder>> GetMyRecentWorkOrderAsync(
            string userId,
            int limit = 10,
            CancellationToken ct = default
        )
        {
            if (string.IsNullOrWhiteSpace(userId))
                throw new ArgumentException("User ID tidak boleh kosong", nameof(userId));

            if (limit <= 0)
                throw new ArgumentException("Limit harus lebih dari 0", nameof(limit));

            return await _woRepository.GetRecentByUserAsync(userId, limit, ct);
        }

        public Task<int> CountAllWoAsync(CancellationToken ct)
        {
            return _woRepository.CountAsync(ct);
        }

        #endregion

        #region Lookup Methods

        public Task<WoTypes?> GetWoTypeByIdAsync(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("ID tipe WO tidak boleh kosong", nameof(id));

            return _woRepository.GetWoTypeByIdAsync(id);
        }

        public Task<Status?> GetStatusByNameAsync(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Nama status tidak boleh kosong", nameof(name));

            return _woRepository.GetStatusByNameAsync(name);
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

        #endregion

        #region Command Methods

        public async Task AddWorkOrderWithDetailsAsync(
            WorkOrder wo,
            List<WoDetail> details,
            List<WoOffer> offers
        )
        {
            ValidateWorkOrder(wo);
            await ValidateWorkOrderType(wo.WoTypeId!);

            wo.CreatedAt = DateTime.Now;

            var validDetails = FilterValidDetails(details);
            var validOffers = FilterValidOffers(offers);
            await _woRepository.StoreWorkOrderWithDetailsAsync(wo, validDetails, validOffers);
        }

        public async Task EditWorkOrderAsync(
            WorkOrder wo,
            string id,
            List<WoDetail> details,
            List<WoOffer> offers
        )
        {
            if (wo == null)
                throw new ArgumentNullException(nameof(wo), "Work Order tidak boleh null");

            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("ID tidak boleh kosong", nameof(id));

            var existingWo =
                await _woRepository.GetByIdAsync(id)
                ?? throw new KeyNotFoundException($"Work Order dengan ID {id} tidak ditemukan");

            UpdateWorkOrderProperties(existingWo, wo);

            if (!string.IsNullOrWhiteSpace(wo.WoTypeId))
                await ValidateWorkOrderType(wo.WoTypeId);

            var validDetails = FilterValidDetails(details);
            var validOffers = FilterValidOffers(offers);

            await _woRepository.UpdateWorkOrderWithDetailsAsync(
                existingWo,
                validDetails,
                validOffers
            );
        }

        public async Task DeleteWorkOrderAsync(WorkOrder wo)
        {
            ArgumentNullException.ThrowIfNull(wo, nameof(wo));
            await _woRepository.DropWorkOrderAsync(wo);
        }

        public async Task MarkAsCompletedAsync(string woId)
        {
            if (string.IsNullOrWhiteSpace(woId))
                throw new ArgumentException("ID Work Order tidak boleh kosong", nameof(woId));

            var wo =
                await _woRepository.GetByIdAsync(woId)
                ?? throw new KeyNotFoundException($"Work order dengan ID {woId} tidak ditemukan");

            var completedStatus = await GetCompletedStatusAsync();
            wo.StatusId = completedStatus.StatusId;
            wo.CompletedAt = DateTime.Now;
            await _woRepository.UpdateWorkOrderAsync(wo);
        }

        #endregion

        #region Private Helper Methods

        private static void ValidateWorkOrder(WorkOrder wo)
        {
            if (wo == null)
                throw new ArgumentNullException(nameof(wo), "Work Order tidak boleh null");

            if (string.IsNullOrWhiteSpace(wo.WoTypeId))
                throw new ArgumentException("Tipe Work Order harus dipilih", nameof(wo.WoTypeId));

            if (wo.StatusId <= 0)
                throw new ArgumentException("Status harus dipilih", nameof(wo.StatusId));
        }

        private async Task ValidateWorkOrderType(string woTypeId)
        {
            var woType =
                await _woRepository.GetWoTypeByIdAsync(woTypeId)
                ?? throw new KeyNotFoundException(
                    $"Tipe Work Order dengan Id {woTypeId} tidak ditemukan"
                );
        }

        private static List<WoDetail> FilterValidDetails(List<WoDetail>? details)
        {
            return (details ?? [])
                .Where(detail =>
                    !string.IsNullOrWhiteSpace(detail.ItemName)
                    && !string.IsNullOrWhiteSpace(detail.Unit)
                    && detail.Quantity > 0
                )
                .ToList();
        }

        private static List<WoOffer> FilterValidOffers(List<WoOffer>? offers)
        {
            return (offers ?? [])
                .Where(o => !string.IsNullOrWhiteSpace(o.ItemPenawaran))
                .ToList();
        }

        private static void UpdateWorkOrderProperties(WorkOrder existing, WorkOrder updated)
        {
            existing.Description = updated.Description;
            existing.Note = updated.Note;
            existing.WoNumLetter = updated.WoNumLetter;
            existing.DateLetter = updated.DateLetter;
            existing.From = updated.From;
            existing.To = updated.To;
            existing.WorkOrderLetter = updated.WorkOrderLetter;
            existing.WBS = updated.WBS;
            existing.GlAccount = updated.GlAccount;
            existing.DateRequired = updated.DateRequired;
            existing.Requester = updated.Requester;
            existing.Approved = updated.Approved;
            existing.ProcurementType = updated.ProcurementType;
            existing.UpdatedAt = DateTime.Now;
            existing.XS1 = updated.XS1;
            existing.XS2 = updated.XS2;
            existing.XS3 = updated.XS3;
            existing.XS4 = updated.XS4;

            if (!string.IsNullOrWhiteSpace(updated.WoTypeId))
                existing.WoTypeId = updated.WoTypeId;

            if (updated.StatusId > 0)
                existing.StatusId = updated.StatusId;
        }

        private async Task<Status> GetCompletedStatusAsync()
        {
            var statuses = await _woRepository.GetStatusesAsync();
            var completedStatus = statuses
                .Where(status =>
                    status.StatusName.Equals(STATUS_COMPLETED, StringComparison.OrdinalIgnoreCase)
                )
                .OrderByDescending(status => status.StatusId)
                .FirstOrDefault();

            if (completedStatus == null)
                throw new InvalidOperationException(
                    $"Status '{STATUS_COMPLETED}' tidak ditemukan dalam database"
                );

            return completedStatus;
        }

        #endregion
    }
}
