using InvoiceAutomation.Web.Core.DTOs;
using InvoiceAutomation.Web.Core.Enums;

namespace InvoiceAutomation.Web.ViewModels;

/// <summary>
/// Main dashboard view model
/// </summary>
public class DashboardViewModel
{
    public Guid CompanyId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public UserRole UserRole { get; set; }

    /// <summary>
    /// Invoice statistics
    /// </summary>
    public InvoiceStatisticsDto InvoiceStatistics { get; set; } = new();

    /// <summary>
    /// Payment statistics
    /// </summary>
    public PaymentStatisticsDto PaymentStatistics { get; set; } = new();

    /// <summary>
    /// Recent invoices
    /// </summary>
    public List<InvoiceListItemViewModel> RecentInvoices { get; set; } = new();

    /// <summary>
    /// Invoices pending approval (for managers/admins)
    /// </summary>
    public List<InvoiceListItemViewModel> PendingApproval { get; set; } = new();

    /// <summary>
    /// Pending payments
    /// </summary>
    public List<PaymentListItemViewModel> PendingPayments { get; set; } = new();

    /// <summary>
    /// Counts for quick view
    /// </summary>
    public int DraftCount { get; set; }
    public int PendingApprovalCount { get; set; }
    public int ApprovedCount { get; set; }
    public int PendingPaymentCount { get; set; }

    /// <summary>
    /// Whether accounting is connected
    /// </summary>
    public bool AccountingConnected { get; set; }
    public AccountingProvider AccountingProvider { get; set; }
}

/// <summary>
/// Company selector for header
/// </summary>
public class CompanySelectorViewModel
{
    public Guid? CurrentCompanyId { get; set; }
    public string CurrentCompanyName { get; set; } = string.Empty;
    public List<CompanySelectorItem> Companies { get; set; } = new();
}

/// <summary>
/// Company selector item
/// </summary>
public class CompanySelectorItem
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsDefault { get; set; }
    public UserRole Role { get; set; }
}
