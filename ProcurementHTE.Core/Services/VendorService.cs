using Microsoft.EntityFrameworkCore;
using ProcurementHTE.Core.Common;
using ProcurementHTE.Core.Interfaces;
using ProcurementHTE.Core.Models;
using ProcurementHTE.Core.Utils;

public class VendorService : IVendorService
{
    private readonly IVendorRepository _vendorRepository;
    private const string VendorPrefix = "VND";

    public VendorService(IVendorRepository vendorRepository) =>
        _vendorRepository = vendorRepository;

    public async Task<IEnumerable<Vendor>> GetAllVendorsAsync()
    {
        return await _vendorRepository.GetAllAsync();
    }

    public async Task<Vendor?> GetVendorByIdAsync(string id)
    {
        if (string.IsNullOrEmpty(id))
            throw new ArgumentException("ID cannot be null or empty");

        return await _vendorRepository.GetByIdAsync(id);
    }

    public Task<PagedResult<Vendor>> GetPagedAsync(
        int page,
        int pageSize,
        string? search,
        ISet<string> fields,
        CancellationToken ct
    )
    {
        return _vendorRepository.GetPagedAsync(page, pageSize, search, fields, ct);
    }

    public async Task AddVendorAsync(Vendor vendor)
    {
        if (vendor == null)
            throw new ArgumentNullException(nameof(vendor));

        if (string.IsNullOrWhiteSpace(vendor.VendorName))
            throw new ArgumentException("Vendor name cannot be empty", nameof(vendor.VendorName));

        if (vendor.VendorName.Length > 200)
            throw new ArgumentException(
                "Vendor name cannot exceed 200 characters",
                nameof(vendor.VendorName)
            );

        var lastCode = await _vendorRepository.GetLastCodeAsync(VendorPrefix);
        vendor.VendorCode = SequenceNumberGenerator.NumId(VendorPrefix, lastCode);
        vendor.CreatedAt = DateTime.Now;

        try
        {
            await _vendorRepository.StoreVendorAsync(vendor);
        }
        catch (DbUpdateException ex)
        {
            if (!IsUniqueConstraint(ex))
                throw;

            lastCode = await _vendorRepository.GetLastCodeAsync(VendorPrefix);
            vendor.VendorCode = SequenceNumberGenerator.NumId(VendorPrefix, lastCode);
            await _vendorRepository.StoreVendorAsync(vendor);
        }
    }

    public async Task EditVendorAsync(Vendor vendor, string id)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("ID cannot be null or empty", nameof(id));

        if (vendor == null)
            throw new ArgumentNullException(nameof(vendor));

        if (string.IsNullOrWhiteSpace(vendor.VendorName))
            throw new ArgumentException("Vendor name cannot be empty", nameof(vendor.VendorName));

        if (vendor.VendorName.Length > 200)
            throw new ArgumentException(
                "Vendor name cannot exceed 200 characters",
                nameof(vendor.VendorName)
            );

        var existingVendor = await _vendorRepository.GetByIdAsync(id);
        if (existingVendor != null)
        {
            existingVendor.VendorName = vendor.VendorName;
            existingVendor.NPWP = vendor.NPWP;
            existingVendor.Address = vendor.Address;
            existingVendor.City = vendor.City;
            existingVendor.Province = vendor.Province;
            existingVendor.PostalCode = vendor.PostalCode;
            existingVendor.Email = vendor.Email;
            existingVendor.Comment = vendor.Comment;

            try
            {
                await _vendorRepository.UpdateVendorAsync(existingVendor);
            }
            catch (DbUpdateConcurrencyException)
            {
                throw new KeyNotFoundException($"Vendor with ID {id} not found");
            }
        }
    }

    public async Task DeleteVendorAsync(Vendor vendor)
    {
        if (vendor == null)
            throw new ArgumentException("Vendor or ID cannot be null");

        try
        {
            await _vendorRepository.DropVendorAsync(vendor);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new KeyNotFoundException(
                "Vendor not found during delete (possibly already removed)."
            );
        }
    }

    private static bool IsUniqueConstraint(DbUpdateException ex)
    {
        var msg = ex.InnerException?.Message ?? ex.Message;
        return msg.Contains("UNIQUE", StringComparison.OrdinalIgnoreCase)
            || msg.Contains("duplicate", StringComparison.OrdinalIgnoreCase);
    }
}
