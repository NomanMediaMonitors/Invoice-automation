using InvoiceAutomation.Web.Core.Enums;

namespace InvoiceAutomation.Web.Core.DTOs;

/// <summary>
/// Create company data
/// </summary>
public class CompanyCreateDto
{
    public string Name { get; set; } = string.Empty;
    public string Ntn { get; set; } = string.Empty;
    public string? Strn { get; set; }
    public string Address { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string? State { get; set; }
    public string Country { get; set; } = "Pakistan";
    public string? PostalCode { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string? Website { get; set; }
    public int FiscalYearStartMonth { get; set; } = 7;
    public string DefaultCurrency { get; set; } = "PKR";
}

/// <summary>
/// Update company data
/// </summary>
public class CompanyUpdateDto
{
    public string? Name { get; set; }
    public string? Strn { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? Country { get; set; }
    public string? PostalCode { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Website { get; set; }
    public int? FiscalYearStartMonth { get; set; }
    public bool? IsActive { get; set; }
}

/// <summary>
/// User in company
/// </summary>
public class UserCompanyDto
{
    public Guid UserId { get; set; }
    public Guid CompanyId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public bool IsActive { get; set; }
    public DateTime JoinedAt { get; set; }
}

/// <summary>
/// Accounting system connection data
/// </summary>
public class AccountingConnectionDto
{
    public AccountingProvider Provider { get; set; }
    public string AccessToken { get; set; } = string.Empty;
    public string? RefreshToken { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public string? ExternalCompanyId { get; set; }
}
