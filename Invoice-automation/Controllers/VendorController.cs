using InvoiceAutomation.Web.Core.DTOs;
using InvoiceAutomation.Web.Core.Entities;
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
/// Vendor management controller
/// </summary>
[Authorize]
public class VendorController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<User> _userManager;
    private readonly IVendorService _vendorService;
    private readonly IChartOfAccountsService _coaService;
    private readonly ILogger<VendorController> _logger;

    public VendorController(
        ApplicationDbContext context,
        UserManager<User> userManager,
        IVendorService vendorService,
        IChartOfAccountsService coaService,
        ILogger<VendorController> logger)
    {
        _context = context;
        _userManager = userManager;
        _vendorService = vendorService;
        _coaService = coaService;
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

    [HttpGet]
    public async Task<IActionResult> Index(bool showInactive = false)
    {
        var companyId = GetCurrentCompanyId();
        var company = await _context.Companies.FindAsync(companyId);
        var vendors = await _vendorService.GetByCompanyIdAsync(companyId, showInactive);

        // Get invoice stats per vendor
        var vendorStats = await _context.Invoices
            .Where(i => i.CompanyId == companyId && i.VendorId != null)
            .GroupBy(i => i.VendorId)
            .Select(g => new
            {
                VendorId = g.Key,
                InvoiceCount = g.Count(),
                TotalAmount = g.Sum(i => i.TotalAmount)
            })
            .ToDictionaryAsync(x => x.VendorId!.Value, x => (x.InvoiceCount, x.TotalAmount));

        // Get expense account names
        var expenseAccounts = await _coaService.GetExpenseAccountsAsync(companyId);
        var accountDict = expenseAccounts.ToDictionary(a => a.ExternalId);

        var model = new VendorListViewModel
        {
            CompanyId = companyId,
            CompanyName = company?.Name ?? "",
            ShowInactive = showInactive,
            Vendors = vendors.Select(v =>
            {
                vendorStats.TryGetValue(v.Id, out var stats);
                accountDict.TryGetValue(v.DefaultExpenseAccountId ?? "", out var account);

                return new VendorListItemViewModel
                {
                    Id = v.Id,
                    Name = v.Name,
                    Ntn = v.Ntn,
                    Email = v.Email,
                    Phone = v.Phone,
                    City = v.City,
                    DefaultExpenseAccountName = account?.DisplayName,
                    InvoiceCount = stats.InvoiceCount,
                    TotalInvoiceAmount = stats.TotalAmount,
                    IsActive = v.IsActive
                };
            }).ToList()
        };

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var companyId = GetCurrentCompanyId();
        var company = await _context.Companies.FindAsync(companyId);
        var expenseAccounts = await _coaService.GetExpenseAccountsAsync(companyId);

        var model = new VendorFormViewModel
        {
            CompanyId = companyId,
            CompanyName = company?.Name ?? "",
            PaymentTermsDays = 30,
            IsActive = true,
            ExpenseAccounts = new List<SelectListItem> { new("-- No Default Account --", "") }
                .Concat(expenseAccounts.Select(a => new SelectListItem(a.DisplayName, a.ExternalId)))
                .ToList()
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(VendorFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            var expenseAccounts = await _coaService.GetExpenseAccountsAsync(model.CompanyId);
            model.ExpenseAccounts = new List<SelectListItem> { new("-- No Default Account --", "") }
                .Concat(expenseAccounts.Select(a => new SelectListItem(a.DisplayName, a.ExternalId)))
                .ToList();
            return View(model);
        }

        try
        {
            var dto = new VendorCreateDto
            {
                Name = model.Name,
                Ntn = model.Ntn,
                Strn = model.Strn,
                Email = model.Email,
                Phone = model.Phone,
                Address = model.Address,
                City = model.City,
                ContactPerson = model.ContactPerson,
                DefaultExpenseAccountId = model.DefaultExpenseAccountId,
                PaymentTermsDays = model.PaymentTermsDays
            };

            var vendor = await _vendorService.CreateAsync(model.CompanyId, dto);
            TempData["Success"] = "Vendor created successfully";
            return RedirectToAction(nameof(Details), new { id = vendor.Id });
        }
        catch (Exception ex)
        {
            ModelState.AddModelError("", ex.Message);
            var expenseAccounts = await _coaService.GetExpenseAccountsAsync(model.CompanyId);
            model.ExpenseAccounts = new List<SelectListItem> { new("-- No Default Account --", "") }
                .Concat(expenseAccounts.Select(a => new SelectListItem(a.DisplayName, a.ExternalId)))
                .ToList();
            return View(model);
        }
    }

    [HttpGet]
    public async Task<IActionResult> Edit(Guid id)
    {
        var vendor = await _vendorService.GetByIdAsync(id);
        if (vendor == null)
        {
            return NotFound();
        }

        var expenseAccounts = await _coaService.GetExpenseAccountsAsync(vendor.CompanyId);

        var model = new VendorFormViewModel
        {
            Id = vendor.Id,
            CompanyId = vendor.CompanyId,
            CompanyName = vendor.Company.Name,
            Name = vendor.Name,
            Ntn = vendor.Ntn,
            Strn = vendor.Strn,
            Email = vendor.Email,
            Phone = vendor.Phone,
            Address = vendor.Address,
            City = vendor.City,
            ContactPerson = vendor.ContactPerson,
            DefaultExpenseAccountId = vendor.DefaultExpenseAccountId,
            PaymentTermsDays = vendor.PaymentTermsDays,
            IsActive = vendor.IsActive,
            ExpenseAccounts = new List<SelectListItem> { new("-- No Default Account --", "") }
                .Concat(expenseAccounts.Select(a => new SelectListItem(a.DisplayName, a.ExternalId)))
                .ToList()
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(VendorFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            var expenseAccounts = await _coaService.GetExpenseAccountsAsync(model.CompanyId);
            model.ExpenseAccounts = new List<SelectListItem> { new("-- No Default Account --", "") }
                .Concat(expenseAccounts.Select(a => new SelectListItem(a.DisplayName, a.ExternalId)))
                .ToList();
            return View(model);
        }

        try
        {
            var dto = new VendorUpdateDto
            {
                Name = model.Name,
                Ntn = model.Ntn,
                Strn = model.Strn,
                Email = model.Email,
                Phone = model.Phone,
                Address = model.Address,
                City = model.City,
                ContactPerson = model.ContactPerson,
                DefaultExpenseAccountId = model.DefaultExpenseAccountId,
                PaymentTermsDays = model.PaymentTermsDays,
                IsActive = model.IsActive
            };

            await _vendorService.UpdateAsync(model.Id!.Value, dto);
            TempData["Success"] = "Vendor updated successfully";
            return RedirectToAction(nameof(Details), new { id = model.Id });
        }
        catch (Exception ex)
        {
            ModelState.AddModelError("", ex.Message);
            var expenseAccounts = await _coaService.GetExpenseAccountsAsync(model.CompanyId);
            model.ExpenseAccounts = new List<SelectListItem> { new("-- No Default Account --", "") }
                .Concat(expenseAccounts.Select(a => new SelectListItem(a.DisplayName, a.ExternalId)))
                .ToList();
            return View(model);
        }
    }

    [HttpGet]
    public async Task<IActionResult> Details(Guid id)
    {
        var vendor = await _vendorService.GetByIdAsync(id);
        if (vendor == null)
        {
            return NotFound();
        }

        // Get expense account name
        AccountDto? expenseAccount = null;
        if (!string.IsNullOrEmpty(vendor.DefaultExpenseAccountId))
        {
            expenseAccount = await _coaService.GetAccountByIdAsync(vendor.CompanyId, vendor.DefaultExpenseAccountId);
        }

        // Get invoice stats
        var invoiceStats = await _context.Invoices
            .Where(i => i.VendorId == id)
            .GroupBy(i => 1)
            .Select(g => new
            {
                Total = g.Count(),
                Pending = g.Count(i => i.Status != Core.Enums.InvoiceStatus.Completed),
                TotalAmount = g.Sum(i => i.TotalAmount),
                PaidAmount = g.Where(i => i.Status == Core.Enums.InvoiceStatus.Completed).Sum(i => i.TotalAmount)
            })
            .FirstOrDefaultAsync();

        // Get recent invoices
        var recentInvoices = await _context.Invoices
            .Include(i => i.UploadedBy)
            .Where(i => i.VendorId == id)
            .OrderByDescending(i => i.CreatedAt)
            .Take(5)
            .ToListAsync();

        var model = new VendorDetailsViewModel
        {
            Id = vendor.Id,
            CompanyId = vendor.CompanyId,
            CompanyName = vendor.Company.Name,
            Name = vendor.Name,
            Ntn = vendor.Ntn,
            Strn = vendor.Strn,
            Email = vendor.Email,
            Phone = vendor.Phone,
            Address = vendor.Address,
            City = vendor.City,
            ContactPerson = vendor.ContactPerson,
            DefaultExpenseAccountId = vendor.DefaultExpenseAccountId,
            DefaultExpenseAccountName = expenseAccount?.DisplayName,
            PaymentTermsDays = vendor.PaymentTermsDays,
            IsActive = vendor.IsActive,
            CreatedAt = vendor.CreatedAt,
            UpdatedAt = vendor.UpdatedAt,
            TotalInvoices = invoiceStats?.Total ?? 0,
            PendingInvoices = invoiceStats?.Pending ?? 0,
            TotalAmount = invoiceStats?.TotalAmount ?? 0,
            PaidAmount = invoiceStats?.PaidAmount ?? 0,
            RecentInvoices = recentInvoices.Select(i => new InvoiceListItemViewModel
            {
                Id = i.Id,
                InvoiceNumber = i.InvoiceNumber,
                InvoiceDate = i.InvoiceDate,
                TotalAmount = i.TotalAmount,
                Status = i.Status,
                CreatedAt = i.CreatedAt,
                UploadedByName = i.UploadedBy.FullName
            }).ToList()
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id)
    {
        try
        {
            await _vendorService.DeleteAsync(id);
            TempData["Success"] = "Vendor deactivated";
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Search(string term)
    {
        var companyId = GetCurrentCompanyId();
        var vendors = await _vendorService.SearchAsync(companyId, term);

        return Json(vendors.Select(v => new
        {
            v.Id,
            v.Name,
            v.Ntn,
            v.DefaultExpenseAccountId
        }));
    }
}
