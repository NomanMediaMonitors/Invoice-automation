using InvoiceAutomation.Web.Core.Enums;

namespace InvoiceAutomation.Web.Core.Entities;

/// <summary>
/// Invoice entity - the main document being processed
/// </summary>
public class Invoice
{
    public Guid Id { get; set; }

    public Guid CompanyId { get; set; }

    public Guid? VendorId { get; set; }

    public Guid UploadedById { get; set; }

    /// <summary>
    /// Invoice number from the document
    /// </summary>
    public string InvoiceNumber { get; set; } = string.Empty;

    /// <summary>
    /// Date on the invoice
    /// </summary>
    public DateTime InvoiceDate { get; set; }

    /// <summary>
    /// Payment due date
    /// </summary>
    public DateTime? DueDate { get; set; }

    /// <summary>
    /// Subtotal before tax
    /// </summary>
    public decimal Subtotal { get; set; }

    /// <summary>
    /// Tax amount (GST/Sales Tax)
    /// </summary>
    public decimal TaxAmount { get; set; }

    /// <summary>
    /// Total amount including tax
    /// </summary>
    public decimal TotalAmount { get; set; }

    /// <summary>
    /// Currency code (ISO 4217)
    /// </summary>
    public string Currency { get; set; } = "PKR";

    /// <summary>
    /// Current processing status
    /// </summary>
    public InvoiceStatus Status { get; set; } = InvoiceStatus.Draft;

    /// <summary>
    /// Relative path to the original uploaded file
    /// </summary>
    public string OriginalFilePath { get; set; } = string.Empty;

    /// <summary>
    /// Original filename as uploaded
    /// </summary>
    public string OriginalFileName { get; set; } = string.Empty;

    /// <summary>
    /// File content type (MIME)
    /// </summary>
    public string ContentType { get; set; } = string.Empty;

    /// <summary>
    /// File size in bytes
    /// </summary>
    public long FileSize { get; set; }

    /// <summary>
    /// Raw OCR extracted text (JSON)
    /// </summary>
    public string? OcrData { get; set; }

    /// <summary>
    /// OCR confidence percentage (0-100)
    /// </summary>
    public decimal? OcrConfidence { get; set; }

    /// <summary>
    /// Notes/comments about this invoice
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// External reference ID after posting to accounting system
    /// </summary>
    public string? ExternalRef { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    public virtual Company Company { get; set; } = null!;
    public virtual Vendor? Vendor { get; set; }
    public virtual User UploadedBy { get; set; } = null!;
    public virtual ICollection<InvoiceItem> Items { get; set; } = new List<InvoiceItem>();
    public virtual ICollection<InvoiceApproval> Approvals { get; set; } = new List<InvoiceApproval>();
    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();
}
