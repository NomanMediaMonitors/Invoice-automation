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
using CoreMatchType = InvoiceAutomation.Web.Core.Enums.MatchType;

namespace InvoiceAutomation.Web.Controllers;

/// <summary>
/// Invoice management controller
/// </summary>
[Authorize]
public class InvoiceController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<User> _userManager;
    private readonly IInvoiceService _invoiceService;
    private readonly IVendorService _vendorService;
    private readonly IChartOfAccountsService _coaService;
    private readonly IApprovalService _approvalService;
    private readonly IFileStorageService _fileStorage;
    private readonly ILogger<InvoiceController> _logger;

    public InvoiceController(
        ApplicationDbContext context,
        UserManager<User> userManager,
        IInvoiceService invoiceService,
        IVendorService vendorService,
        IChartOfAccountsService coaService,
        IApprovalService approvalService,
        IFileStorageService fileStorage,
        ILogger<InvoiceController> logger)
    {
        _context = context;
        _userManager = userManager;
        _invoiceService = invoiceService;
        _vendorService = vendorService;
        _coaService = coaService;
        _approvalService = approvalService;
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
    public async Task<IActionResult> Index(InvoiceFilterDto? filter = null)
    {
        var companyId = GetCurrentCompanyId();

        filter ??= new InvoiceFilterDto();
        filter.CompanyId = companyId;

        var result = await _invoiceService.GetInvoicesAsync(filter);
        var company = await _context.Companies.FindAsync(companyId);
        var vendors = await _vendorService.GetByCompanyIdAsync(companyId);

        var model = new InvoiceListViewModel
        {
            CompanyId = companyId,
            CompanyName = company?.Name ?? "",
            Filter = filter,
            Statistics = await _invoiceService.GetStatisticsAsync(companyId),
            Invoices = new PagedResult<InvoiceListItemViewModel>
            {
                Items = result.Items.Select(i => new InvoiceListItemViewModel
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
                    UploadedByName = i.UploadedBy.FullName,
                    MatchType = i.Items.FirstOrDefault()?.MatchType ?? CoreMatchType.Manual
                }).ToList(),
                TotalCount = result.TotalCount,
                Page = result.Page,
                PageSize = result.PageSize
            },
            Vendors = vendors.Select(v => new SelectListItem(v.Name, v.Id.ToString())).ToList(),
            StatusOptions = Enum.GetValues<InvoiceStatus>()
                .Select(s => new SelectListItem(s.ToString(), ((int)s).ToString()))
                .ToList()
        };

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Upload()
    {
        var companyId = GetCurrentCompanyId();
        var company = await _context.Companies.FindAsync(companyId);
        var vendors = await _vendorService.GetByCompanyIdAsync(companyId);

        var model = new InvoiceUploadViewModel
        {
            CompanyId = companyId,
            Companies = new List<SelectListItem>
            {
                new(company?.Name ?? "", companyId.ToString())
            },
            Vendors = vendors.Select(v => new SelectListItem(v.Name, v.Id.ToString())).ToList()
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Upload(InvoiceUploadViewModel model)
    {
        if (!ModelState.IsValid)
        {
            var vendors = await _vendorService.GetByCompanyIdAsync(model.CompanyId);
            model.Vendors = vendors.Select(v => new SelectListItem(v.Name, v.Id.ToString())).ToList();
            return View(model);
        }

        try
        {
            var userId = GetCurrentUserId();
            var invoice = await _invoiceService.CreateFromUploadAsync(model.CompanyId, userId, model.File, model.VendorId);

            TempData["Success"] = "Invoice uploaded successfully. Please review the extracted data.";

            if (invoice.OcrConfidence.HasValue && invoice.OcrConfidence < 85)
            {
                TempData["Warning"] = $"OCR confidence is {invoice.OcrConfidence:F1}%. Please verify all fields carefully.";
            }

            return RedirectToAction(nameof(Edit), new { id = invoice.Id });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Invoice upload failed");
            var vendors = await _vendorService.GetByCompanyIdAsync(model.CompanyId);
            model.Vendors = vendors.Select(v => new SelectListItem(v.Name, v.Id.ToString())).ToList();
            ModelState.AddModelError("", $"Upload failed: {ex.Message}");
            return View(model);
        }
    }

    [HttpGet]
    public async Task<IActionResult> Edit(Guid id)
    {
        var invoice = await _invoiceService.GetByIdAsync(id);
        if (invoice == null)
        {
            return NotFound();
        }

        var companyId = invoice.CompanyId;
        var vendors = await _vendorService.GetByCompanyIdAsync(companyId);
        var expenseAccounts = await _coaService.GetExpenseAccountsAsync(companyId);
        var approvalHistory = await _approvalService.GetApprovalHistoryAsync(id);

        var model = new InvoiceEditViewModel
        {
            Id = invoice.Id,
            CompanyId = companyId,
            CompanyName = invoice.Company.Name,
            InvoiceFileUrl = _fileStorage.GetFileUrl(invoice.OriginalFilePath),
            IsPdf = invoice.ContentType.Contains("pdf", StringComparison.OrdinalIgnoreCase),
            OriginalFileName = invoice.OriginalFileName,
            OcrConfidence = invoice.OcrConfidence,
            Status = invoice.Status,
            CanEdit = _invoiceService.CanEdit(invoice),
            CanSubmit = invoice.Status == InvoiceStatus.Draft || invoice.Status == InvoiceStatus.RejectedByManager,
            CanDelete = _invoiceService.CanDelete(invoice),
            Invoice = new InvoiceFormModel
            {
                Id = invoice.Id,
                VendorId = invoice.VendorId,
                InvoiceNumber = invoice.InvoiceNumber,
                InvoiceDate = invoice.InvoiceDate,
                DueDate = invoice.DueDate,
                SubTotal = invoice.Subtotal,
                TaxAmount = invoice.TaxAmount,
                TotalAmount = invoice.TotalAmount,
                Currency = invoice.Currency,
                Notes = invoice.Notes,
                Items = invoice.Items.OrderBy(i => i.LineNumber).Select(i => new InvoiceItemFormModel
                {
                    Id = i.Id,
                    ExpenseAccountId = i.ExpenseAccountId,
                    Description = i.Description,
                    Quantity = i.Quantity,
                    Unit = i.Unit,
                    UnitPrice = i.UnitPrice,
                    TaxAmount = i.TaxAmount,
                    Amount = i.Amount,
                    LineNumber = i.LineNumber
                }).ToList()
            },
            Vendors = new List<SelectListItem> { new("-- Select Vendor --", "") }
                .Concat(vendors.Select(v => new SelectListItem(v.Name, v.Id.ToString())))
                .ToList(),
            ExpenseAccounts = new List<SelectListItem> { new("-- Select Account --", "") }
                .Concat(expenseAccounts.Select(a => new SelectListItem(a.DisplayName, a.ExternalId)))
                .ToList(),
            ApprovalHistory = approvalHistory.Select(a => new ApprovalHistoryDto
            {
                Id = a.Id,
                Level = a.ApprovalLevel,
                Status = a.Status,
                ApproverName = a.Approver?.FullName,
                ApproverEmail = a.Approver?.Email,
                Comments = a.Comments,
                DecidedAt = a.DecidedAt,
                CreatedAt = a.CreatedAt
            }).ToList(),
            MatchType = invoice.Items.FirstOrDefault()?.MatchType ?? CoreMatchType.Manual,
            VendorNtn = invoice.Vendor?.Ntn
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(InvoiceFormModel model)
    {
        if (!ModelState.IsValid)
        {
            return RedirectToAction(nameof(Edit), new { id = model.Id });
        }

        try
        {
            var updateDto = new InvoiceUpdateDto
            {
                VendorId = model.VendorId,
                InvoiceNumber = model.InvoiceNumber,
                InvoiceDate = model.InvoiceDate,
                DueDate = model.DueDate,
                Subtotal = model.Subtotal,
                TaxAmount = model.TaxAmount,
                TotalAmount = model.TotalAmount,
                Notes = model.Notes,
                Items = model.Items?.Select(i => new InvoiceItemDto
                {
                    Id = i.Id,
                    ExpenseAccountId = i.ExpenseAccountId,
                    Description = i.Description,
                    Quantity = i.Quantity,
                    Unit = i.Unit,
                    UnitPrice = i.UnitPrice,
                    TaxAmount = i.TaxAmount,
                    Amount = i.Amount,
                    LineNumber = i.LineNumber,
                    MatchType = CoreMatchType.Manual
                }).ToList()
            };

            await _invoiceService.UpdateAsync(model.Id, updateDto);
            TempData["Success"] = "Invoice updated successfully";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Invoice update failed");
            TempData["Error"] = $"Update failed: {ex.Message}";
        }

        return RedirectToAction(nameof(Edit), new { id = model.Id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Submit(Guid id)
    {
        try
        {
            var userId = GetCurrentUserId();
            await _invoiceService.SubmitForApprovalAsync(id, userId);
            TempData["Success"] = "Invoice submitted for approval";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Invoice submission failed");
            TempData["Error"] = $"Submission failed: {ex.Message}";
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id)
    {
        try
        {
            await _invoiceService.DeleteAsync(id);
            TempData["Success"] = "Invoice deleted successfully";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Invoice deletion failed");
            TempData["Error"] = $"Delete failed: {ex.Message}";
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Review(Guid id)
    {
        var invoice = await _invoiceService.GetByIdAsync(id);
        if (invoice == null)
        {
            return NotFound();
        }

        var userId = GetCurrentUserId();
        var canApprove = await _approvalService.CanApproveAsync(id, userId);
        var expenseAccounts = await _coaService.GetExpenseAccountsAsync(invoice.CompanyId);
        var accountDict = expenseAccounts.ToDictionary(a => a.ExternalId);
        var approvalHistory = await _approvalService.GetApprovalHistoryAsync(id);

        var model = new InvoiceReviewViewModel
        {
            Id = invoice.Id,
            InvoiceNumber = invoice.InvoiceNumber,
            InvoiceDate = invoice.InvoiceDate,
            DueDate = invoice.DueDate,
            VendorName = invoice.Vendor?.Name ?? "Unknown",
            VendorNtn = invoice.Vendor?.Ntn,
            SubTotal = invoice.Subtotal,
            TaxAmount = invoice.TaxAmount,
            TotalAmount = invoice.TotalAmount,
            Currency = invoice.Currency,
            Status = invoice.Status,
            OcrConfidence = invoice.OcrConfidence,
            Notes = invoice.Notes,
            InvoiceFileUrl = _fileStorage.GetFileUrl(invoice.OriginalFilePath),
            IsPdf = invoice.ContentType.Contains("pdf", StringComparison.OrdinalIgnoreCase),
            CanApprove = canApprove,
            RequiredLevel = _approvalService.GetRequiredApprovalLevel(invoice),
            CompanyName = invoice.Company.Name,
            UploadedByName = invoice.UploadedBy.FullName,
            UploadedAt = invoice.CreatedAt,
            Items = invoice.Items.OrderBy(i => i.LineNumber).Select(i =>
            {
                accountDict.TryGetValue(i.ExpenseAccountId ?? "", out var account);
                return new InvoiceItemReviewModel
                {
                    Description = i.Description,
                    Quantity = i.Quantity,
                    Unit = i.Unit,
                    UnitPrice = i.UnitPrice,
                    TaxAmount = i.TaxAmount,
                    Amount = i.Amount,
                    ExpenseAccountName = account?.Name,
                    ExpenseAccountCode = account?.Code,
                    MatchType = i.MatchType
                };
            }).ToList(),
            ApprovalHistory = approvalHistory.Select(a => new ApprovalHistoryDto
            {
                Id = a.Id,
                Level = a.ApprovalLevel,
                Status = a.Status,
                ApproverName = a.Approver?.FullName,
                ApproverEmail = a.Approver?.Email,
                Comments = a.Comments,
                DecidedAt = a.DecidedAt,
                CreatedAt = a.CreatedAt
            }).ToList()
        };

        return View(model);
    }
}
