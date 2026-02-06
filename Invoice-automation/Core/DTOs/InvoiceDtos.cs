using InvoiceAutomation.Web.Core.Enums;
using CoreMatchType = InvoiceAutomation.Web.Core.Enums.MatchType;

namespace InvoiceAutomation.Web.Core.DTOs;

/// <summary>
/// Filter for invoice queries
/// </summary>
public class InvoiceFilterDto
{
    public Guid CompanyId { get; set; }
    public Guid? VendorId { get; set; }
    public InvoiceStatus? Status { get; set; }
    public DateTime? DateFrom { get; set; }
    public DateTime? DateTo { get; set; }
    public decimal? AmountMin { get; set; }
    public decimal? AmountMax { get; set; }
    public string? SearchTerm { get; set; }
    public string SortBy { get; set; } = "CreatedAt";
    public bool SortDescending { get; set; } = true;
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;

    // Aliases for view compatibility
    public DateTime? FromDate { get => DateFrom; set => DateFrom = value; }
    public DateTime? ToDate { get => DateTo; set => DateTo = value; }
}

/// <summary>
/// Invoice update data
/// </summary>
public class InvoiceUpdateDto
{
    public Guid? VendorId { get; set; }
    public string? InvoiceNumber { get; set; }
    public DateTime? InvoiceDate { get; set; }
    public DateTime? DueDate { get; set; }
    public decimal? Subtotal { get; set; }
    public decimal? TaxAmount { get; set; }
    public decimal? TotalAmount { get; set; }
    public string? Notes { get; set; }
    public List<InvoiceItemDto>? Items { get; set; }
}

/// <summary>
/// Invoice line item data
/// </summary>
public class InvoiceItemDto
{
    public Guid? Id { get; set; }
    public string? ExpenseAccountId { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Quantity { get; set; } = 1;
    public string? Unit { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal Amount { get; set; }
    public int LineNumber { get; set; }
    public CoreMatchType MatchType { get; set; } = CoreMatchType.Manual;
}

/// <summary>
/// Invoice statistics for dashboard
/// </summary>
public class InvoiceStatisticsDto
{
    public int TotalCount { get; set; }
    public int DraftCount { get; set; }
    public int PendingApprovalCount { get; set; }
    public int ApprovedCount { get; set; }
    public int CompletedCount { get; set; }
    public int RejectedCount { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal PendingAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public Dictionary<string, decimal> AmountByMonth { get; set; } = new();
}

/// <summary>
/// Paged result wrapper
/// </summary>
public class PagedResult<T>
{
    public List<T> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
    public bool HasPrevious => Page > 1;
    public bool HasNext => Page < TotalPages;
}
