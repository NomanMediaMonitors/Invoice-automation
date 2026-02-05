namespace InvoiceAutomation.Web.ViewModels;

/// <summary>
/// Error view model
/// </summary>
public class ErrorViewModel
{
    public string? RequestId { get; set; }
    public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
    public string? Message { get; set; }
    public int? StatusCode { get; set; }
}
