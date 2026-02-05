namespace InvoiceAutomation.Web.Core.DTOs;

/// <summary>
/// Create vendor data
/// </summary>
public class VendorCreateDto
{
    public string Name { get; set; } = string.Empty;
    public string? Ntn { get; set; }
    public string? Strn { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? ContactPerson { get; set; }
    public string? DefaultExpenseAccountId { get; set; }
    public int PaymentTermsDays { get; set; } = 30;
}

/// <summary>
/// Update vendor data
/// </summary>
public class VendorUpdateDto
{
    public string? Name { get; set; }
    public string? Ntn { get; set; }
    public string? Strn { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? ContactPerson { get; set; }
    public string? DefaultExpenseAccountId { get; set; }
    public int? PaymentTermsDays { get; set; }
    public bool? IsActive { get; set; }
}

/// <summary>
/// Vendor sync to accounting system
/// </summary>
public class VendorSyncDto
{
    public string? ExternalId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public string? TaxNumber { get; set; }
}

/// <summary>
/// Result of vendor sync
/// </summary>
public class VendorSyncResultDto
{
    public bool Success { get; set; }
    public string? ExternalId { get; set; }
    public string? ErrorMessage { get; set; }
}
