using ProcurementHTE.Core.Common;
using ProcurementHTE.Core.Interfaces;
using ProcurementHTE.Core.Models;

namespace ProcurementHTE.Core.Services
{
    public class WoTypesService : IWoTypeService
    {

        private readonly IWoTypeRepository _woTypeRepository;

        public WoTypesService(IWoTypeRepository woTypeRepository)
        {
            _woTypeRepository = woTypeRepository;
        }

        public Task<PagedResult<WoTypes>> GetAllWoTypessAsync(int page, int pageSize, CancellationToken ct)
        {
            return _woTypeRepository.GetAllAsync(page, pageSize, ct);
        }

        public async Task AddWoTypesAsync(WoTypes woTypes)
        {
            if (string.IsNullOrEmpty(woTypes.TypeName))
            {
                throw new ArgumentException("Type Name cannot be empty");
            }

            await _woTypeRepository.CreateWoTypeAsync(woTypes);
        }

        public async Task EditWoTypesAsync(WoTypes woTypes, string woTypeId)
        {
            if (woTypes == null)
            {
                throw new ArgumentNullException(nameof(woTypes));
            }

            var existingWoTypes = await _woTypeRepository.GetByIdAsync(woTypeId);
            if (existingWoTypes == null)
            {
                throw new KeyNotFoundException($"Wo Type With ID {woTypeId}");
            }

            existingWoTypes.TypeName = woTypes.TypeName;
            existingWoTypes.Description = woTypes.Description;

            await _woTypeRepository.UpdateWoTypeAsync(existingWoTypes);
        }

        public async Task DeleteWoTypesAsync(WoTypes woTypes)
        {
            if (woTypes == null)
            {
                throw new ArgumentNullException(nameof(woTypes));
            }
            try
            {
                await _woTypeRepository.DropWoTypeAsync(woTypes);
            }
            catch(Exception e)
            {
                Console.WriteLine($"[DEBUG] {e}");
            }

        }

        public async Task<WoTypes?> GetWoTypesByIdAsync(string WoTypeId)
        {
            return await _woTypeRepository.GetByIdAsync(WoTypeId);
        }
    }
}
