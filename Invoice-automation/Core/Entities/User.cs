using Microsoft.AspNetCore.Identity;

namespace InvoiceAutomation.Web.Core.Entities;

/// <summary>
/// Application user extending ASP.NET Core Identity
/// </summary>
public class User : IdentityUser<Guid>
{
    public string FullName { get; set; } = string.Empty;

    public string? Phone { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    public DateTime? LastLoginAt { get; set; }

    // Navigation properties
    public virtual ICollection<UserCompany> UserCompanies { get; set; } = new List<UserCompany>();
    public virtual ICollection<Invoice> UploadedInvoices { get; set; } = new List<Invoice>();
    public virtual ICollection<InvoiceApproval> Approvals { get; set; } = new List<InvoiceApproval>();
    public virtual ICollection<Payment> ExecutedPayments { get; set; } = new List<Payment>();
}

/// <summary>
/// Application role
/// </summary>
public class Role : IdentityRole<Guid>
{
    public string? Description { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
