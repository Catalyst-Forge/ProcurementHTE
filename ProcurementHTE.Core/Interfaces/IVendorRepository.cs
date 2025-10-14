using ProcurementHTE.Core.Models;

namespace ProcurementHTE.Core.Interfaces
{
    public interface IVendorRepository
    {
        Task<List<Vendor>> GetAllAsync();
        Task<Vendor?> GetByIdAsync(string id);
        Task<int> CountAsync();
        Task<string?> GetLastCodeAsync(string prefix);
        Task StoreVendorAsync(Vendor vendor);
        Task UpdateVendorAsync(Vendor vendor);
        Task DropVendorAsync(Vendor vendor);
    }

}
