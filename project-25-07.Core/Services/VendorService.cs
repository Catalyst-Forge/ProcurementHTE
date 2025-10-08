using project_25_07.Core.Interfaces;
using project_25_07.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace project_25_07.Core.Services {
  public class VendorService : IVendorService {
    private readonly IVendorRepository _vendorRepository;

    public VendorService(IVendorRepository vendorRepository) {
      _vendorRepository = vendorRepository;
    }

    public async Task<IEnumerable<Vendor>> GetAllVendorsAsync() {
      return await _vendorRepository.GetAllAsync();
    }

    public async Task<Vendor?> GetVendorByIdAsync(string id) {
      return await _vendorRepository.GetByIdAsync(id);
    }

    public async Task AddVendorAsync(Vendor vendor) {
      if (string.IsNullOrEmpty(vendor.VendorName)) {
          throw new ArgumentException("Vendor name cannot be empty");
      }

      vendor.CreatedAt = DateTime.Now;
      await _vendorRepository.CreateVendorAsync(vendor);
    }

    public async Task EditVendorAsync(Vendor vendor, string id) {
      //await _vendorRepository.UpdateVendorAsync(vendor);
    }

    public async Task DeleteVendorAsync(Vendor vendor, string id) {
      //
    }
  }
}
