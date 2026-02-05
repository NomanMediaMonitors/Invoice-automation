using InvoiceAutomation.Web.Core.DTOs;
using InvoiceAutomation.Web.Core.Entities;
using InvoiceAutomation.Web.Core.Enums;
using InvoiceAutomation.Web.Core.Interfaces;
using InvoiceAutomation.Web.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace InvoiceAutomation.Web.Core.Services;

/// <summary>
/// Payment processing service
/// </summary>
public class PaymentService : IPaymentService
{
    private readonly ApplicationDbContext _context;
    private readonly IChartOfAccountsService _coaService;
    private readonly IAccountingApiClientFactory _apiClientFactory;
    private readonly ILogger<PaymentService> _logger;

    public PaymentService(
        ApplicationDbContext context,
        IChartOfAccountsService coaService,
        IAccountingApiClientFactory apiClientFactory,
        ILogger<PaymentService> logger)
    {
        _context = context;
        _coaService = coaService;
        _apiClientFactory = apiClientFactory;
        _logger = logger;
    }

    public async Task<Payment> SchedulePaymentAsync(Guid invoiceId, PaymentScheduleDto dto)
    {
        var invoice = await _context.Invoices
            .Include(i => i.Payments)
            .FirstOrDefaultAsync(i => i.Id == invoiceId)
            ?? throw new KeyNotFoundException($"Invoice not found: {invoiceId}");

        if (invoice.Status != InvoiceStatus.Approved)
            throw new InvalidOperationException("Only approved invoices can have payments scheduled");

        // Check if payment already exists
        if (invoice.Payments.Any(p => p.Status != PaymentStatus.Failed))
            throw new InvalidOperationException("A payment already exists for this invoice");

        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            InvoiceId = invoiceId,
            PaymentAccountId = dto.PaymentAccountId,
            PaymentAccountName = dto.PaymentAccountName,
            Amount = dto.Amount,
            PaymentMethod = dto.PaymentMethod,
            ReferenceNumber = dto.ReferenceNumber,
            ScheduledDate = dto.ScheduledDate ?? DateTime.Today,
            Status = PaymentStatus.Scheduled,
            CreatedAt = DateTime.UtcNow
        };

        invoice.Status = InvoiceStatus.PaymentPending;
        invoice.UpdatedAt = DateTime.UtcNow;

        _context.Payments.Add(payment);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Payment scheduled for invoice {InvoiceId}: {PaymentId}", invoiceId, payment.Id);
        return payment;
    }

    public async Task<Payment> ExecutePaymentAsync(Guid paymentId, Guid executedById)
    {
        var payment = await _context.Payments
            .Include(p => p.Invoice)
                .ThenInclude(i => i.Items)
            .Include(p => p.Invoice.Vendor)
            .FirstOrDefaultAsync(p => p.Id == paymentId)
            ?? throw new KeyNotFoundException($"Payment not found: {paymentId}");

        if (payment.Status != PaymentStatus.Scheduled)
            throw new InvalidOperationException("Only scheduled payments can be executed");

        payment.Status = PaymentStatus.Processing;
        payment.Invoice.Status = InvoiceStatus.PaymentProcessing;
        await _context.SaveChangesAsync();

        try
        {
            // 1. Generate journal entry
            var journalEntry = await GenerateJournalEntryAsync(payment);

            // 2. Post to accounting system
            var apiClient = _apiClientFactory.CreateClient(payment.Invoice.CompanyId);

            var journalResult = await apiClient.CreateJournalEntryAsync(journalEntry);

            if (!journalResult.Success)
            {
                throw new Exception($"Failed to create journal entry: {journalResult.ErrorMessage}");
            }

            // 3. Record payment in accounting system
            var paymentRecord = new PaymentRecordDto
            {
                BillId = payment.Invoice.ExternalRef ?? "",
                BankAccountId = payment.PaymentAccountId,
                Amount = payment.Amount,
                PaymentDate = DateTime.Today,
                ReferenceNumber = payment.ReferenceNumber,
                Memo = $"Payment for invoice {payment.Invoice.InvoiceNumber}"
            };

            // Note: In some cases you might skip this if journal entry handles it
            // var paymentResult = await apiClient.RecordPaymentAsync(paymentRecord);

            // 4. Update payment record
            payment.Status = PaymentStatus.Completed;
            payment.ExecutedById = executedById;
            payment.ExecutedAt = DateTime.UtcNow;
            payment.JournalEntryRef = journalResult.ReferenceNumber;
            payment.ExternalRef = journalResult.ExternalId;

            // 5. Update invoice status
            payment.Invoice.Status = InvoiceStatus.Completed;
            payment.Invoice.ExternalRef = journalResult.ExternalId;
            payment.Invoice.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Payment executed: {PaymentId}, Journal: {JournalRef}",
                paymentId, journalResult.ReferenceNumber);

            return payment;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Payment execution failed: {PaymentId}", paymentId);

            payment.Status = PaymentStatus.Failed;
            payment.FailureReason = ex.Message;
            payment.Invoice.Status = InvoiceStatus.Approved; // Revert to approved
            payment.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            throw;
        }
    }

    public async Task<Payment?> GetByIdAsync(Guid id)
    {
        return await _context.Payments
            .Include(p => p.Invoice)
                .ThenInclude(i => i.Vendor)
            .Include(p => p.ExecutedBy)
            .FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<List<Payment>> GetByInvoiceIdAsync(Guid invoiceId)
    {
        return await _context.Payments
            .Include(p => p.ExecutedBy)
            .Where(p => p.InvoiceId == invoiceId)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<Payment>> GetPendingPaymentsAsync(Guid companyId)
    {
        return await _context.Payments
            .Include(p => p.Invoice)
                .ThenInclude(i => i.Vendor)
            .Where(p => p.Invoice.CompanyId == companyId &&
                       (p.Status == PaymentStatus.Scheduled || p.Status == PaymentStatus.Processing))
            .OrderBy(p => p.ScheduledDate)
            .ToListAsync();
    }

    public async Task CancelPaymentAsync(Guid paymentId)
    {
        var payment = await _context.Payments
            .Include(p => p.Invoice)
            .FirstOrDefaultAsync(p => p.Id == paymentId)
            ?? throw new KeyNotFoundException($"Payment not found: {paymentId}");

        if (payment.Status != PaymentStatus.Scheduled)
            throw new InvalidOperationException("Only scheduled payments can be cancelled");

        _context.Payments.Remove(payment);

        payment.Invoice.Status = InvoiceStatus.Approved;
        payment.Invoice.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Payment cancelled: {PaymentId}", paymentId);
    }

    public async Task<JournalEntryDto> PreviewJournalEntryAsync(Guid invoiceId, string paymentAccountId)
    {
        var invoice = await _context.Invoices
            .Include(i => i.Items)
            .Include(i => i.Vendor)
            .FirstOrDefaultAsync(i => i.Id == invoiceId)
            ?? throw new KeyNotFoundException($"Invoice not found: {invoiceId}");

        // Get account details
        var paymentAccount = await _coaService.GetAccountByIdAsync(invoice.CompanyId, paymentAccountId);
        var expenseAccounts = new Dictionary<string, AccountDto>();

        foreach (var item in invoice.Items.Where(i => !string.IsNullOrEmpty(i.ExpenseAccountId)))
        {
            if (!expenseAccounts.ContainsKey(item.ExpenseAccountId!))
            {
                var account = await _coaService.GetAccountByIdAsync(invoice.CompanyId, item.ExpenseAccountId!);
                if (account != null)
                {
                    expenseAccounts[item.ExpenseAccountId!] = account;
                }
            }
        }

        return CreateJournalEntry(invoice, paymentAccountId, paymentAccount, expenseAccounts);
    }

    public async Task<PaymentStatisticsDto> GetStatisticsAsync(Guid companyId)
    {
        var payments = await _context.Payments
            .Include(p => p.Invoice)
            .Where(p => p.Invoice.CompanyId == companyId)
            .ToListAsync();

        var stats = new PaymentStatisticsDto
        {
            TotalPayments = payments.Count,
            PendingPayments = payments.Count(p => p.Status == PaymentStatus.Scheduled),
            CompletedPayments = payments.Count(p => p.Status == PaymentStatus.Completed),
            FailedPayments = payments.Count(p => p.Status == PaymentStatus.Failed),
            TotalPaid = payments.Where(p => p.Status == PaymentStatus.Completed).Sum(p => p.Amount),
            TotalPending = payments.Where(p => p.Status == PaymentStatus.Scheduled).Sum(p => p.Amount)
        };

        // Payments by month for the last 6 months
        var sixMonthsAgo = DateTime.Today.AddMonths(-6);
        stats.PaymentsByMonth = payments
            .Where(p => p.Status == PaymentStatus.Completed && p.ExecutedAt >= sixMonthsAgo)
            .GroupBy(p => p.ExecutedAt!.Value.ToString("yyyy-MM"))
            .ToDictionary(g => g.Key, g => g.Sum(p => p.Amount));

        return stats;
    }

    private async Task<JournalEntryDto> GenerateJournalEntryAsync(Payment payment)
    {
        var invoice = payment.Invoice;

        // Get account details from COA service
        var paymentAccount = await _coaService.GetAccountByIdAsync(invoice.CompanyId, payment.PaymentAccountId);
        var expenseAccounts = new Dictionary<string, AccountDto>();

        foreach (var item in invoice.Items.Where(i => !string.IsNullOrEmpty(i.ExpenseAccountId)))
        {
            if (!expenseAccounts.ContainsKey(item.ExpenseAccountId!))
            {
                var account = await _coaService.GetAccountByIdAsync(invoice.CompanyId, item.ExpenseAccountId!);
                if (account != null)
                {
                    expenseAccounts[item.ExpenseAccountId!] = account;
                }
            }
        }

        return CreateJournalEntry(invoice, payment.PaymentAccountId, paymentAccount, expenseAccounts);
    }

    private JournalEntryDto CreateJournalEntry(
        Invoice invoice,
        string paymentAccountId,
        AccountDto? paymentAccount,
        Dictionary<string, AccountDto> expenseAccounts)
    {
        var entry = new JournalEntryDto
        {
            Date = DateTime.Today,
            Memo = $"Payment for Invoice {invoice.InvoiceNumber} - {invoice.Vendor?.Name ?? "Vendor"}",
            ReferenceNumber = $"PAY-{invoice.InvoiceNumber}",
            Lines = new List<JournalLineDto>()
        };

        // Group line items by expense account
        var itemsByAccount = invoice.Items
            .Where(i => !string.IsNullOrEmpty(i.ExpenseAccountId))
            .GroupBy(i => i.ExpenseAccountId!)
            .ToList();

        // Debit: Expense accounts
        foreach (var group in itemsByAccount)
        {
            var accountId = group.Key;
            var amount = group.Sum(i => i.Amount);
            expenseAccounts.TryGetValue(accountId, out var account);

            entry.Lines.Add(new JournalLineDto
            {
                AccountId = accountId,
                AccountName = account?.Name ?? "Expense",
                AccountCode = account?.Code ?? "",
                AccountType = AccountType.Expense,
                DebitAmount = amount,
                CreditAmount = 0,
                Description = $"Invoice {invoice.InvoiceNumber}"
            });
        }

        // Credit: Bank/Cash account
        entry.Lines.Add(new JournalLineDto
        {
            AccountId = paymentAccountId,
            AccountName = paymentAccount?.Name ?? "Bank Account",
            AccountCode = paymentAccount?.Code ?? "",
            AccountType = AccountType.Asset,
            DebitAmount = 0,
            CreditAmount = invoice.TotalAmount,
            Description = $"Payment for Invoice {invoice.InvoiceNumber}"
        });

        return entry;
    }
}
