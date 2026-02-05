using InvoiceAutomation.Web.Core.Enums;

namespace InvoiceAutomation.Web.Core.Entities;

/// <summary>
/// Payment record for an invoice
/// </summary>
public class Payment
{
    public Guid Id { get; set; }

    public Guid InvoiceId { get; set; }

    /// <summary>
    /// External bank/cash account ID from accounting system
    /// This is NOT a foreign key - it's a reference to the external COA
    /// </summary>
    public string PaymentAccountId { get; set; } = string.Empty;

    /// <summary>
    /// Name of the payment account (denormalized for display)
    /// </summary>
    public string? PaymentAccountName { get; set; }

    /// <summary>
    /// User who executed the payment
    /// </summary>
    public Guid? ExecutedById { get; set; }

    /// <summary>
    /// Payment amount
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// Payment method description
    /// </summary>
    public string? PaymentMethod { get; set; }

    /// <summary>
    /// Reference number (check number, transfer ID, etc.)
    /// </summary>
    public string? ReferenceNumber { get; set; }

    /// <summary>
    /// Current payment status
    /// </summary>
    public PaymentStatus Status { get; set; } = PaymentStatus.Scheduled;

    /// <summary>
    /// Scheduled payment date
    /// </summary>
    public DateTime? ScheduledDate { get; set; }

    /// <summary>
    /// Actual execution timestamp
    /// </summary>
    public DateTime? ExecutedAt { get; set; }

    /// <summary>
    /// External reference after posting to accounting system
    /// </summary>
    public string? ExternalRef { get; set; }

    /// <summary>
    /// Journal entry ID in accounting system
    /// </summary>
    public string? JournalEntryRef { get; set; }

    /// <summary>
    /// Failure reason if payment failed
    /// </summary>
    public string? FailureReason { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    public virtual Invoice Invoice { get; set; } = null!;
    public virtual User? ExecutedBy { get; set; }
}
