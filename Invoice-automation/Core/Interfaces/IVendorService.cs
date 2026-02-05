using InvoiceAutomation.Web.Core.DTOs;
using InvoiceAutomation.Web.Core.Entities;

namespace InvoiceAutomation.Web.Core.Interfaces;

/// <summary>
/// Interface for vendor/supplier operations
/// </summary>
public interface IVendorService
{
    /// <summary>
    /// Create a new vendor
    /// </summary>
    Task<Vendor> CreateAsync(Guid companyId, VendorCreateDto dto);

    /// <summary>
    /// Update vendor details
    /// </summary>
    Task<Vendor> UpdateAsync(Guid id, VendorUpdateDto dto);

    /// <summary>
    /// Get vendor by ID
    /// </summary>
    Task<Vendor?> GetByIdAsync(Guid id);

    /// <summary>
    /// Find vendor by NTN
    /// </summary>
    Task<Vendor?> FindByNtnAsync(Guid companyId, string ntn);

    /// <summary>
    /// Get all vendors for a company
    /// </summary>
    Task<List<Vendor>> GetByCompanyIdAsync(Guid companyId, bool includeInactive = false);

    /// <summary>
    /// Search vendors by name or NTN
    /// </summary>
    Task<List<Vendor>> SearchAsync(Guid companyId, string searchTerm);

    /// <summary>
    /// Delete (deactivate) a vendor
    /// </summary>
    Task DeleteAsync(Guid id);

    /// <summary>
    /// Set default expense account for a vendor
    /// </summary>
    Task SetDefaultExpenseAccountAsync(Guid vendorId, string expenseAccountId);
}
