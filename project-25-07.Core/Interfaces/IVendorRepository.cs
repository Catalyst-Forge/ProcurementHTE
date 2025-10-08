using project_25_07.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace project_25_07.Core.Interfaces {
  public interface IVendorRepository {
    Task<IEnumerable<Vendor>> GetAllAsync();
    Task<Vendor?> GetByIdAsync(string id);
    Task CreateVendorAsync(Vendor vendor);
    Task UpdateVendorAsync(Vendor vendor, string id);
    Task DropVendorAsync(Vendor vendor, string id);
  }
}
