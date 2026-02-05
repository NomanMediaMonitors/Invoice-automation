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
/// Company management controller
/// </summary>
[Authorize]
public class CompanyController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<User> _userManager;
    private readonly ICompanyService _companyService;
    private readonly IInvoiceService _invoiceService;
    private readonly ILogger<CompanyController> _logger;

    public CompanyController(
        ApplicationDbContext context,
        UserManager<User> userManager,
        ICompanyService companyService,
        IInvoiceService invoiceService,
        ILogger<CompanyController> logger)
    {
        _context = context;
        _userManager = userManager;
        _companyService = companyService;
        _invoiceService = invoiceService;
        _logger = logger;
    }

    private Guid GetCurrentUserId() => Guid.Parse(_userManager.GetUserId(User)!);

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var userId = GetCurrentUserId();

        var userCompanies = await _context.UserCompanies
            .Include(uc => uc.Company)
            .Where(uc => uc.UserId == userId && uc.IsActive)
            .ToListAsync();

        var companyStats = new Dictionary<Guid, (int InvoiceCount, int PendingCount)>();
        foreach (var uc in userCompanies)
        {
            var stats = await _invoiceService.GetStatisticsAsync(uc.CompanyId);
            companyStats[uc.CompanyId] = (stats.TotalCount, stats.PendingApprovalCount);
        }

        var model = new CompanyListViewModel
        {
            Companies = userCompanies.Select(uc => new CompanyListItemViewModel
            {
                Id = uc.Company.Id,
                Name = uc.Company.Name,
                Ntn = uc.Company.Ntn,
                City = uc.Company.City,
                UserRole = uc.Role,
                IsDefault = uc.IsDefault,
                AccountingConnected = uc.Company.AccountingProvider != AccountingProvider.None,
                AccountingProvider = uc.Company.AccountingProvider,
                InvoiceCount = companyStats.GetValueOrDefault(uc.CompanyId).InvoiceCount,
                PendingApprovalCount = companyStats.GetValueOrDefault(uc.CompanyId).PendingCount
            }).ToList()
        };

        return View(model);
    }

    [HttpGet]
    public IActionResult Create()
    {
        var model = new CompanyFormViewModel
        {
            FiscalYearStartMonth = 7,
            DefaultCurrency = "PKR",
            CurrencyOptions = GetCurrencyOptions(),
            MonthOptions = GetMonthOptions()
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CompanyFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            model.CurrencyOptions = GetCurrencyOptions();
            model.MonthOptions = GetMonthOptions();
            return View(model);
        }

        try
        {
            var userId = GetCurrentUserId();
            var dto = new CompanyCreateDto
            {
                Name = model.Name,
                Ntn = model.Ntn,
                Strn = model.Strn,
                Address = model.Address,
                City = model.City,
                State = model.State,
                Country = model.Country,
                PostalCode = model.PostalCode,
                Email = model.Email,
                Phone = model.Phone,
                Website = model.Website,
                FiscalYearStartMonth = model.FiscalYearStartMonth,
                DefaultCurrency = model.DefaultCurrency
            };

            var company = await _companyService.CreateAsync(dto, userId);

            // Set as current company
            HttpContext.Session.SetString("CurrentCompanyId", company.Id.ToString());

            TempData["Success"] = "Company created successfully";
            return RedirectToAction("Index", "Dashboard");
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError("", ex.Message);
            model.CurrencyOptions = GetCurrencyOptions();
            model.MonthOptions = GetMonthOptions();
            return View(model);
        }
    }

    [HttpGet]
    public async Task<IActionResult> Edit(Guid id)
    {
        var company = await _companyService.GetByIdAsync(id);
        if (company == null)
        {
            return NotFound();
        }

        var model = new CompanyFormViewModel
        {
            Id = company.Id,
            Name = company.Name,
            Ntn = company.Ntn,
            Strn = company.Strn,
            Address = company.Address,
            City = company.City,
            State = company.State,
            Country = company.Country,
            PostalCode = company.PostalCode,
            Email = company.Email,
            Phone = company.Phone,
            Website = company.Website,
            FiscalYearStartMonth = company.FiscalYearStartMonth,
            DefaultCurrency = company.DefaultCurrency,
            CurrencyOptions = GetCurrencyOptions(),
            MonthOptions = GetMonthOptions()
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(CompanyFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            model.CurrencyOptions = GetCurrencyOptions();
            model.MonthOptions = GetMonthOptions();
            return View(model);
        }

        try
        {
            var dto = new CompanyUpdateDto
            {
                Name = model.Name,
                Strn = model.Strn,
                Address = model.Address,
                City = model.City,
                State = model.State,
                Country = model.Country,
                PostalCode = model.PostalCode,
                Email = model.Email,
                Phone = model.Phone,
                Website = model.Website,
                FiscalYearStartMonth = model.FiscalYearStartMonth
            };

            await _companyService.UpdateAsync(model.Id!.Value, dto);
            TempData["Success"] = "Company updated successfully";
            return RedirectToAction(nameof(Details), new { id = model.Id });
        }
        catch (Exception ex)
        {
            ModelState.AddModelError("", ex.Message);
            model.CurrencyOptions = GetCurrencyOptions();
            model.MonthOptions = GetMonthOptions();
            return View(model);
        }
    }

    [HttpGet]
    public async Task<IActionResult> Details(Guid id)
    {
        var company = await _companyService.GetByIdAsync(id);
        if (company == null)
        {
            return NotFound();
        }

        var userId = GetCurrentUserId();
        var userCompany = await _context.UserCompanies
            .FirstOrDefaultAsync(uc => uc.UserId == userId && uc.CompanyId == id);

        if (userCompany == null)
        {
            return Forbid();
        }

        var stats = await _invoiceService.GetStatisticsAsync(id);
        var vendorCount = await _context.Vendors.CountAsync(v => v.CompanyId == id && v.IsActive);

        var model = new CompanyDetailsViewModel
        {
            Id = company.Id,
            Name = company.Name,
            Ntn = company.Ntn,
            Strn = company.Strn,
            Address = company.Address,
            City = company.City,
            State = company.State,
            Country = company.Country,
            Email = company.Email,
            Phone = company.Phone,
            Website = company.Website,
            LogoPath = company.LogoPath,
            FiscalYearStartMonth = company.FiscalYearStartMonth,
            DefaultCurrency = company.DefaultCurrency,
            IsActive = company.IsActive,
            CreatedAt = company.CreatedAt,
            AccountingConnected = company.AccountingProvider != AccountingProvider.None,
            AccountingProvider = company.AccountingProvider,
            Users = await _companyService.GetUsersAsync(id),
            TotalInvoices = stats.TotalCount,
            PendingInvoices = stats.PendingApprovalCount,
            TotalVendors = vendorCount,
            TotalAmount = stats.TotalAmount,
            CurrentUserRole = userCompany.Role
        };

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Users(Guid id)
    {
        var company = await _companyService.GetByIdAsync(id);
        if (company == null)
        {
            return NotFound();
        }

        var userId = GetCurrentUserId();
        var userCompany = await _context.UserCompanies
            .FirstOrDefaultAsync(uc => uc.UserId == userId && uc.CompanyId == id);

        if (userCompany == null || userCompany.Role < UserRole.Manager)
        {
            return Forbid();
        }

        var model = new CompanyUsersViewModel
        {
            CompanyId = company.Id,
            CompanyName = company.Name,
            Users = await _companyService.GetUsersAsync(id),
            CurrentUserRole = userCompany.Role,
            RoleOptions = Enum.GetValues<UserRole>()
                .Where(r => r <= userCompany.Role) // Can't assign higher roles
                .Select(r => new SelectListItem(r.ToString(), ((int)r).ToString()))
                .ToList()
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddUser(Guid companyId, AddUserFormModel model)
    {
        try
        {
            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                TempData["Error"] = "User not found. They must register first.";
                return RedirectToAction(nameof(Users), new { id = companyId });
            }

            await _companyService.AddUserAsync(companyId, user.Id, model.Role);
            TempData["Success"] = "User added successfully";
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction(nameof(Users), new { id = companyId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveUser(Guid companyId, Guid userId)
    {
        try
        {
            await _companyService.RemoveUserAsync(companyId, userId);
            TempData["Success"] = "User removed";
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction(nameof(Users), new { id = companyId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateUserRole(Guid companyId, Guid userId, UserRole role)
    {
        try
        {
            await _companyService.UpdateUserRoleAsync(companyId, userId, role);
            TempData["Success"] = "User role updated";
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction(nameof(Users), new { id = companyId });
    }

    private static List<SelectListItem> GetCurrencyOptions()
    {
        return new List<SelectListItem>
        {
            new("PKR - Pakistani Rupee", "PKR"),
            new("USD - US Dollar", "USD"),
            new("EUR - Euro", "EUR"),
            new("GBP - British Pound", "GBP"),
            new("AED - UAE Dirham", "AED"),
            new("SAR - Saudi Riyal", "SAR")
        };
    }

    private static List<SelectListItem> GetMonthOptions()
    {
        return Enumerable.Range(1, 12)
            .Select(m => new SelectListItem(
                new DateTime(2000, m, 1).ToString("MMMM"),
                m.ToString()))
            .ToList();
    }
}
