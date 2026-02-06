using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace InvoiceAutomation.Web.ViewModels;

/// <summary>
/// Vendor list view model
/// </summary>
public class VendorListViewModel
{
    public Guid CompanyId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public List<VendorListItemViewModel> Vendors { get; set; } = new();
    public bool ShowInactive { get; set; }

    // Search and filtering
    public string? SearchTerm { get; set; }

    // Count properties for view compatibility
    public int TotalCount => Vendors.Count;
    public int ActiveCount => Vendors.Count(v => v.IsActive);

    // Pagination properties for view compatibility
    public int CurrentPage { get; set; } = 1;
    public int TotalPages { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

/// <summary>
/// Vendor list item
/// </summary>
public class VendorListItemViewModel
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Ntn { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? ContactPerson { get; set; }
    public string? DefaultExpenseAccountName { get; set; }
    public int InvoiceCount { get; set; }
    public decimal TotalInvoiceAmount { get; set; }
    public bool IsActive { get; set; }

    // Alias for view compatibility
    public decimal TotalAmount => TotalInvoiceAmount;
}

/// <summary>
/// Vendor create/edit view model
/// </summary>
public class VendorFormViewModel
{
    public Guid? Id { get; set; }
    public Guid CompanyId { get; set; }
    public string CompanyName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vendor name is required")]
    [Display(Name = "Vendor Name")]
    [StringLength(200, MinimumLength = 2)]
    public string Name { get; set; } = string.Empty;

    [Display(Name = "National Tax Number (NTN)")]
    [StringLength(20)]
    public string? Ntn { get; set; }

    [Display(Name = "Sales Tax Registration Number (STRN)")]
    [StringLength(20)]
    public string? Strn { get; set; }

    [EmailAddress(ErrorMessage = "Invalid email address")]
    [Display(Name = "Email")]
    public string? Email { get; set; }

    [Phone]
    [Display(Name = "Phone")]
    public string? Phone { get; set; }

    [Display(Name = "Address")]
    [StringLength(500)]
    public string? Address { get; set; }

    [Display(Name = "City")]
    [StringLength(100)]
    public string? City { get; set; }

    [Display(Name = "Contact Person")]
    [StringLength(200)]
    public string? ContactPerson { get; set; }

    [Display(Name = "Default Expense Account")]
    public string? DefaultExpenseAccountId { get; set; }

    [Display(Name = "Payment Terms (Days)")]
    [Range(0, 365)]
    public int PaymentTermsDays { get; set; } = 30;

    [Display(Name = "Active")]
    public bool IsActive { get; set; } = true;

    // Bank details
    [Display(Name = "Bank Name")]
    [StringLength(200)]
    public string? BankName { get; set; }

    [Display(Name = "Bank Account Number")]
    [StringLength(50)]
    public string? BankAccountNumber { get; set; }

    [Display(Name = "IBAN")]
    [StringLength(50)]
    public string? BankIban { get; set; }

    [Display(Name = "External Account ID")]
    [StringLength(100)]
    public string? ExternalAccountId { get; set; }

    [Display(Name = "Notes")]
    [StringLength(2000)]
    public string? Notes { get; set; }

    // Timestamps for edit view
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// Expense accounts from accounting system
    /// </summary>
    public List<SelectListItem> ExpenseAccounts { get; set; } = new();
}

/// <summary>
/// Vendor details view model
/// </summary>
public class VendorDetailsViewModel
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Ntn { get; set; }
    public string? Strn { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? ContactPerson { get; set; }
    public string? DefaultExpenseAccountId { get; set; }
    public string? DefaultExpenseAccountName { get; set; }
    public int PaymentTermsDays { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    // Bank details
    public string? BankName { get; set; }
    public string? BankAccountNumber { get; set; }
    public string? BankIban { get; set; }

    // Statistics
    public int TotalInvoices { get; set; }
    public int PendingInvoices { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal PaidAmount { get; set; }

    // Recent invoices
    public List<InvoiceListItemViewModel> RecentInvoices { get; set; } = new();
}

/// <summary>
/// Alias for VendorFormViewModel for Create view
/// </summary>
public class VendorCreateViewModel : VendorFormViewModel { }

/// <summary>
/// Alias for VendorFormViewModel for Edit view
/// </summary>
public class VendorEditViewModel : VendorFormViewModel { }
