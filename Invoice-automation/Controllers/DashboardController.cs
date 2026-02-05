using InvoiceAutomation.Web.Core.Entities;
using InvoiceAutomation.Web.Core.Interfaces;
using InvoiceAutomation.Web.Infrastructure.Data;
using InvoiceAutomation.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InvoiceAutomation.Web.Controllers;

/// <summary>
/// Dashboard controller
/// </summary>
[Authorize]
public class DashboardController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<User> _userManager;
    private readonly IInvoiceService _invoiceService;
    private readonly IPaymentService _paymentService;
    private readonly IApprovalService _approvalService;

    public DashboardController(
        ApplicationDbContext context,
        UserManager<User> userManager,
        IInvoiceService invoiceService,
        IPaymentService paymentService,
        IApprovalService approvalService)
    {
        _context = context;
        _userManager = userManager;
        _invoiceService = invoiceService;
        _paymentService = paymentService;
        _approvalService = approvalService;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var userId = Guid.Parse(_userManager.GetUserId(User)!);

        // Get user's companies
        var userCompanies = await _context.UserCompanies
            .Include(uc => uc.Company)
            .Where(uc => uc.UserId == userId && uc.IsActive && uc.Company.IsActive)
            .OrderByDescending(uc => uc.IsDefault)
            .ThenBy(uc => uc.Company.Name)
            .ToListAsync();

        if (!userCompanies.Any())
        {
            // Redirect to create company if no companies
            return RedirectToAction("Create", "Company");
        }

        // Get current company from session or use default
        var currentCompanyId = HttpContext.Session.GetString("CurrentCompanyId");
        Guid companyId;

        if (!string.IsNullOrEmpty(currentCompanyId) && Guid.TryParse(currentCompanyId, out var parsedId))
        {
            companyId = userCompanies.Any(uc => uc.CompanyId == parsedId)
                ? parsedId
                : userCompanies.First().CompanyId;
        }
        else
        {
            companyId = userCompanies.First().CompanyId;
        }

        // Save to session
        HttpContext.Session.SetString("CurrentCompanyId", companyId.ToString());

        var currentUserCompany = userCompanies.First(uc => uc.CompanyId == companyId);

        // Build dashboard
        var model = new DashboardViewModel
        {
            CompanyId = companyId,
            CompanyName = currentUserCompany.Company.Name,
            UserRole = currentUserCompany.Role,
            InvoiceStatistics = await _invoiceService.GetStatisticsAsync(companyId),
            PaymentStatistics = await _paymentService.GetStatisticsAsync(companyId),
            AccountingConnected = currentUserCompany.Company.AccountingProvider != Core.Enums.AccountingProvider.None,
            AccountingProvider = currentUserCompany.Company.AccountingProvider
        };

        // Get recent invoices
        var recentInvoices = await _context.Invoices
            .Include(i => i.Vendor)
            .Include(i => i.UploadedBy)
            .Where(i => i.CompanyId == companyId)
            .OrderByDescending(i => i.CreatedAt)
            .Take(5)
            .ToListAsync();

        model.RecentInvoices = recentInvoices.Select(i => new InvoiceListItemViewModel
        {
            Id = i.Id,
            InvoiceNumber = i.InvoiceNumber,
            InvoiceDate = i.InvoiceDate,
            DueDate = i.DueDate,
            VendorName = i.Vendor?.Name ?? "Unknown",
            TotalAmount = i.TotalAmount,
            Currency = i.Currency,
            Status = i.Status,
            OcrConfidence = i.OcrConfidence,
            CreatedAt = i.CreatedAt,
            UploadedByName = i.UploadedBy.FullName
        }).ToList();

        // Get pending approvals if manager or above
        if (currentUserCompany.Role >= Core.Enums.UserRole.Manager)
        {
            var pendingApprovals = await _invoiceService.GetPendingApprovalAsync(
                companyId, userId, currentUserCompany.Role);

            model.PendingApproval = pendingApprovals.Select(i => new InvoiceListItemViewModel
            {
                Id = i.Id,
                InvoiceNumber = i.InvoiceNumber,
                InvoiceDate = i.InvoiceDate,
                VendorName = i.Vendor?.Name ?? "Unknown",
                TotalAmount = i.TotalAmount,
                Status = i.Status,
                CreatedAt = i.CreatedAt
            }).ToList();

            model.PendingApprovalCount = await _approvalService.GetPendingApprovalCountAsync(
                companyId, userId, currentUserCompany.Role);
        }

        // Get pending payments
        if (currentUserCompany.Role >= Core.Enums.UserRole.Admin)
        {
            var pendingPayments = await _paymentService.GetPendingPaymentsAsync(companyId);

            model.PendingPayments = pendingPayments.Select(p => new PaymentListItemViewModel
            {
                Id = p.Id,
                InvoiceId = p.InvoiceId,
                InvoiceNumber = p.Invoice.InvoiceNumber,
                VendorName = p.Invoice.Vendor?.Name ?? "Unknown",
                PaymentAccountName = p.PaymentAccountName ?? "",
                Amount = p.Amount,
                Status = p.Status,
                ScheduledDate = p.ScheduledDate
            }).ToList();
        }

        // Store company selector data
        ViewBag.CompanySelector = new CompanySelectorViewModel
        {
            CurrentCompanyId = companyId,
            CurrentCompanyName = currentUserCompany.Company.Name,
            Companies = userCompanies.Select(uc => new CompanySelectorItem
            {
                Id = uc.CompanyId,
                Name = uc.Company.Name,
                IsDefault = uc.IsDefault,
                Role = uc.Role
            }).ToList()
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult SwitchCompany(Guid companyId)
    {
        HttpContext.Session.SetString("CurrentCompanyId", companyId.ToString());
        return RedirectToAction(nameof(Index));
    }
}
