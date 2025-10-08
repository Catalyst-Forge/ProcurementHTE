using ProcurementHTE.Core.Models;

namespace ProcurementHTE.Core.Interfaces {
  public interface IVendorService {
    Task<IEnumerable<Vendor>> GetAllVendorsAsync();
    Task<Vendor?> GetVendorByIdAsync(string id);
    Task AddVendorAsync(Vendor vendor);
    Task EditVendorAsync(Vendor vendor, string id);
    Task DeleteVendorAsync(Vendor vendor, string id);
  }
}
