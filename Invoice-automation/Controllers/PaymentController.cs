using InvoiceAutomation.Web.Core.DTOs;
using InvoiceAutomation.Web.Core.Entities;
using InvoiceAutomation.Web.Core.Enums;
using InvoiceAutomation.Web.Core.Interfaces;
using InvoiceAutomation.Web.Infrastructure.Data;
using InvoiceAutomation.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace InvoiceAutomation.Web.Controllers;

/// <summary>
/// Payment management controller
/// </summary>
[Authorize]
public class PaymentController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<User> _userManager;
    private readonly IPaymentService _paymentService;
    private readonly IInvoiceService _invoiceService;
    private readonly IChartOfAccountsService _coaService;
    private readonly IFileStorageService _fileStorage;
    private readonly ILogger<PaymentController> _logger;

    public PaymentController(
        ApplicationDbContext context,
        UserManager<User> userManager,
        IPaymentService paymentService,
        IInvoiceService invoiceService,
        IChartOfAccountsService coaService,
        IFileStorageService fileStorage,
        ILogger<PaymentController> logger)
    {
        _context = context;
        _userManager = userManager;
        _paymentService = paymentService;
        _invoiceService = invoiceService;
        _coaService = coaService;
        _fileStorage = fileStorage;
        _logger = logger;
    }

    private Guid GetCurrentCompanyId()
    {
        var companyIdStr = HttpContext.Session.GetString("CurrentCompanyId");
        if (string.IsNullOrEmpty(companyIdStr) || !Guid.TryParse(companyIdStr, out var companyId))
        {
            throw new InvalidOperationException("No company selected");
        }
        return companyId;
    }

    private Guid GetCurrentUserId() => Guid.Parse(_userManager.GetUserId(User)!);

    [HttpGet]
    public async Task<IActionResult> Index(PaymentStatus? status = null)
    {
        var companyId = GetCurrentCompanyId();
        var company = await _context.Companies.FindAsync(companyId);

        var payments = await _context.Payments
            .Include(p => p.Invoice)
                .ThenInclude(i => i.Vendor)
            .Include(p => p.ExecutedBy)
            .Where(p => p.Invoice.CompanyId == companyId)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();

        if (status.HasValue)
        {
            payments = payments.Where(p => p.Status == status).ToList();
        }

        var model = new PaymentListViewModel
        {
            CompanyId = companyId,
            CompanyName = company?.Name ?? "",
            FilterStatus = status,
            Statistics = await _paymentService.GetStatisticsAsync(companyId),
            Payments = payments.Select(p => new PaymentListItemViewModel
            {
                Id = p.Id,
                InvoiceId = p.InvoiceId,
                InvoiceNumber = p.Invoice.InvoiceNumber,
                VendorName = p.Invoice.Vendor?.Name ?? "Unknown",
                PaymentAccountName = p.PaymentAccountName ?? "",
                Amount = p.Amount,
                Currency = p.Invoice.Currency,
                Status = p.Status,
                ScheduledDate = p.ScheduledDate,
                ExecutedAt = p.ExecutedAt,
                ExecutedByName = p.ExecutedBy?.FullName,
                ReferenceNumber = p.ReferenceNumber
            }).ToList(),
            StatusOptions = Enum.GetValues<PaymentStatus>()
                .Select(s => new SelectListItem(s.ToString(), ((int)s).ToString()))
                .ToList()
        };

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Execute(Guid invoiceId)
    {
        var invoice = await _invoiceService.GetByIdAsync(invoiceId);
        if (invoice == null)
        {
            return NotFound();
        }

        if (invoice.Status != InvoiceStatus.Approved)
        {
            TempData["Error"] = "Only approved invoices can have payments scheduled";
            return RedirectToAction("Review", "Invoice", new { id = invoiceId });
        }

        var paymentAccounts = await _coaService.GetPaymentAccountsAsync(invoice.CompanyId);
        var expenseAccounts = await _coaService.GetExpenseAccountsAsync(invoice.CompanyId);
        var accountDict = expenseAccounts.ToDictionary(a => a.ExternalId);

        var model = new PaymentExecuteViewModel
        {
            InvoiceId = invoice.Id,
            InvoiceNumber = invoice.InvoiceNumber,
            VendorName = invoice.Vendor?.Name ?? "Unknown",
            InvoiceDate = invoice.InvoiceDate,
            DueDate = invoice.DueDate,
            InvoiceAmount = invoice.TotalAmount,
            Currency = invoice.Currency,
            InvoiceFileUrl = _fileStorage.GetFileUrl(invoice.OriginalFilePath),
            IsPdf = invoice.ContentType.Contains("pdf", StringComparison.OrdinalIgnoreCase),
            LineItems = invoice.Items.Select(i =>
            {
                accountDict.TryGetValue(i.ExpenseAccountId ?? "", out var account);
                return new InvoiceItemReviewModel
                {
                    Description = i.Description,
                    Quantity = i.Quantity,
                    UnitPrice = i.UnitPrice,
                    Amount = i.Amount,
                    ExpenseAccountName = account?.Name,
                    ExpenseAccountCode = account?.Code,
                    MatchType = i.MatchType
                };
            }).ToList(),
            Payment = new PaymentFormModel
            {
                Amount = invoice.TotalAmount,
                ScheduledDate = DateTime.Today
            },
            PaymentAccounts = new List<SelectListItem> { new("-- Select Payment Account --", "") }
                .Concat(paymentAccounts.Select(a => new SelectListItem(a.DisplayName, a.ExternalId)))
                .ToList()
        };

        // Generate journal preview if we have a default account
        if (paymentAccounts.Any())
        {
            var defaultAccount = paymentAccounts.First();
            model.JournalPreview = await _paymentService.PreviewJournalEntryAsync(invoiceId, defaultAccount.ExternalId);
        }

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Execute(Guid invoiceId, PaymentFormModel model)
    {
        if (!ModelState.IsValid)
        {
            return RedirectToAction(nameof(Execute), new { invoiceId });
        }

        try
        {
            // Get account name
            var invoice = await _invoiceService.GetByIdAsync(invoiceId);
            if (invoice == null)
            {
                return NotFound();
            }

            var account = await _coaService.GetAccountByIdAsync(invoice.CompanyId, model.PaymentAccountId);

            var dto = new PaymentScheduleDto
            {
                PaymentAccountId = model.PaymentAccountId,
                PaymentAccountName = account?.DisplayName ?? model.PaymentAccountName,
                Amount = model.Amount,
                PaymentMethod = model.PaymentMethod,
                ReferenceNumber = model.ReferenceNumber,
                ScheduledDate = model.ScheduledDate
            };

            var payment = await _paymentService.SchedulePaymentAsync(invoiceId, dto);

            // Execute immediately
            var userId = GetCurrentUserId();
            await _paymentService.ExecutePaymentAsync(payment.Id, userId);

            TempData["Success"] = "Payment executed successfully";
            return RedirectToAction("Details", new { id = payment.Id });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Payment execution failed for invoice {InvoiceId}", invoiceId);
            TempData["Error"] = $"Payment failed: {ex.Message}";
            return RedirectToAction(nameof(Execute), new { invoiceId });
        }
    }

    [HttpGet]
    public async Task<IActionResult> Details(Guid id)
    {
        var payment = await _paymentService.GetByIdAsync(id);
        if (payment == null)
        {
            return NotFound();
        }

        var model = new PaymentDetailsViewModel
        {
            Id = payment.Id,
            InvoiceId = payment.InvoiceId,
            InvoiceNumber = payment.Invoice.InvoiceNumber,
            VendorName = payment.Invoice.Vendor?.Name ?? "Unknown",
            PaymentAccountId = payment.PaymentAccountId,
            PaymentAccountName = payment.PaymentAccountName ?? "",
            Amount = payment.Amount,
            Currency = payment.Invoice.Currency,
            PaymentMethod = payment.PaymentMethod,
            ReferenceNumber = payment.ReferenceNumber,
            Status = payment.Status,
            ScheduledDate = payment.ScheduledDate,
            ExecutedAt = payment.ExecutedAt,
            ExecutedByName = payment.ExecutedBy?.FullName,
            ExternalRef = payment.ExternalRef,
            JournalEntryRef = payment.JournalEntryRef,
            FailureReason = payment.FailureReason,
            CreatedAt = payment.CreatedAt
        };

        // Get journal entry if completed
        if (payment.Status == PaymentStatus.Completed)
        {
            model.JournalEntry = await _paymentService.PreviewJournalEntryAsync(
                payment.InvoiceId, payment.PaymentAccountId);
        }

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(Guid id)
    {
        try
        {
            await _paymentService.CancelPaymentAsync(id);
            TempData["Success"] = "Payment cancelled";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Payment cancellation failed: {PaymentId}", id);
            TempData["Error"] = $"Cancellation failed: {ex.Message}";
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> JournalPreview(Guid invoiceId, string paymentAccountId)
    {
        try
        {
            var journal = await _paymentService.PreviewJournalEntryAsync(invoiceId, paymentAccountId);
            return Json(journal);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }
}
