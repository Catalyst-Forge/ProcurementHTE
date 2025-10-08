using ProcurementHTE.Core.Models;

namespace ProcurementHTE.Core.Interfaces {
  public interface IVendorRepository {
    Task<IEnumerable<Vendor>> GetAllAsync();
    Task<Vendor?> GetByIdAsync(string id);
    Task CreateVendorAsync(Vendor vendor);
    Task UpdateVendorAsync(Vendor vendor, string id);
    Task DropVendorAsync(Vendor vendor, string id);
  }
}
