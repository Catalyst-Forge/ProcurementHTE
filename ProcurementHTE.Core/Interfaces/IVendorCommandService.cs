using ProcurementHTE.Core.Models;

namespace ProcurementHTE.Core.Interfaces;

public interface IVendorCommandService
{
    Task AddVendorAsync(Vendor vendor);
    Task EditVendorAsync(Vendor vendor, string id);
    Task DeleteVendorAsync(Vendor vendor, string deletedByUserId);
}
