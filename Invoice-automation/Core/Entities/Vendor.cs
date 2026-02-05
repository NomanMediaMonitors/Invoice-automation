namespace InvoiceAutomation.Web.Core.Entities;

/// <summary>
/// Vendor/Supplier entity
/// </summary>
public class Vendor
{
    public Guid Id { get; set; }

    public Guid CompanyId { get; set; }

    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Vendor's National Tax Number
    /// </summary>
    public string? Ntn { get; set; }

    /// <summary>
    /// Vendor's Sales Tax Registration Number
    /// </summary>
    public string? Strn { get; set; }

    public string? Email { get; set; }

    public string? Phone { get; set; }

    public string? Address { get; set; }

    public string? City { get; set; }

    public string? ContactPerson { get; set; }

    /// <summary>
    /// External account ID from accounting system for default expense account
    /// This is NOT a foreign key - it's a reference to the external COA
    /// </summary>
    public string? DefaultExpenseAccountId { get; set; }

    /// <summary>
    /// External vendor ID in accounting system (for sync)
    /// </summary>
    public string? ExternalVendorId { get; set; }

    /// <summary>
    /// Default payment terms in days
    /// </summary>
    public int PaymentTermsDays { get; set; } = 30;

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    public virtual Company Company { get; set; } = null!;
    public virtual ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();
}
