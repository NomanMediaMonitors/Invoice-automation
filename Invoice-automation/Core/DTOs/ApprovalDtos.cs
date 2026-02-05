using InvoiceAutomation.Web.Core.Enums;

namespace InvoiceAutomation.Web.Core.DTOs;

/// <summary>
/// Approval rules configuration
/// </summary>
public class ApprovalRulesDto
{
    /// <summary>
    /// Amount threshold for manager-only approval
    /// </summary>
    public decimal ManagerOnlyThreshold { get; set; } = 50000;

    /// <summary>
    /// Amount threshold requiring admin approval
    /// </summary>
    public decimal AdminRequiredThreshold { get; set; } = 500000;

    /// <summary>
    /// Amount threshold requiring CFO approval
    /// </summary>
    public decimal CfoRequiredThreshold { get; set; } = 1000000;

    /// <summary>
    /// Require admin approval for new vendors
    /// </summary>
    public bool RequireAdminForNewVendor { get; set; } = true;

    /// <summary>
    /// Auto-approve invoices from trusted vendors
    /// </summary>
    public bool AutoApproveFromTrustedVendors { get; set; } = false;
}

/// <summary>
/// Approval action data
/// </summary>
public class ApprovalActionDto
{
    public Guid InvoiceId { get; set; }
    public bool Approved { get; set; }
    public string? Comments { get; set; }
}

/// <summary>
/// Approval history item for display
/// </summary>
public class ApprovalHistoryDto
{
    public Guid Id { get; set; }
    public ApprovalLevel Level { get; set; }
    public ApprovalStatus Status { get; set; }
    public string? ApproverName { get; set; }
    public string? ApproverEmail { get; set; }
    public string? Comments { get; set; }
    public DateTime? DecidedAt { get; set; }
    public DateTime CreatedAt { get; set; }
}
