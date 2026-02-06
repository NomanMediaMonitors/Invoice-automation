namespace InvoiceAutomation.Web.Core.Entities;

/// <summary>
/// Application user - simple entity matching database schema
/// </summary>
public class User
{
    public Guid Id { get; set; }

    public string Email { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;

    public string? Phone { get; set; }

    public bool IsActive { get; set; } = true;

    public bool EmailVerified { get; set; } = false;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Additional properties not in base schema but used in app
    public DateTime? UpdatedAt { get; set; }

    public DateTime? LastLoginAt { get; set; }

    // Navigation properties
    public virtual ICollection<UserCompany> UserCompanies { get; set; } = new List<UserCompany>();
    public virtual ICollection<Invoice> UploadedInvoices { get; set; } = new List<Invoice>();
    public virtual ICollection<InvoiceApproval> Approvals { get; set; } = new List<InvoiceApproval>();
    public virtual ICollection<Payment> ExecutedPayments { get; set; } = new List<Payment>();
}

/// <summary>
/// Application role - simple entity
/// </summary>
public class Role
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
