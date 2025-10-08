using project_25_07.Core.Models;

namespace project_25_07.Core.Interfaces {
  public interface IVendorService {
    Task<IEnumerable<Vendor>> GetAllVendorsAsync();
    Task<Vendor?> GetVendorByIdAsync(string id);
    Task AddVendorAsync(Vendor vendor);
    Task EditVendorAsync(Vendor vendor, string id);
    Task DeleteVendorAsync(Vendor vendor, string id);
  }
}
