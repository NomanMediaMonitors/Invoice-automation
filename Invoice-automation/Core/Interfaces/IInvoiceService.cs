using InvoiceAutomation.Web.Core.DTOs;
using InvoiceAutomation.Web.Core.Entities;
using InvoiceAutomation.Web.Core.Enums;
using Microsoft.AspNetCore.Http;

namespace InvoiceAutomation.Web.Core.Interfaces;

/// <summary>
/// Interface for invoice business operations
/// </summary>
public interface IInvoiceService
{
    /// <summary>
    /// Create a new invoice from uploaded file
    /// </summary>
    Task<Invoice> CreateFromUploadAsync(Guid companyId, Guid userId, IFormFile file);

    /// <summary>
    /// Get an invoice by ID
    /// </summary>
    Task<Invoice?> GetByIdAsync(Guid id, bool includeRelated = true);

    /// <summary>
    /// Get invoices for a company with optional filters
    /// </summary>
    Task<PagedResult<Invoice>> GetInvoicesAsync(InvoiceFilterDto filter);

    /// <summary>
    /// Update invoice details
    /// </summary>
    Task<Invoice> UpdateAsync(Guid id, InvoiceUpdateDto dto);

    /// <summary>
    /// Update invoice line items
    /// </summary>
    Task UpdateLineItemsAsync(Guid invoiceId, List<InvoiceItemDto> items);

    /// <summary>
    /// Submit invoice for approval
    /// </summary>
    Task<Invoice> SubmitForApprovalAsync(Guid id, Guid userId);

    /// <summary>
    /// Delete an invoice (only drafts)
    /// </summary>
    Task DeleteAsync(Guid id);

    /// <summary>
    /// Get invoice statistics for dashboard
    /// </summary>
    Task<InvoiceStatisticsDto> GetStatisticsAsync(Guid companyId);

    /// <summary>
    /// Get invoices pending approval for a user
    /// </summary>
    Task<List<Invoice>> GetPendingApprovalAsync(Guid companyId, Guid userId, UserRole userRole);

    /// <summary>
    /// Check if an invoice can be edited
    /// </summary>
    bool CanEdit(Invoice invoice);

    /// <summary>
    /// Check if an invoice can be deleted
    /// </summary>
    bool CanDelete(Invoice invoice);
}
