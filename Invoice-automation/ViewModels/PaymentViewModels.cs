using System.ComponentModel.DataAnnotations;
using InvoiceAutomation.Web.Core.DTOs;
using InvoiceAutomation.Web.Core.Enums;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace InvoiceAutomation.Web.ViewModels;

/// <summary>
/// Payment list view model
/// </summary>
public class PaymentListViewModel
{
    public Guid CompanyId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public List<PaymentListItemViewModel> Payments { get; set; } = new();
    public PaymentStatisticsDto Statistics { get; set; } = new();
    public PaymentStatus? FilterStatus { get; set; }
    public List<SelectListItem> StatusOptions { get; set; } = new();
}

/// <summary>
/// Payment list item
/// </summary>
public class PaymentListItemViewModel
{
    public Guid Id { get; set; }
    public Guid InvoiceId { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public string VendorName { get; set; } = string.Empty;
    public string PaymentAccountName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "PKR";
    public PaymentStatus Status { get; set; }
    public DateTime? ScheduledDate { get; set; }
    public DateTime? ExecutedAt { get; set; }
    public string? ExecutedByName { get; set; }
    public string? ReferenceNumber { get; set; }
}

/// <summary>
/// Payment execute view model
/// </summary>
public class PaymentExecuteViewModel
{
    public Guid InvoiceId { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public string VendorName { get; set; } = string.Empty;
    public DateTime InvoiceDate { get; set; }
    public DateTime? DueDate { get; set; }
    public decimal InvoiceAmount { get; set; }
    public string Currency { get; set; } = "PKR";

    /// <summary>
    /// Invoice file URL for preview
    /// </summary>
    public string InvoiceFileUrl { get; set; } = string.Empty;
    public bool IsPdf { get; set; }

    /// <summary>
    /// Line items summary
    /// </summary>
    public List<InvoiceItemReviewModel> LineItems { get; set; } = new();

    /// <summary>
    /// Payment form
    /// </summary>
    public PaymentFormModel Payment { get; set; } = new();

    /// <summary>
    /// Available bank/cash accounts from accounting system
    /// </summary>
    public List<SelectListItem> PaymentAccounts { get; set; } = new();

    /// <summary>
    /// Preview of journal entry that will be created
    /// </summary>
    public JournalEntryDto? JournalPreview { get; set; }
}

/// <summary>
/// Payment form model
/// </summary>
public class PaymentFormModel
{
    [Required(ErrorMessage = "Payment account is required")]
    [Display(Name = "Payment Account")]
    public string PaymentAccountId { get; set; } = string.Empty;

    [Display(Name = "Payment Account Name")]
    public string? PaymentAccountName { get; set; }

    [Required(ErrorMessage = "Amount is required")]
    [Display(Name = "Amount")]
    [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than 0")]
    public decimal Amount { get; set; }

    [Display(Name = "Payment Method")]
    [StringLength(100)]
    public string? PaymentMethod { get; set; }

    [Display(Name = "Reference/Check Number")]
    [StringLength(100)]
    public string? ReferenceNumber { get; set; }

    [Display(Name = "Scheduled Date")]
    [DataType(DataType.Date)]
    public DateTime? ScheduledDate { get; set; }
}

/// <summary>
/// Payment details view model
/// </summary>
public class PaymentDetailsViewModel
{
    public Guid Id { get; set; }
    public Guid InvoiceId { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public string VendorName { get; set; } = string.Empty;
    public string PaymentAccountId { get; set; } = string.Empty;
    public string PaymentAccountName { get; set; } = string.Empty;

    /// <summary>
    /// Alias for PaymentAccountName for view compatibility
    /// </summary>
    public string BankAccountName => PaymentAccountName;

    public decimal Amount { get; set; }
    public string Currency { get; set; } = "PKR";
    public string? PaymentMethod { get; set; }
    public string? ReferenceNumber { get; set; }

    /// <summary>
    /// Alias for ReferenceNumber for view compatibility
    /// </summary>
    public string? TransactionReference => ReferenceNumber;

    public PaymentStatus Status { get; set; }
    public DateTime? ScheduledDate { get; set; }
    public DateTime? ExecutedAt { get; set; }

    /// <summary>
    /// Alias for ExecutedAt for view compatibility
    /// </summary>
    public DateTime? PaymentDate => ExecutedAt;

    public string? ExecutedByName { get; set; }
    public string? ExternalRef { get; set; }
    public string? JournalEntryRef { get; set; }

    /// <summary>
    /// Alias for JournalEntryRef for view compatibility
    /// </summary>
    public string? JournalEntryId => JournalEntryRef;

    public string? FailureReason { get; set; }
    public DateTime? CreatedAt { get; set; }
    public string? CreatedByName { get; set; }
    public string? Notes { get; set; }

    /// <summary>
    /// Journal entry details
    /// </summary>
    public JournalEntryDto? JournalEntry { get; set; }

    /// <summary>
    /// Journal entries list (from JournalEntry.Lines)
    /// </summary>
    public List<JournalLineDto>? JournalEntries => JournalEntry?.Lines;
}

/// <summary>
/// Payment schedule view model (alias for PaymentExecuteViewModel for Schedule view)
/// </summary>
public class PaymentScheduleViewModel : PaymentExecuteViewModel { }
