using InvoiceAutomation.Web.Core.DTOs;
using InvoiceAutomation.Web.Core.Entities;
using InvoiceAutomation.Web.Core.Enums;

namespace InvoiceAutomation.Web.Core.Interfaces;

/// <summary>
/// Interface for approval workflow operations
/// </summary>
public interface IApprovalService
{
    /// <summary>
    /// Approve an invoice at the current level
    /// </summary>
    Task<Invoice> ApproveAsync(Guid invoiceId, Guid approverId, string? comments = null);

    /// <summary>
    /// Reject an invoice
    /// </summary>
    Task<Invoice> RejectAsync(Guid invoiceId, Guid approverId, string comments);

    /// <summary>
    /// Get the required approval level for an invoice based on amount and rules
    /// </summary>
    ApprovalLevel GetRequiredApprovalLevel(Invoice invoice);

    /// <summary>
    /// Check if a user can approve an invoice
    /// </summary>
    Task<bool> CanApproveAsync(Guid invoiceId, Guid userId);

    /// <summary>
    /// Get approval history for an invoice
    /// </summary>
    Task<List<InvoiceApproval>> GetApprovalHistoryAsync(Guid invoiceId);

    /// <summary>
    /// Get pending approvals count for a user
    /// </summary>
    Task<int> GetPendingApprovalCountAsync(Guid companyId, Guid userId, UserRole userRole);

    /// <summary>
    /// Get approval rules for a company
    /// </summary>
    Task<ApprovalRulesDto> GetApprovalRulesAsync(Guid companyId);
}
