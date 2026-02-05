using System.Text.Json;
using InvoiceAutomation.Web.Core.DTOs;
using InvoiceAutomation.Web.Core.Entities;
using InvoiceAutomation.Web.Core.Enums;
using InvoiceAutomation.Web.Core.Interfaces;
using InvoiceAutomation.Web.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace InvoiceAutomation.Web.Core.Services;

/// <summary>
/// Invoice business service
/// </summary>
public class InvoiceService : IInvoiceService
{
    private readonly ApplicationDbContext _context;
    private readonly IFileStorageService _fileStorage;
    private readonly IOcrService _ocrService;
    private readonly IVendorService _vendorService;
    private readonly ILogger<InvoiceService> _logger;

    public InvoiceService(
        ApplicationDbContext context,
        IFileStorageService fileStorage,
        IOcrService ocrService,
        IVendorService vendorService,
        ILogger<InvoiceService> logger)
    {
        _context = context;
        _fileStorage = fileStorage;
        _ocrService = ocrService;
        _vendorService = vendorService;
        _logger = logger;
    }

    public async Task<Invoice> CreateFromUploadAsync(Guid companyId, Guid userId, IFormFile file, Guid? vendorId = null)
    {
        _logger.LogInformation("Creating invoice from upload for company {CompanyId}", companyId);

        // 1. Save file to local storage
        var filePath = await _fileStorage.SaveInvoiceFileAsync(companyId, file);
        var physicalPath = _fileStorage.GetPhysicalPath(filePath);

        // 2. Run OCR
        InvoiceOcrResult? ocrResult = null;
        try
        {
            ocrResult = await _ocrService.ExtractInvoiceDataAsync(physicalPath);
            _logger.LogInformation("OCR completed with confidence: {Confidence}%", ocrResult.Confidence);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "OCR failed, creating invoice without extracted data");
        }

        // 3. Try to match vendor - use provided vendorId, or fallback to OCR NTN match
        Guid? matchedVendorId = vendorId;
        if (!matchedVendorId.HasValue && !string.IsNullOrEmpty(ocrResult?.VendorNtn))
        {
            var vendor = await _vendorService.FindByNtnAsync(companyId, ocrResult.VendorNtn);
            matchedVendorId = vendor?.Id;
        }

        // 4. Create invoice
        var invoice = new Invoice
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            VendorId = matchedVendorId,
            UploadedById = userId,
            InvoiceNumber = ocrResult?.InvoiceNumber ?? "PENDING",
            InvoiceDate = ocrResult?.InvoiceDate ?? DateTime.Today,
            DueDate = ocrResult?.DueDate,
            Subtotal = ocrResult?.Subtotal ?? 0,
            TaxAmount = ocrResult?.TaxAmount ?? 0,
            TotalAmount = ocrResult?.TotalAmount ?? 0,
            Currency = ocrResult?.Currency ?? "PKR",
            Status = InvoiceStatus.Draft,
            OriginalFilePath = filePath,
            OriginalFileName = file.FileName,
            ContentType = file.ContentType,
            FileSize = file.Length,
            OcrData = ocrResult != null ? JsonSerializer.Serialize(ocrResult) : null,
            OcrConfidence = ocrResult?.Confidence,
            CreatedAt = DateTime.UtcNow
        };

        // 5. Add line items from OCR
        if (ocrResult?.LineItems?.Any() == true)
        {
            var lineNumber = 1;
            foreach (var item in ocrResult.LineItems)
            {
                var invoiceItem = new InvoiceItem
                {
                    Id = Guid.NewGuid(),
                    InvoiceId = invoice.Id,
                    Description = item.Description,
                    Quantity = item.Quantity ?? 1,
                    UnitPrice = item.UnitPrice ?? item.Amount ?? 0,
                    Amount = item.Amount ?? 0,
                    LineNumber = lineNumber++,
                    MatchType = MatchType.Manual
                };

                // If vendor has default expense account, use it
                if (!string.IsNullOrEmpty(vendor?.DefaultExpenseAccountId))
                {
                    invoiceItem.ExpenseAccountId = vendor.DefaultExpenseAccountId;
                    invoiceItem.MatchType = MatchType.VendorDefault;
                }

                invoice.Items.Add(invoiceItem);
            }
        }
        else
        {
            // Add at least one line item for the total
            invoice.Items.Add(new InvoiceItem
            {
                Id = Guid.NewGuid(),
                InvoiceId = invoice.Id,
                Description = "Invoice Total",
                Quantity = 1,
                UnitPrice = invoice.TotalAmount,
                Amount = invoice.TotalAmount,
                LineNumber = 1,
                MatchType = MatchType.Manual,
                ExpenseAccountId = vendor?.DefaultExpenseAccountId
            });
        }

        _context.Invoices.Add(invoice);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Invoice created: {InvoiceId}", invoice.Id);
        return invoice;
    }

    public async Task<Invoice?> GetByIdAsync(Guid id, bool includeRelated = true)
    {
        var query = _context.Invoices.AsQueryable();

        if (includeRelated)
        {
            query = query
                .Include(i => i.Company)
                .Include(i => i.Vendor)
                .Include(i => i.UploadedBy)
                .Include(i => i.Items)
                .Include(i => i.Approvals)
                    .ThenInclude(a => a.Approver)
                .Include(i => i.Payments);
        }

        return await query.FirstOrDefaultAsync(i => i.Id == id);
    }

    public async Task<PagedResult<Invoice>> GetInvoicesAsync(InvoiceFilterDto filter)
    {
        var query = _context.Invoices
            .Include(i => i.Vendor)
            .Include(i => i.UploadedBy)
            .Where(i => i.CompanyId == filter.CompanyId);

        // Apply filters
        if (filter.VendorId.HasValue)
            query = query.Where(i => i.VendorId == filter.VendorId);

        if (filter.Status.HasValue)
            query = query.Where(i => i.Status == filter.Status);

        if (filter.DateFrom.HasValue)
            query = query.Where(i => i.InvoiceDate >= filter.DateFrom);

        if (filter.DateTo.HasValue)
            query = query.Where(i => i.InvoiceDate <= filter.DateTo);

        if (filter.AmountMin.HasValue)
            query = query.Where(i => i.TotalAmount >= filter.AmountMin);

        if (filter.AmountMax.HasValue)
            query = query.Where(i => i.TotalAmount <= filter.AmountMax);

        if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
        {
            var term = filter.SearchTerm.ToLower();
            query = query.Where(i =>
                i.InvoiceNumber.ToLower().Contains(term) ||
                (i.Vendor != null && i.Vendor.Name.ToLower().Contains(term)));
        }

        // Get total count
        var totalCount = await query.CountAsync();

        // Apply sorting
        query = filter.SortBy?.ToLower() switch
        {
            "invoicenumber" => filter.SortDescending
                ? query.OrderByDescending(i => i.InvoiceNumber)
                : query.OrderBy(i => i.InvoiceNumber),
            "invoicedate" => filter.SortDescending
                ? query.OrderByDescending(i => i.InvoiceDate)
                : query.OrderBy(i => i.InvoiceDate),
            "totalamount" => filter.SortDescending
                ? query.OrderByDescending(i => i.TotalAmount)
                : query.OrderBy(i => i.TotalAmount),
            "status" => filter.SortDescending
                ? query.OrderByDescending(i => i.Status)
                : query.OrderBy(i => i.Status),
            _ => filter.SortDescending
                ? query.OrderByDescending(i => i.CreatedAt)
                : query.OrderBy(i => i.CreatedAt)
        };

        // Apply pagination
        var items = await query
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync();

        return new PagedResult<Invoice>
        {
            Items = items,
            TotalCount = totalCount,
            Page = filter.Page,
            PageSize = filter.PageSize
        };
    }

    public async Task<Invoice> UpdateAsync(Guid id, InvoiceUpdateDto dto)
    {
        var invoice = await _context.Invoices
            .Include(i => i.Items)
            .FirstOrDefaultAsync(i => i.Id == id)
            ?? throw new KeyNotFoundException($"Invoice not found: {id}");

        if (!CanEdit(invoice))
            throw new InvalidOperationException("Invoice cannot be edited in its current status");

        // Update fields
        if (dto.VendorId.HasValue)
            invoice.VendorId = dto.VendorId;

        if (!string.IsNullOrEmpty(dto.InvoiceNumber))
            invoice.InvoiceNumber = dto.InvoiceNumber;

        if (dto.InvoiceDate.HasValue)
            invoice.InvoiceDate = dto.InvoiceDate.Value;

        if (dto.DueDate.HasValue)
            invoice.DueDate = dto.DueDate;

        if (dto.Subtotal.HasValue)
            invoice.Subtotal = dto.Subtotal.Value;

        if (dto.TaxAmount.HasValue)
            invoice.TaxAmount = dto.TaxAmount.Value;

        if (dto.TotalAmount.HasValue)
            invoice.TotalAmount = dto.TotalAmount.Value;

        if (dto.Notes != null)
            invoice.Notes = dto.Notes;

        invoice.UpdatedAt = DateTime.UtcNow;

        // Update line items if provided
        if (dto.Items != null)
        {
            await UpdateLineItemsAsync(id, dto.Items);
        }

        await _context.SaveChangesAsync();
        return invoice;
    }

    public async Task UpdateLineItemsAsync(Guid invoiceId, List<InvoiceItemDto> items)
    {
        var invoice = await _context.Invoices
            .Include(i => i.Items)
            .FirstOrDefaultAsync(i => i.Id == invoiceId)
            ?? throw new KeyNotFoundException($"Invoice not found: {invoiceId}");

        // Remove existing items not in the update
        var itemIdsToKeep = items.Where(i => i.Id.HasValue).Select(i => i.Id!.Value).ToHashSet();
        var itemsToRemove = invoice.Items.Where(i => !itemIdsToKeep.Contains(i.Id)).ToList();
        foreach (var item in itemsToRemove)
        {
            _context.InvoiceItems.Remove(item);
        }

        // Update or add items
        var lineNumber = 1;
        foreach (var dto in items)
        {
            InvoiceItem item;

            if (dto.Id.HasValue)
            {
                item = invoice.Items.FirstOrDefault(i => i.Id == dto.Id)
                    ?? throw new KeyNotFoundException($"Invoice item not found: {dto.Id}");
            }
            else
            {
                item = new InvoiceItem
                {
                    Id = Guid.NewGuid(),
                    InvoiceId = invoiceId
                };
                _context.InvoiceItems.Add(item);
            }

            item.ExpenseAccountId = dto.ExpenseAccountId;
            item.Description = dto.Description;
            item.Quantity = dto.Quantity;
            item.Unit = dto.Unit;
            item.UnitPrice = dto.UnitPrice;
            item.TaxAmount = dto.TaxAmount;
            item.Amount = dto.Amount;
            item.LineNumber = lineNumber++;
            item.MatchType = dto.MatchType;
        }

        // Recalculate totals
        invoice.Subtotal = invoice.Items.Sum(i => i.Amount - i.TaxAmount);
        invoice.TaxAmount = invoice.Items.Sum(i => i.TaxAmount);
        invoice.TotalAmount = invoice.Items.Sum(i => i.Amount);
        invoice.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
    }

    public async Task<Invoice> SubmitForApprovalAsync(Guid id, Guid userId)
    {
        var invoice = await GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Invoice not found: {id}");

        if (invoice.Status != InvoiceStatus.Draft && invoice.Status != InvoiceStatus.RejectedByManager)
            throw new InvalidOperationException("Only draft or rejected invoices can be submitted for approval");

        // Validate invoice is complete
        if (string.IsNullOrEmpty(invoice.InvoiceNumber) || invoice.InvoiceNumber == "PENDING")
            throw new InvalidOperationException("Invoice number is required");

        if (invoice.TotalAmount <= 0)
            throw new InvalidOperationException("Invoice total must be greater than zero");

        if (!invoice.Items.Any())
            throw new InvalidOperationException("Invoice must have at least one line item");

        if (invoice.Items.Any(i => string.IsNullOrEmpty(i.ExpenseAccountId)))
            throw new InvalidOperationException("All line items must have an expense account");

        // Create manager approval record
        var approval = new InvoiceApproval
        {
            Id = Guid.NewGuid(),
            InvoiceId = id,
            ApprovalLevel = ApprovalLevel.Manager,
            Status = ApprovalStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };

        invoice.Status = InvoiceStatus.PendingManagerReview;
        invoice.UpdatedAt = DateTime.UtcNow;
        invoice.Approvals.Add(approval);

        await _context.SaveChangesAsync();

        _logger.LogInformation("Invoice {InvoiceId} submitted for approval", id);
        return invoice;
    }

    public async Task DeleteAsync(Guid id)
    {
        var invoice = await GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Invoice not found: {id}");

        if (!CanDelete(invoice))
            throw new InvalidOperationException("Invoice cannot be deleted in its current status");

        // Delete file
        if (!string.IsNullOrEmpty(invoice.OriginalFilePath))
        {
            await _fileStorage.DeleteFileAsync(invoice.OriginalFilePath);
        }

        _context.Invoices.Remove(invoice);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Invoice deleted: {InvoiceId}", id);
    }

    public async Task<InvoiceStatisticsDto> GetStatisticsAsync(Guid companyId)
    {
        var invoices = await _context.Invoices
            .Where(i => i.CompanyId == companyId)
            .ToListAsync();

        var stats = new InvoiceStatisticsDto
        {
            TotalCount = invoices.Count,
            DraftCount = invoices.Count(i => i.Status == InvoiceStatus.Draft),
            PendingApprovalCount = invoices.Count(i =>
                i.Status == InvoiceStatus.PendingManagerReview ||
                i.Status == InvoiceStatus.PendingAdminApproval),
            ApprovedCount = invoices.Count(i =>
                i.Status == InvoiceStatus.Approved ||
                i.Status == InvoiceStatus.PaymentPending ||
                i.Status == InvoiceStatus.PaymentProcessing),
            CompletedCount = invoices.Count(i => i.Status == InvoiceStatus.Completed),
            RejectedCount = invoices.Count(i =>
                i.Status == InvoiceStatus.RejectedByManager ||
                i.Status == InvoiceStatus.RejectedByAdmin),
            TotalAmount = invoices.Sum(i => i.TotalAmount),
            PendingAmount = invoices
                .Where(i => i.Status != InvoiceStatus.Completed)
                .Sum(i => i.TotalAmount),
            PaidAmount = invoices
                .Where(i => i.Status == InvoiceStatus.Completed)
                .Sum(i => i.TotalAmount)
        };

        // Amount by month for the last 6 months
        var sixMonthsAgo = DateTime.Today.AddMonths(-6);
        stats.AmountByMonth = invoices
            .Where(i => i.InvoiceDate >= sixMonthsAgo)
            .GroupBy(i => i.InvoiceDate.ToString("yyyy-MM"))
            .ToDictionary(g => g.Key, g => g.Sum(i => i.TotalAmount));

        return stats;
    }

    public async Task<List<Invoice>> GetPendingApprovalAsync(Guid companyId, Guid userId, UserRole userRole)
    {
        var query = _context.Invoices
            .Include(i => i.Vendor)
            .Include(i => i.UploadedBy)
            .Where(i => i.CompanyId == companyId);

        if (userRole == UserRole.Manager)
        {
            query = query.Where(i => i.Status == InvoiceStatus.PendingManagerReview);
        }
        else if (userRole == UserRole.Admin || userRole == UserRole.SuperAdmin)
        {
            query = query.Where(i =>
                i.Status == InvoiceStatus.PendingManagerReview ||
                i.Status == InvoiceStatus.PendingAdminApproval);
        }

        return await query
            .OrderBy(i => i.CreatedAt)
            .Take(50)
            .ToListAsync();
    }

    public bool CanEdit(Invoice invoice)
    {
        return invoice.Status == InvoiceStatus.Draft ||
               invoice.Status == InvoiceStatus.RejectedByManager ||
               invoice.Status == InvoiceStatus.RejectedByAdmin;
    }

    public bool CanDelete(Invoice invoice)
    {
        return invoice.Status == InvoiceStatus.Draft;
    }
}
