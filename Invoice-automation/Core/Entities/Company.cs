using InvoiceAutomation.Web.Core.Enums;

namespace InvoiceAutomation.Web.Core.Entities;

/// <summary>
/// Company entity representing a registered business
/// </summary>
public class Company
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// National Tax Number (Pakistan)
    /// </summary>
    public string Ntn { get; set; } = string.Empty;

    /// <summary>
    /// Sales Tax Registration Number (optional)
    /// </summary>
    public string? Strn { get; set; }

    public string Address { get; set; } = string.Empty;

    public string City { get; set; } = string.Empty;

    public string? State { get; set; }

    public string Country { get; set; } = "Pakistan";

    public string? PostalCode { get; set; }

    public string Email { get; set; } = string.Empty;

    public string Phone { get; set; } = string.Empty;

    public string? Website { get; set; }

    /// <summary>
    /// Logo file path
    /// </summary>
    public string? LogoPath { get; set; }

    /// <summary>
    /// Fiscal year start month (1-12)
    /// </summary>
    public int FiscalYearStartMonth { get; set; } = 7; // July for Pakistan

    /// <summary>
    /// Default currency code (ISO 4217)
    /// </summary>
    public string DefaultCurrency { get; set; } = "PKR";

    /// <summary>
    /// Connected accounting system
    /// </summary>
    public AccountingProvider AccountingProvider { get; set; } = AccountingProvider.None;

    /// <summary>
    /// Encrypted OAuth access token for accounting API
    /// </summary>
    public string? EncryptedAccessToken { get; set; }

    /// <summary>
    /// Encrypted OAuth refresh token
    /// </summary>
    public string? EncryptedRefreshToken { get; set; }

    /// <summary>
    /// Token expiration time
    /// </summary>
    public DateTime? TokenExpiresAt { get; set; }

    /// <summary>
    /// External company/realm ID in accounting system
    /// </summary>
    public string? ExternalCompanyId { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    public virtual ICollection<UserCompany> UserCompanies { get; set; } = new List<UserCompany>();
    public virtual ICollection<Vendor> Vendors { get; set; } = new List<Vendor>();
    public virtual ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();
}
