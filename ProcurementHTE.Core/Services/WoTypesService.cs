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

        public async Task<IEnumerable<WoTypes>> GetAllWoTypessAsync()
        {
            return await _woTypeRepository.GetAllAsync();
        }

        public async Task AddWoTypesAsync(WoTypes woTypes)
        {
            if (string.IsNullOrEmpty(woTypes.TypeName))
            {
                throw new ArgumentException("Type Name cannot be empty");
            }

            await _woTypeRepository.CreateWoTypeAsync(woTypes);
        }

        public async Task EditWoTypesAsync(WoTypes woTypes, int woTypeId)
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

        public async Task<WoTypes?> GetWoTypesByIdAsync(int WoTypeId)
        {
            return await _woTypeRepository.GetByIdAsync(WoTypeId);
        }
    }
}
