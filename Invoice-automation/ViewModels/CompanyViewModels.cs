using System.ComponentModel.DataAnnotations;
using InvoiceAutomation.Web.Core.DTOs;
using InvoiceAutomation.Web.Core.Enums;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace InvoiceAutomation.Web.ViewModels;

/// <summary>
/// Company list view model
/// </summary>
public class CompanyListViewModel
{
    public List<CompanyListItemViewModel> Companies { get; set; } = new();
}

/// <summary>
/// Company list item
/// </summary>
public class CompanyListItemViewModel
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Ntn { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public UserRole UserRole { get; set; }
    public bool IsDefault { get; set; }
    public bool AccountingConnected { get; set; }
    public AccountingProvider AccountingProvider { get; set; }
    public int InvoiceCount { get; set; }
    public int PendingApprovalCount { get; set; }
}

/// <summary>
/// Company create/edit view model
/// </summary>
public class CompanyFormViewModel
{
    public Guid? Id { get; set; }

    [Required(ErrorMessage = "Company name is required")]
    [Display(Name = "Company Name")]
    [StringLength(200, MinimumLength = 2)]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "NTN is required")]
    [Display(Name = "National Tax Number (NTN)")]
    [StringLength(20)]
    [RegularExpression(@"^\d{7,8}(-\d)?$", ErrorMessage = "Invalid NTN format")]
    public string Ntn { get; set; } = string.Empty;

    [Display(Name = "Sales Tax Registration Number (STRN)")]
    [StringLength(20)]
    public string? Strn { get; set; }

    [Required(ErrorMessage = "Address is required")]
    [Display(Name = "Address")]
    [StringLength(500)]
    public string Address { get; set; } = string.Empty;

    [Required(ErrorMessage = "City is required")]
    [Display(Name = "City")]
    [StringLength(100)]
    public string City { get; set; } = string.Empty;

    [Display(Name = "State/Province")]
    [StringLength(100)]
    public string? State { get; set; }

    [Display(Name = "Country")]
    [StringLength(100)]
    public string Country { get; set; } = "Pakistan";

    [Display(Name = "Postal Code")]
    [StringLength(20)]
    public string? PostalCode { get; set; }

    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email address")]
    [Display(Name = "Email")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Phone is required")]
    [Phone]
    [Display(Name = "Phone")]
    public string Phone { get; set; } = string.Empty;

    [Url]
    [Display(Name = "Website")]
    public string? Website { get; set; }

    [Display(Name = "Fiscal Year Start Month")]
    [Range(1, 12)]
    public int FiscalYearStartMonth { get; set; } = 7;

    [Display(Name = "Default Currency")]
    public string DefaultCurrency { get; set; } = "PKR";

    // Accounting Integration
    [Display(Name = "Accounting Provider")]
    public AccountingProvider AccountingProvider { get; set; } = AccountingProvider.None;

    [Display(Name = "API Base URL")]
    [Url]
    public string? ApiBaseUrl { get; set; }

    [Display(Name = "Client ID")]
    public string? ClientId { get; set; }

    [Display(Name = "Client Secret")]
    public string? ClientSecret { get; set; }

    [Display(Name = "Realm ID")]
    public string? RealmId { get; set; }

    // Approval Settings
    [Display(Name = "Manager Approval Threshold")]
    [Range(0, double.MaxValue)]
    public decimal ManagerApprovalThreshold { get; set; } = 50000;

    [Display(Name = "Admin Approval Threshold")]
    [Range(0, double.MaxValue)]
    public decimal AdminApprovalThreshold { get; set; } = 200000;

    [Display(Name = "Active")]
    public bool IsActive { get; set; } = true;

    public List<SelectListItem> CurrencyOptions { get; set; } = new();
    public List<SelectListItem> MonthOptions { get; set; } = new();
}

/// <summary>
/// Company details view model
/// </summary>
public class CompanyDetailsViewModel
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Ntn { get; set; } = string.Empty;
    public string? Strn { get; set; }
    public string Address { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string? State { get; set; }
    public string Country { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string? Website { get; set; }
    public string? LogoPath { get; set; }
    public int FiscalYearStartMonth { get; set; }
    public string DefaultCurrency { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }

    // Accounting connection
    public bool AccountingConnected { get; set; }
    public AccountingProvider AccountingProvider { get; set; }

    // Users
    public List<UserCompanyDto> Users { get; set; } = new();

    // Statistics
    public int TotalInvoices { get; set; }
    public int PendingInvoices { get; set; }
    public int TotalVendors { get; set; }
    public decimal TotalAmount { get; set; }

    // Current user's role
    public UserRole CurrentUserRole { get; set; }
}

/// <summary>
/// Company user management view model
/// </summary>
public class CompanyUsersViewModel
{
    public Guid CompanyId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public List<UserCompanyDto> Users { get; set; } = new();
    public UserRole CurrentUserRole { get; set; }

    // Add user form
    public AddUserFormModel AddUserForm { get; set; } = new();
    public List<SelectListItem> RoleOptions { get; set; } = new();

    /// <summary>
    /// Available users that can be added to the company
    /// </summary>
    public List<SelectListItem> AvailableUsers { get; set; } = new();
}

/// <summary>
/// Add user form model
/// </summary>
public class AddUserFormModel
{
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email address")]
    [Display(Name = "User Email")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Role is required")]
    [Display(Name = "Role")]
    public UserRole Role { get; set; } = UserRole.Viewer;
}

/// <summary>
/// Accounting connection view model
/// </summary>
public class AccountingConnectionViewModel
{
    public Guid CompanyId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public bool IsConnected { get; set; }
    public AccountingProvider? CurrentProvider { get; set; }
    public DateTime? ConnectedAt { get; set; }

    // OAuth URLs
    public string? EndraajOAuthUrl { get; set; }
    public string? QuickBooksOAuthUrl { get; set; }
}

/// <summary>
/// Alias for CompanyFormViewModel for Create view
/// </summary>
public class CompanyCreateViewModel : CompanyFormViewModel { }

/// <summary>
/// Alias for CompanyFormViewModel for Edit view
/// </summary>
public class CompanyEditViewModel : CompanyFormViewModel { }
