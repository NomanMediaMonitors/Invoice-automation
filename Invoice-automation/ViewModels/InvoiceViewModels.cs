using System.ComponentModel.DataAnnotations;
using InvoiceAutomation.Web.Core.DTOs;
using InvoiceAutomation.Web.Core.Entities;
using InvoiceAutomation.Web.Core.Enums;
using Microsoft.AspNetCore.Mvc.Rendering;
using CoreMatchType = InvoiceAutomation.Web.Core.Enums.MatchType;

namespace InvoiceAutomation.Web.ViewModels;

/// <summary>
/// Invoice list view model
/// </summary>
public class InvoiceListViewModel
{
    public PagedResult<InvoiceListItemViewModel> Invoices { get; set; } = new();
    public InvoiceFilterDto Filter { get; set; } = new();
    public InvoiceStatisticsDto Statistics { get; set; } = new();
    public List<SelectListItem> Vendors { get; set; } = new();
    public List<SelectListItem> StatusOptions { get; set; } = new();
    public Guid CompanyId { get; set; }
    public string CompanyName { get; set; } = string.Empty;

    // Pagination aliases for view compatibility
    public int CurrentPage => Invoices.Page;
    public int TotalPages => Invoices.TotalPages;
}

/// <summary>
/// Invoice list item
/// </summary>
public class InvoiceListItemViewModel
{
    public Guid Id { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public DateTime InvoiceDate { get; set; }
    public DateTime? DueDate { get; set; }
    public string VendorName { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public string Currency { get; set; } = "PKR";
    public InvoiceStatus Status { get; set; }
    public decimal? OcrConfidence { get; set; }
    public DateTime CreatedAt { get; set; }
    public string UploadedByName { get; set; } = string.Empty;
    public CoreMatchType MatchType { get; set; }
}

/// <summary>
/// Invoice upload view model
/// </summary>
public class InvoiceUploadViewModel
{
    public Guid CompanyId { get; set; }

    [Required(ErrorMessage = "Please select an invoice file")]
    [Display(Name = "Invoice File")]
    public IFormFile File { get; set; } = null!;

    [Display(Name = "Vendor")]
    public Guid? VendorId { get; set; }

    public List<SelectListItem> Companies { get; set; } = new();

    public List<SelectListItem> Vendors { get; set; } = new();
}

/// <summary>
/// Invoice edit view model with image preview
/// </summary>
public class InvoiceEditViewModel
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public string CompanyName { get; set; } = string.Empty;

    /// <summary>
    /// URL to the invoice file for preview
    /// </summary>
    public string InvoiceFileUrl { get; set; } = string.Empty;

    /// <summary>
    /// Whether the file is a PDF
    /// </summary>
    public bool IsPdf { get; set; }

    /// <summary>
    /// Original filename
    /// </summary>
    public string OriginalFileName { get; set; } = string.Empty;

    /// <summary>
    /// OCR confidence percentage
    /// </summary>
    public decimal? OcrConfidence { get; set; }

    /// <summary>
    /// Invoice data for editing
    /// </summary>
    public InvoiceFormModel Invoice { get; set; } = new();

    /// <summary>
    /// Current status
    /// </summary>
    public InvoiceStatus Status { get; set; }

    /// <summary>
    /// Can the invoice be edited
    /// </summary>
    public bool CanEdit { get; set; }

    /// <summary>
    /// Can the invoice be submitted for approval
    /// </summary>
    public bool CanSubmit { get; set; }

    /// <summary>
    /// Can the invoice be deleted
    /// </summary>
    public bool CanDelete { get; set; }

    /// <summary>
    /// Available vendors for dropdown
    /// </summary>
    public List<SelectListItem> Vendors { get; set; } = new();

    /// <summary>
    /// Expense accounts from accounting system
    /// </summary>
    public List<SelectListItem> ExpenseAccounts { get; set; } = new();

    /// <summary>
    /// Approval history
    /// </summary>
    public List<ApprovalHistoryDto> ApprovalHistory { get; set; } = new();

    /// <summary>
    /// Match type for OCR extraction
    /// </summary>
    public CoreMatchType MatchType { get; set; }

    /// <summary>
    /// Vendor NTN for display
    /// </summary>
    public string? VendorNtn { get; set; }

    // Aliases for view compatibility - these access Invoice model properties
    public string ImagePath => InvoiceFileUrl;
    public string InvoiceNumber => Invoice.InvoiceNumber;
    public Guid? VendorId => Invoice.VendorId;
    public DateTime InvoiceDate => Invoice.InvoiceDate;
    public DateTime? DueDate => Invoice.DueDate;
    public decimal SubTotal => Invoice.Subtotal;
    public decimal TaxAmount => Invoice.TaxAmount;
    public decimal TotalAmount => Invoice.TotalAmount;
    public string? Notes => Invoice.Notes;
    public List<InvoiceItemFormModel> Items => Invoice.Items;
}

/// <summary>
/// Invoice form model for binding
/// </summary>
public class InvoiceFormModel
{
    public Guid Id { get; set; }

    [Display(Name = "Vendor")]
    public Guid? VendorId { get; set; }

    [Required(ErrorMessage = "Invoice number is required")]
    [Display(Name = "Invoice Number")]
    [StringLength(100)]
    public string InvoiceNumber { get; set; } = string.Empty;

    [Required(ErrorMessage = "Invoice date is required")]
    [Display(Name = "Invoice Date")]
    [DataType(DataType.Date)]
    public DateTime InvoiceDate { get; set; } = DateTime.Today;

    [Display(Name = "Due Date")]
    [DataType(DataType.Date)]
    public DateTime? DueDate { get; set; }

    [Display(Name = "Subtotal")]
    [Range(0, double.MaxValue)]
    public decimal Subtotal { get; set; }

    /// <summary>
    /// Alias for Subtotal for view compatibility
    /// </summary>
    public decimal SubTotal
    {
        get => Subtotal;
        set => Subtotal = value;
    }

    [Display(Name = "Tax Amount")]
    [Range(0, double.MaxValue)]
    public decimal TaxAmount { get; set; }

    [Required(ErrorMessage = "Total amount is required")]
    [Display(Name = "Total Amount")]
    [Range(0.01, double.MaxValue, ErrorMessage = "Total amount must be greater than 0")]
    public decimal TotalAmount { get; set; }

    [Display(Name = "Currency")]
    public string Currency { get; set; } = "PKR";

    [Display(Name = "Notes")]
    [StringLength(2000)]
    public string? Notes { get; set; }

    /// <summary>
    /// Line items
    /// </summary>
    public List<InvoiceItemFormModel> Items { get; set; } = new();
}

/// <summary>
/// Invoice line item form model
/// </summary>
public class InvoiceItemFormModel
{
    public Guid? Id { get; set; }

    [Display(Name = "Expense Account")]
    public string? ExpenseAccountId { get; set; }

    [Required(ErrorMessage = "Description is required")]
    [Display(Name = "Description")]
    [StringLength(500)]
    public string Description { get; set; } = string.Empty;

    [Display(Name = "Quantity")]
    [Range(0.001, double.MaxValue)]
    public decimal Quantity { get; set; } = 1;

    [Display(Name = "Unit")]
    [StringLength(50)]
    public string? Unit { get; set; }

    [Display(Name = "Unit Price")]
    [Range(0, double.MaxValue)]
    public decimal UnitPrice { get; set; }

    [Display(Name = "Tax")]
    [Range(0, double.MaxValue)]
    public decimal TaxAmount { get; set; }

    [Display(Name = "Amount")]
    [Range(0, double.MaxValue)]
    public decimal Amount { get; set; }

    public int LineNumber { get; set; }
}

/// <summary>
/// Invoice review view model for approvers
/// </summary>
public class InvoiceReviewViewModel
{
    public Guid Id { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public DateTime InvoiceDate { get; set; }
    public DateTime? DueDate { get; set; }
    public string VendorName { get; set; } = string.Empty;
    public string? VendorNtn { get; set; }
    public decimal SubTotal { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public string Currency { get; set; } = "PKR";
    public InvoiceStatus Status { get; set; }
    public decimal? OcrConfidence { get; set; }
    public string? Notes { get; set; }

    /// <summary>
    /// URL to the invoice file for preview
    /// </summary>
    public string InvoiceFileUrl { get; set; } = string.Empty;

    /// <summary>
    /// Path to the invoice image (alias for InvoiceFileUrl for view compatibility)
    /// </summary>
    public string ImagePath => InvoiceFileUrl;

    public bool IsPdf { get; set; }

    /// <summary>
    /// Line items with account names
    /// </summary>
    public List<InvoiceItemReviewModel> Items { get; set; } = new();

    /// <summary>
    /// Approval history
    /// </summary>
    public List<ApprovalHistoryDto> ApprovalHistory { get; set; } = new();

    /// <summary>
    /// Can the current user approve
    /// </summary>
    public bool CanApprove { get; set; }

    /// <summary>
    /// Required approval level
    /// </summary>
    public ApprovalLevel RequiredLevel { get; set; }

    /// <summary>
    /// Company name
    /// </summary>
    public string CompanyName { get; set; } = string.Empty;

    /// <summary>
    /// Who uploaded the invoice
    /// </summary>
    public string UploadedByName { get; set; } = string.Empty;

    /// <summary>
    /// Alias for UploadedByName for view compatibility
    /// </summary>
    public string CreatedByName => UploadedByName;

    public DateTime UploadedAt { get; set; }

    /// <summary>
    /// Alias for UploadedAt for view compatibility
    /// </summary>
    public DateTime CreatedAt => UploadedAt;
}

/// <summary>
/// Invoice item for review display
/// </summary>
public class InvoiceItemReviewModel
{
    public string Description { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public string? Unit { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal Amount { get; set; }
    public string? ExpenseAccountName { get; set; }
    public string? ExpenseAccountCode { get; set; }
    public CoreMatchType MatchType { get; set; }
}
