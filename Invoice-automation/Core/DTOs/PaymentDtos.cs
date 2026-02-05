using InvoiceAutomation.Web.Core.Enums;

namespace InvoiceAutomation.Web.Core.DTOs;

/// <summary>
/// Schedule payment data
/// </summary>
public class PaymentScheduleDto
{
    public string PaymentAccountId { get; set; } = string.Empty;
    public string? PaymentAccountName { get; set; }
    public decimal Amount { get; set; }
    public string? PaymentMethod { get; set; }
    public string? ReferenceNumber { get; set; }
    public DateTime? ScheduledDate { get; set; }
}

/// <summary>
/// Payment statistics
/// </summary>
public class PaymentStatisticsDto
{
    public int TotalPayments { get; set; }
    public int PendingPayments { get; set; }
    public int CompletedPayments { get; set; }
    public int FailedPayments { get; set; }
    public decimal TotalPaid { get; set; }
    public decimal TotalPending { get; set; }
    public Dictionary<string, decimal> PaymentsByMonth { get; set; } = new();
}

/// <summary>
/// Journal entry data for accounting system
/// </summary>
public class JournalEntryDto
{
    public DateTime Date { get; set; } = DateTime.Today;
    public string? Memo { get; set; }
    public string? ReferenceNumber { get; set; }
    public List<JournalLineDto> Lines { get; set; } = new();
}

/// <summary>
/// Journal entry line
/// </summary>
public class JournalLineDto
{
    public string AccountId { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;
    public string AccountCode { get; set; } = string.Empty;
    public AccountType AccountType { get; set; }
    public decimal DebitAmount { get; set; }
    public decimal CreditAmount { get; set; }
    public string? Description { get; set; }
}

/// <summary>
/// Result of journal entry creation
/// </summary>
public class JournalEntryResultDto
{
    public bool Success { get; set; }
    public string? ExternalId { get; set; }
    public string? ReferenceNumber { get; set; }
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Bill data for accounting system
/// </summary>
public class BillDto
{
    public string VendorId { get; set; } = string.Empty;
    public string BillNumber { get; set; } = string.Empty;
    public DateTime BillDate { get; set; }
    public DateTime? DueDate { get; set; }
    public decimal TotalAmount { get; set; }
    public List<BillLineDto> Lines { get; set; } = new();
}

/// <summary>
/// Bill line item
/// </summary>
public class BillLineDto
{
    public string AccountId { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
}

/// <summary>
/// Result of bill creation
/// </summary>
public class BillResultDto
{
    public bool Success { get; set; }
    public string? ExternalId { get; set; }
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Payment record for accounting system
/// </summary>
public class PaymentRecordDto
{
    public string BillId { get; set; } = string.Empty;
    public string BankAccountId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateTime PaymentDate { get; set; }
    public string? ReferenceNumber { get; set; }
    public string? Memo { get; set; }
}

/// <summary>
/// Result of payment recording
/// </summary>
public class PaymentResultDto
{
    public bool Success { get; set; }
    public string? ExternalId { get; set; }
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Token refresh result
/// </summary>
public class TokenRefreshResult
{
    public string AccessToken { get; set; } = string.Empty;
    public string? RefreshToken { get; set; }
    public DateTime ExpiresAt { get; set; }
}
