using InvoiceAutomation.Web.Core.Enums;
using CoreMatchType = InvoiceAutomation.Web.Core.Enums.MatchType;

namespace InvoiceAutomation.Web.Core.Entities;

/// <summary>
/// Individual line item on an invoice
/// </summary>
public class InvoiceItem
{
    public Guid Id { get; set; }

    public Guid InvoiceId { get; set; }

    /// <summary>
    /// External expense account ID from accounting system
    /// This is NOT a foreign key - it's a reference to the external COA
    /// </summary>
    public string? ExpenseAccountId { get; set; }

    /// <summary>
    /// Description of the item/service
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Quantity (default 1)
    /// </summary>
    public decimal Quantity { get; set; } = 1;

    /// <summary>
    /// Unit of measure
    /// </summary>
    public string? Unit { get; set; }

    /// <summary>
    /// Price per unit
    /// </summary>
    public decimal UnitPrice { get; set; }

    /// <summary>
    /// Tax amount for this line item
    /// </summary>
    public decimal TaxAmount { get; set; }

    /// <summary>
    /// Total amount for this line item
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// Line number for ordering
    /// </summary>
    public int LineNumber { get; set; }

    /// <summary>
    /// How the expense account was matched
    /// </summary>
    public CoreMatchType MatchType { get; set; } = CoreMatchType.Manual;

    /// <summary>
    /// AI confidence score for auto-matched accounts
    /// </summary>
    public decimal? MatchConfidence { get; set; }

    // Navigation property
    public virtual Invoice Invoice { get; set; } = null!;
}
