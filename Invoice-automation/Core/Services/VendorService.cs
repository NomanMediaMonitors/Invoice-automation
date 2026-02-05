using InvoiceAutomation.Web.Core.DTOs;
using InvoiceAutomation.Web.Core.Entities;
using InvoiceAutomation.Web.Core.Interfaces;
using InvoiceAutomation.Web.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace InvoiceAutomation.Web.Core.Services;

/// <summary>
/// Vendor/supplier service
/// </summary>
public class VendorService : IVendorService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<VendorService> _logger;

    public VendorService(
        ApplicationDbContext context,
        ILogger<VendorService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Vendor> CreateAsync(Guid companyId, VendorCreateDto dto)
    {
        // Check for duplicate NTN
        if (!string.IsNullOrEmpty(dto.Ntn))
        {
            var existing = await _context.Vendors
                .FirstOrDefaultAsync(v => v.CompanyId == companyId && v.Ntn == dto.Ntn);

            if (existing != null)
            {
                throw new InvalidOperationException($"A vendor with NTN {dto.Ntn} already exists");
            }
        }

        var vendor = new Vendor
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            Name = dto.Name,
            Ntn = dto.Ntn,
            Strn = dto.Strn,
            Email = dto.Email,
            Phone = dto.Phone,
            Address = dto.Address,
            City = dto.City,
            ContactPerson = dto.ContactPerson,
            DefaultExpenseAccountId = dto.DefaultExpenseAccountId,
            PaymentTermsDays = dto.PaymentTermsDays,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.Vendors.Add(vendor);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Vendor created: {VendorId} - {VendorName}", vendor.Id, vendor.Name);
        return vendor;
    }

    public async Task<Vendor> UpdateAsync(Guid id, VendorUpdateDto dto)
    {
        var vendor = await _context.Vendors.FindAsync(id)
            ?? throw new KeyNotFoundException($"Vendor not found: {id}");

        if (dto.Name != null) vendor.Name = dto.Name;
        if (dto.Ntn != null) vendor.Ntn = dto.Ntn;
        if (dto.Strn != null) vendor.Strn = dto.Strn;
        if (dto.Email != null) vendor.Email = dto.Email;
        if (dto.Phone != null) vendor.Phone = dto.Phone;
        if (dto.Address != null) vendor.Address = dto.Address;
        if (dto.City != null) vendor.City = dto.City;
        if (dto.ContactPerson != null) vendor.ContactPerson = dto.ContactPerson;
        if (dto.DefaultExpenseAccountId != null) vendor.DefaultExpenseAccountId = dto.DefaultExpenseAccountId;
        if (dto.PaymentTermsDays.HasValue) vendor.PaymentTermsDays = dto.PaymentTermsDays.Value;
        if (dto.IsActive.HasValue) vendor.IsActive = dto.IsActive.Value;

        vendor.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return vendor;
    }

    public async Task<Vendor?> GetByIdAsync(Guid id)
    {
        return await _context.Vendors
            .Include(v => v.Company)
            .FirstOrDefaultAsync(v => v.Id == id);
    }

    public async Task<Vendor?> FindByNtnAsync(Guid companyId, string ntn)
    {
        if (string.IsNullOrEmpty(ntn))
            return null;

        // Normalize NTN (remove dashes, spaces)
        var normalizedNtn = ntn.Replace("-", "").Replace(" ", "").Trim();

        return await _context.Vendors
            .FirstOrDefaultAsync(v =>
                v.CompanyId == companyId &&
                v.Ntn != null &&
                v.Ntn.Replace("-", "").Replace(" ", "") == normalizedNtn &&
                v.IsActive);
    }

    public async Task<List<Vendor>> GetByCompanyIdAsync(Guid companyId, bool includeInactive = false)
    {
        var query = _context.Vendors.Where(v => v.CompanyId == companyId);

        if (!includeInactive)
        {
            query = query.Where(v => v.IsActive);
        }

        return await query
            .OrderBy(v => v.Name)
            .ToListAsync();
    }

    public async Task<List<Vendor>> SearchAsync(Guid companyId, string searchTerm)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
        {
            return await GetByCompanyIdAsync(companyId);
        }

        var term = searchTerm.ToLower();

        return await _context.Vendors
            .Where(v => v.CompanyId == companyId && v.IsActive &&
                       (v.Name.ToLower().Contains(term) ||
                        (v.Ntn != null && v.Ntn.Contains(term)) ||
                        (v.Email != null && v.Email.ToLower().Contains(term))))
            .OrderBy(v => v.Name)
            .Take(20)
            .ToListAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        var vendor = await _context.Vendors.FindAsync(id)
            ?? throw new KeyNotFoundException($"Vendor not found: {id}");

        // Soft delete - just deactivate
        vendor.IsActive = false;
        vendor.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        _logger.LogInformation("Vendor deactivated: {VendorId}", id);
    }

    public async Task SetDefaultExpenseAccountAsync(Guid vendorId, string expenseAccountId)
    {
        var vendor = await _context.Vendors.FindAsync(vendorId)
            ?? throw new KeyNotFoundException($"Vendor not found: {vendorId}");

        vendor.DefaultExpenseAccountId = expenseAccountId;
        vendor.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
    }
}
