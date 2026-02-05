using InvoiceAutomation.Web.Core.Enums;

namespace InvoiceAutomation.Web.ViewModels;

/// <summary>
/// Approval list view model
/// </summary>
public class ApprovalListViewModel
{
    public List<PendingApprovalItemViewModel> PendingApprovals { get; set; } = new();
    public int PendingCount { get; set; }
    public int ApprovedCount { get; set; }
    public int RejectedCount { get; set; }
}

/// <summary>
/// Pending approval item view model
/// </summary>
public class PendingApprovalItemViewModel
{
    public Guid Id { get; set; }
    public Guid InvoiceId { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public string VendorName { get; set; } = string.Empty;
    public DateTime InvoiceDate { get; set; }
    public DateTime? DueDate { get; set; }
    public decimal TotalAmount { get; set; }
    public string Currency { get; set; } = "PKR";
    public ApprovalLevel RequiredLevel { get; set; }
    public DateTime SubmittedAt { get; set; }
    public string SubmittedByName { get; set; } = string.Empty;
}
