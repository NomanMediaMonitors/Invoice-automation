using InvoiceAutomation.Web.Core.Enums;

namespace InvoiceAutomation.Web.Core.Entities;

/// <summary>
/// Approval record for an invoice
/// </summary>
public class InvoiceApproval
{
    public Guid Id { get; set; }

    public Guid InvoiceId { get; set; }

    /// <summary>
    /// User who made the decision (null if pending)
    /// </summary>
    public Guid? ApproverId { get; set; }

    /// <summary>
    /// Level of approval (Manager, Admin, CFO)
    /// </summary>
    public ApprovalLevel ApprovalLevel { get; set; }

    /// <summary>
    /// Current status of this approval
    /// </summary>
    public ApprovalStatus Status { get; set; } = ApprovalStatus.Pending;

    /// <summary>
    /// Comments from the approver
    /// </summary>
    public string? Comments { get; set; }

    /// <summary>
    /// When the decision was made
    /// </summary>
    public DateTime? DecidedAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual Invoice Invoice { get; set; } = null!;
    public virtual User? Approver { get; set; }
}
