using InvoiceAutomation.Web.Core.DTOs;

namespace InvoiceAutomation.Web.Core.Interfaces;

/// <summary>
/// Interface for OCR processing service
/// </summary>
public interface IOcrService
{
    /// <summary>
    /// Extract invoice data from an uploaded file
    /// </summary>
    /// <param name="filePath">Physical path to the file</param>
    /// <returns>Extracted invoice data with confidence scores</returns>
    Task<InvoiceOcrResult> ExtractInvoiceDataAsync(string filePath);

    /// <summary>
    /// Check if the OCR engine is available and properly configured
    /// </summary>
    Task<bool> IsAvailableAsync();
}
