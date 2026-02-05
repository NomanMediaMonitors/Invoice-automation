using InvoiceAutomation.Web.Core.DTOs;
using InvoiceAutomation.Web.Core.Entities;
using InvoiceAutomation.Web.Core.Enums;
using InvoiceAutomation.Web.Core.Interfaces;
using InvoiceAutomation.Web.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace InvoiceAutomation.Web.Core.Services;

/// <summary>
/// Approval workflow service
/// </summary>
public class ApprovalService : IApprovalService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ApprovalService> _logger;

    // Default approval thresholds (PKR)
    private const decimal ManagerOnlyThreshold = 50000;
    private const decimal AdminRequiredThreshold = 500000;
    private const decimal CfoRequiredThreshold = 1000000;

    public ApprovalService(
        ApplicationDbContext context,
        ILogger<ApprovalService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Invoice> ApproveAsync(Guid invoiceId, Guid approverId, string? comments = null)
    {
        var invoice = await _context.Invoices
            .Include(i => i.Approvals)
            .Include(i => i.Vendor)
            .FirstOrDefaultAsync(i => i.Id == invoiceId)
            ?? throw new KeyNotFoundException($"Invoice not found: {invoiceId}");

        // Get approver's role
        var userCompany = await _context.UserCompanies
            .FirstOrDefaultAsync(uc => uc.UserId == approverId && uc.CompanyId == invoice.CompanyId)
            ?? throw new UnauthorizedAccessException("User is not a member of this company");

        // Validate approval is allowed
        ValidateApproval(invoice, userCompany.Role);

        // Find or create approval record
        var currentLevel = GetCurrentApprovalLevel(invoice);
        var approval = invoice.Approvals
            .FirstOrDefault(a => a.ApprovalLevel == currentLevel && a.Status == ApprovalStatus.Pending);

        if (approval == null)
        {
            approval = new InvoiceApproval
            {
                Id = Guid.NewGuid(),
                InvoiceId = invoiceId,
                ApprovalLevel = currentLevel,
                CreatedAt = DateTime.UtcNow
            };
            invoice.Approvals.Add(approval);
        }

        approval.ApproverId = approverId;
        approval.Status = ApprovalStatus.Approved;
        approval.Comments = comments;
        approval.DecidedAt = DateTime.UtcNow;

        // Determine next status
        var requiredLevel = GetRequiredApprovalLevel(invoice);

        if (currentLevel >= requiredLevel)
        {
            // Final approval reached
            invoice.Status = InvoiceStatus.Approved;
            _logger.LogInformation("Invoice {InvoiceId} fully approved", invoiceId);
        }
        else
        {
            // Need higher level approval
            invoice.Status = currentLevel == ApprovalLevel.Manager
                ? InvoiceStatus.PendingAdminApproval
                : InvoiceStatus.Approved;

            // Create next level approval record
            var nextLevel = (ApprovalLevel)((int)currentLevel + 1);
            invoice.Approvals.Add(new InvoiceApproval
            {
                Id = Guid.NewGuid(),
                InvoiceId = invoiceId,
                ApprovalLevel = nextLevel,
                Status = ApprovalStatus.Pending,
                CreatedAt = DateTime.UtcNow
            });

            _logger.LogInformation("Invoice {InvoiceId} approved at {Level}, needs {NextLevel} approval",
                invoiceId, currentLevel, nextLevel);
        }

        invoice.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return invoice;
    }

    public async Task<Invoice> RejectAsync(Guid invoiceId, Guid approverId, string comments)
    {
        if (string.IsNullOrWhiteSpace(comments))
            throw new ArgumentException("Rejection comments are required");

        var invoice = await _context.Invoices
            .Include(i => i.Approvals)
            .FirstOrDefaultAsync(i => i.Id == invoiceId)
            ?? throw new KeyNotFoundException($"Invoice not found: {invoiceId}");

        var userCompany = await _context.UserCompanies
            .FirstOrDefaultAsync(uc => uc.UserId == approverId && uc.CompanyId == invoice.CompanyId)
            ?? throw new UnauthorizedAccessException("User is not a member of this company");

        ValidateApproval(invoice, userCompany.Role);

        var currentLevel = GetCurrentApprovalLevel(invoice);
        var approval = invoice.Approvals
            .FirstOrDefault(a => a.ApprovalLevel == currentLevel && a.Status == ApprovalStatus.Pending);

        if (approval == null)
        {
            approval = new InvoiceApproval
            {
                Id = Guid.NewGuid(),
                InvoiceId = invoiceId,
                ApprovalLevel = currentLevel,
                CreatedAt = DateTime.UtcNow
            };
            invoice.Approvals.Add(approval);
        }

        approval.ApproverId = approverId;
        approval.Status = ApprovalStatus.Rejected;
        approval.Comments = comments;
        approval.DecidedAt = DateTime.UtcNow;

        invoice.Status = currentLevel == ApprovalLevel.Manager
            ? InvoiceStatus.RejectedByManager
            : InvoiceStatus.RejectedByAdmin;

        invoice.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Invoice {InvoiceId} rejected by {Level}: {Comments}",
            invoiceId, currentLevel, comments);

        return invoice;
    }

    public ApprovalLevel GetRequiredApprovalLevel(Invoice invoice)
    {
        // Check for new vendor (requires admin)
        if (invoice.VendorId.HasValue)
        {
            var vendorInvoiceCount = _context.Invoices
                .Count(i => i.VendorId == invoice.VendorId && i.Id != invoice.Id);

            if (vendorInvoiceCount == 0)
            {
                return ApprovalLevel.Admin; // First invoice from vendor
            }
        }

        // Check amount thresholds
        if (invoice.TotalAmount > CfoRequiredThreshold)
            return ApprovalLevel.CFO;

        if (invoice.TotalAmount > AdminRequiredThreshold)
            return ApprovalLevel.Admin;

        if (invoice.TotalAmount > ManagerOnlyThreshold)
            return ApprovalLevel.Admin;

        return ApprovalLevel.Manager;
    }

    public async Task<bool> CanApproveAsync(Guid invoiceId, Guid userId)
    {
        var invoice = await _context.Invoices
            .FirstOrDefaultAsync(i => i.Id == invoiceId);

        if (invoice == null)
            return false;

        var userCompany = await _context.UserCompanies
            .FirstOrDefaultAsync(uc => uc.UserId == userId && uc.CompanyId == invoice.CompanyId);

        if (userCompany == null)
            return false;

        // Check if invoice is pending approval
        if (invoice.Status != InvoiceStatus.PendingManagerReview &&
            invoice.Status != InvoiceStatus.PendingAdminApproval)
            return false;

        var currentLevel = GetCurrentApprovalLevel(invoice);

        // Check role permissions
        return currentLevel switch
        {
            ApprovalLevel.Manager => userCompany.Role >= UserRole.Manager,
            ApprovalLevel.Admin => userCompany.Role >= UserRole.Admin,
            ApprovalLevel.CFO => userCompany.Role >= UserRole.SuperAdmin,
            _ => false
        };
    }

    public async Task<List<InvoiceApproval>> GetApprovalHistoryAsync(Guid invoiceId)
    {
        return await _context.InvoiceApprovals
            .Include(a => a.Approver)
            .Where(a => a.InvoiceId == invoiceId)
            .OrderBy(a => a.CreatedAt)
            .ToListAsync();
    }

    public async Task<int> GetPendingApprovalCountAsync(Guid companyId, Guid userId, UserRole userRole)
    {
        var query = _context.Invoices.Where(i => i.CompanyId == companyId);

        if (userRole == UserRole.Manager)
        {
            query = query.Where(i => i.Status == InvoiceStatus.PendingManagerReview);
        }
        else if (userRole >= UserRole.Admin)
        {
            query = query.Where(i =>
                i.Status == InvoiceStatus.PendingManagerReview ||
                i.Status == InvoiceStatus.PendingAdminApproval);
        }
        else
        {
            return 0;
        }

        return await query.CountAsync();
    }

    public Task<ApprovalRulesDto> GetApprovalRulesAsync(Guid companyId)
    {
        // In a real implementation, these could be stored per company
        return Task.FromResult(new ApprovalRulesDto
        {
            ManagerOnlyThreshold = ManagerOnlyThreshold,
            AdminRequiredThreshold = AdminRequiredThreshold,
            CfoRequiredThreshold = CfoRequiredThreshold,
            RequireAdminForNewVendor = true,
            AutoApproveFromTrustedVendors = false
        });
    }

    private ApprovalLevel GetCurrentApprovalLevel(Invoice invoice)
    {
        return invoice.Status switch
        {
            InvoiceStatus.PendingManagerReview => ApprovalLevel.Manager,
            InvoiceStatus.PendingAdminApproval => ApprovalLevel.Admin,
            _ => ApprovalLevel.Manager
        };
    }

    private void ValidateApproval(Invoice invoice, UserRole role)
    {
        if (invoice.Status != InvoiceStatus.PendingManagerReview &&
            invoice.Status != InvoiceStatus.PendingAdminApproval)
        {
            throw new InvalidOperationException("Invoice is not pending approval");
        }

        var currentLevel = GetCurrentApprovalLevel(invoice);

        var hasPermission = currentLevel switch
        {
            ApprovalLevel.Manager => role >= UserRole.Manager,
            ApprovalLevel.Admin => role >= UserRole.Admin,
            ApprovalLevel.CFO => role >= UserRole.SuperAdmin,
            _ => false
        };

        if (!hasPermission)
        {
            throw new UnauthorizedAccessException($"User does not have permission to approve at {currentLevel} level");
        }
    }
}
