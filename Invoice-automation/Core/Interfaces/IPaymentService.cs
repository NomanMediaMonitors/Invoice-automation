using InvoiceAutomation.Web.Core.DTOs;
using InvoiceAutomation.Web.Core.Entities;

namespace InvoiceAutomation.Web.Core.Interfaces;

/// <summary>
/// Interface for payment operations
/// </summary>
public interface IPaymentService
{
    /// <summary>
    /// Schedule a payment for an approved invoice
    /// </summary>
    Task<Payment> SchedulePaymentAsync(Guid invoiceId, PaymentScheduleDto dto);

    /// <summary>
    /// Execute a scheduled payment and create journal entry
    /// </summary>
    Task<Payment> ExecutePaymentAsync(Guid paymentId, Guid executedById);

    /// <summary>
    /// Get payment details
    /// </summary>
    Task<Payment?> GetByIdAsync(Guid id);

    /// <summary>
    /// Get payments for an invoice
    /// </summary>
    Task<List<Payment>> GetByInvoiceIdAsync(Guid invoiceId);

    /// <summary>
    /// Get pending payments for a company
    /// </summary>
    Task<List<Payment>> GetPendingPaymentsAsync(Guid companyId);

    /// <summary>
    /// Cancel a scheduled payment
    /// </summary>
    Task CancelPaymentAsync(Guid paymentId);

    /// <summary>
    /// Generate journal entry preview (without posting)
    /// </summary>
    Task<JournalEntryDto> PreviewJournalEntryAsync(Guid invoiceId, string paymentAccountId);

    /// <summary>
    /// Get payment statistics
    /// </summary>
    Task<PaymentStatisticsDto> GetStatisticsAsync(Guid companyId);
}
