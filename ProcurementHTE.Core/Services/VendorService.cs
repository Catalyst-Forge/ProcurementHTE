using Microsoft.EntityFrameworkCore;
using ProcurementHTE.Core.Common;
using ProcurementHTE.Core.Interfaces;
using ProcurementHTE.Core.Models;
using ProcurementHTE.Core.Utils;

public class VendorService : IVendorService
{
    private readonly IVendorRepository _vendorRepository;
    private const string VendorPrefix = "VND";

    public VendorService(IVendorRepository vendorRepository)
    {
        _vendorRepository = vendorRepository;
    }

    public async Task<IEnumerable<Vendor>> GetAllVendorsAsync() =>
        await _vendorRepository.GetAllAsync();

    public async Task<Vendor?> GetVendorByIdAsync(string id)
    {
        if (string.IsNullOrEmpty(id)) throw new ArgumentException("ID cannot be null or empty");
        return await _vendorRepository.GetByIdAsync(id);
    }
    public Task<PagedResult<Vendor>> GetPagedAsync(int page, int pageSize, CancellationToken ct)
        => _vendorRepository.GetPagedAsync(page, pageSize, ct);

    public async Task AddVendorAsync(Vendor vendor)
    {
        if (string.IsNullOrWhiteSpace(vendor.VendorName))
            throw new ArgumentException("Vendor name cannot be empty");

        // Generate code dari last code di DB
        var lastCode = await _vendorRepository.GetLastCodeAsync(VendorPrefix);
        vendor.VendorCode = SequenceNumberGenerator.NumId(VendorPrefix, lastCode);

        // CreatedAt sudah diset initializer; boleh ditegaskan lagi jika perlu
        vendor.CreatedAt = DateTime.Now;

        // Simpan
        try
        {
            await _vendorRepository.StoreVendorAsync(vendor);
        }
        catch (DbUpdateException ex)
        {
            // fallback 1x retry kalau kena unique constraint (jika kamu tambahkan unique index)
            if (!IsUniqueConstraint(ex)) throw;
            lastCode = await _vendorRepository.GetLastCodeAsync(VendorPrefix);
            vendor.VendorCode = SequenceNumberGenerator.NumId(VendorPrefix, lastCode);
            await _vendorRepository.StoreVendorAsync(vendor);
        }
    }

    public async Task EditVendorAsync(Vendor vendor, string id)
    {
        if (vendor == null || string.IsNullOrEmpty(id))
            throw new ArgumentException("Vendor or ID cannot be null");

        var existingVendor = await _vendorRepository.GetByIdAsync(id)
                             ?? throw new KeyNotFoundException($"Vendor with ID {id} not found");

        // Biasanya VendorCode TIDAK diubah. Kalau memang mau, silakan pertahankan baris bawah ini.
        // existingVendor.VendorCode = vendor.VendorCode;

        existingVendor.VendorName = vendor.VendorName;
        existingVendor.NPWP = vendor.NPWP;
        existingVendor.Address = vendor.Address;
        existingVendor.City = vendor.City;
        existingVendor.Province = vendor.Province;
        existingVendor.PostalCode = vendor.PostalCode;
        existingVendor.Email = vendor.Email;
        existingVendor.PhoneNumber = vendor.PhoneNumber;
        existingVendor.ContactPerson = vendor.ContactPerson;
        existingVendor.ContactPosition = vendor.ContactPosition;
        existingVendor.Status = vendor.Status;
        existingVendor.Comment = vendor.Comment;

        await _vendorRepository.UpdateVendorAsync(existingVendor);
    }

    public async Task DeleteVendorAsync(Vendor vendor)
    {
        if (vendor == null) throw new ArgumentException("Vendor or ID cannot be null");
        await _vendorRepository.DropVendorAsync(vendor);
    }

    private static bool IsUniqueConstraint(DbUpdateException ex)
    {
        var msg = ex.InnerException?.Message ?? ex.Message;
        return msg.Contains("UNIQUE", StringComparison.OrdinalIgnoreCase)
            || msg.Contains("duplicate", StringComparison.OrdinalIgnoreCase);
    }
}
