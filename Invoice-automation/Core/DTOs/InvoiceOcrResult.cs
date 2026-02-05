namespace InvoiceAutomation.Web.Core.DTOs;

/// <summary>
/// Result from OCR processing of an invoice
/// </summary>
public class InvoiceOcrResult
{
    /// <summary>
    /// Raw extracted text
    /// </summary>
    public string RawText { get; set; } = string.Empty;

    /// <summary>
    /// Overall confidence percentage (0-100)
    /// </summary>
    public decimal Confidence { get; set; }

    /// <summary>
    /// Extracted invoice number
    /// </summary>
    public string? InvoiceNumber { get; set; }

    /// <summary>
    /// Extracted invoice date
    /// </summary>
    public DateTime? InvoiceDate { get; set; }

    /// <summary>
    /// Extracted due date
    /// </summary>
    public DateTime? DueDate { get; set; }

    /// <summary>
    /// Extracted vendor name
    /// </summary>
    public string? VendorName { get; set; }

    /// <summary>
    /// Extracted vendor NTN
    /// </summary>
    public string? VendorNtn { get; set; }

    /// <summary>
    /// Extracted vendor address
    /// </summary>
    public string? VendorAddress { get; set; }

    /// <summary>
    /// Extracted subtotal amount
    /// </summary>
    public decimal? Subtotal { get; set; }

    /// <summary>
    /// Extracted tax amount
    /// </summary>
    public decimal? TaxAmount { get; set; }

    /// <summary>
    /// Extracted total amount
    /// </summary>
    public decimal? TotalAmount { get; set; }

    /// <summary>
    /// Detected currency
    /// </summary>
    public string Currency { get; set; } = "PKR";

    /// <summary>
    /// Extracted line items
    /// </summary>
    public List<OcrLineItem> LineItems { get; set; } = new();

    /// <summary>
    /// Any errors during processing
    /// </summary>
    public List<string> Errors { get; set; } = new();

    /// <summary>
    /// Warnings about low confidence fields
    /// </summary>
    public List<string> Warnings { get; set; } = new();

    /// <summary>
    /// Processing time in milliseconds
    /// </summary>
    public long ProcessingTimeMs { get; set; }
}

/// <summary>
/// Line item extracted from OCR
/// </summary>
public class OcrLineItem
{
    public string Description { get; set; } = string.Empty;
    public decimal? Quantity { get; set; }
    public string? Unit { get; set; }
    public decimal? UnitPrice { get; set; }
    public decimal? Amount { get; set; }
    public decimal Confidence { get; set; }
}
